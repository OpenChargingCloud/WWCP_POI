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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Proofs
{

    /// <summary>
    /// A proof is whatever it was handed (POI3-ISSUES.md S29): a size beyond 64 bits, which Newtonsoft reads as a
    /// BigInteger, or an object as the identification of the roaming network makes it no proof, and its verification says
    /// so instead of throwing; what it gives of a child array partially or not at all, it tells.
    /// </summary>
    [TestFixture]
    public class ProofRobustnessTests
    {

        #region Data

        private static readonly ChargingStation_Id  S1  = ChargingStation_Id.Parse("DE*GEF*S1");

        #endregion


        #region (private static) ProofWith(Change)

        /// <summary>
        /// The station proof of S1 of a fresh history, with the given change to its JSON, read again.
        /// </summary>
        private static RoamingNetworkProof ProofWith(Action<JObject> Change)
        {

            var history = RoamingNetworkHistory.Start(StationProofTests.NewNetwork());

            Assert.That(history.TryGetChargingStationProof(S1, out var proof, out var error), Is.True, error);

            var json = proof!.ToJSON();
            Change(json);

            Assert.That(RoamingNetworkProof.TryParse(json, out var changed, out error), Is.True, error);

            return changed!;

        }

        #endregion


        #region A_size_beyond_64_bits_makes_no_proof()

        [Test]
        public void A_size_beyond_64_bits_makes_no_proof()
        {

            var proof  = ProofWith(json => json["roamingNetwork"]!["chargingStationOperators"]!["@size"] = JToken.Parse("100000000000000000000"));
            var anchor = StationProofTests.NewNetwork().TrustAnchor();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(((JValue) proof.RoamingNetwork["chargingStationOperators"]!["@size"]!).Value,  Is.TypeOf<System.Numerics.BigInteger>());
                Assert.That(proof.TryCalculateETag(out _, out var error),                     Is.False);
                Assert.That(error,                                                           Does.Contain("@size"));
                Assert.That(() => proof.Verify(StationProofTests.Trusted, out _, out _),     Throws.Nothing);
                Assert.That(proof.Verify(StationProofTests.Trusted, out _, out _),            Is.False);
                Assert.That(proof.Verify(anchor, out _, out _),                               Is.False);
                Assert.That(RoamingNetworkProof.SizeOf(proof.RoamingNetwork, "chargingStationOperators"),  Is.Null);
            }

        }

        #endregion

        #region An_object_as_the_identification_makes_no_proof()

        [Test]
        public void An_object_as_the_identification_makes_no_proof()
        {

            var proof  = ProofWith(json => json["roamingNetwork"]!["@id"] = new JObject(new JProperty("@id", "rn1")));
            var anchor = StationProofTests.NewNetwork().TrustAnchor();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => proof.Verify(anchor, out _, out _),                         Throws.Nothing);
                Assert.That(proof.Verify(anchor, out _, out var error),                       Is.False);
                Assert.That(error,                                                           Does.Contain("trust anchor"));
                Assert.That(() => proof.Verify(StationProofTests.Trusted, out _, out _),     Throws.Nothing);
                Assert.That(proof.Verify(StationProofTests.Trusted, out _, out _),            Is.False);
            }

        }

        #endregion

        #region A_proof_tells_what_it_gives_completely()

        [Test]
        public void A_proof_tells_what_it_gives_completely()
        {

            var proof     = ProofWith(json => { });
            var network   = proof.RoamingNetwork;
            var operator1 = proof.Given("chargingStationOperators").Single();
            var station   = proof.Given("chargingStationOperators", "chargingPools", "chargingStations").Single();

            using (Assert.EnterMultipleScope())
            {

                // Two operators, one given: partial.
                Assert.That(RoamingNetworkProof.GivesCompletely(network,   "chargingStationOperators"),  Is.False);
                Assert.That(RoamingNetworkProof.SizeOf         (network,   "chargingStationOperators"),  Is.EqualTo(2));

                // Its pools: the one of the station given, of two.
                Assert.That(RoamingNetworkProof.GivesCompletely(operator1, "chargingPools"),             Is.False);
                Assert.That(RoamingNetworkProof.SizeOf         (operator1, "chargingPools"),             Is.EqualTo(2));

                // The station with all its EVSEs.
                Assert.That(RoamingNetworkProof.GivesCompletely(station,   "EVSEs"),                     Is.True);
                Assert.That(RoamingNetworkProof.SizeOf         (station,   "EVSEs"),                     Is.EqualTo((UInt64) proof.GivenBelow(station, "EVSEs").Count()));

                // A child array the entity has none of is given completely, as none.
                Assert.That(RoamingNetworkProof.GivesCompletely(station,   "noSuchChildren"),            Is.True);
                Assert.That(RoamingNetworkProof.SizeOf         (station,   "noSuchChildren"),            Is.EqualTo(0));

            }

        }

        #endregion

    }

}
