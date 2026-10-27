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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// A history started from another server only anchored in the trust anchor of its roaming network (SPECIFICATION.md,
    /// sections 8.5 and 10.5): TryFromAnchoredSnapshot and TryFromAnchoredCheckpointSnapshot, as RoamingNetworkPeer starts.
    /// </summary>
    [TestFixture]
    public class AnchoredSnapshotTests
    {

        private static readonly DateTimeOffset Created = DateTimeOffset.Parse("2026-01-01T00:00:00Z");


        #region (static) NewHistory()

        /// <summary>
        /// A history of the roaming network rn1, its operator stating its identity key, attesting its head and signing its
        /// checkpoints.
        /// </summary>
        private static RoamingNetworkHistory NewHistory()
        {

            var history = RoamingNetworkHistory.Start(new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                                                         Created:                   Created,
                                                                         ChargingStationOperators:  [ new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GEF"),
                                                                                                                                  Created: Created) ]).WithKeys().WithKeyStatement());

            history.CheckpointSigners = [ history.Head.RoamingNetwork.OperatorSigner() ];

            return history.Attesting();

        }

        #endregion


        #region An_attested_snapshot_is_anchored()

        [Test]
        public void An_attested_snapshot_is_anchored()
        {

            var server = NewHistory();

            Assert.That(RoamingNetworkHistory.TryFromAnchoredSnapshot(server.ToSnapshotJSON(IncludeAttestations: true), server.Head.RoamingNetwork.TrustAnchor(), out var history, out var error), Is.True, error);
            Assert.That(history!.Head.Commit.Id,  Is.EqualTo(server.Head.Commit.Id));

        }

        #endregion

        #region A_snapshot_without_attestations_is_not_anchored()

        /// <summary>
        /// The keys of the operator a version names are public and prove nothing.
        /// </summary>
        [Test]
        public void A_snapshot_without_attestations_is_not_anchored()
        {

            var server = NewHistory();

            Assert.That(RoamingNetworkHistory.TryFromAnchoredSnapshot(server.ToSnapshotJSON(), server.Head.RoamingNetwork.TrustAnchor(), out _, out var error), Is.False);
            Assert.That(error,  Does.StartWith("No attestation of the head"));

        }

        #endregion

        #region An_attestation_that_can_not_be_read_refuses_the_snapshot(Unreadable, Error)

        /// <summary>
        /// The attestations of a snapshot are read as those of a proof: one that can not be read refuses the snapshot, though
        /// the attestation of the operator beside it attests the head.
        /// </summary>
        [TestCase("text",     "Invalid attestation 1 of the snapshot: it must be a JSON object!",  Description = "Beside the attestation of the head, one given as JSON text")]
        [TestCase("unsigned", "Invalid attestation 1 of the snapshot: The signing timestamp",      Description = "Beside the attestation of the head, one without its signedAt")]
        [TestCase("object",   "The attestations of the snapshot must be a JSON array!",            Description = "The attestation of the head, not in an array")]
        public void An_attestation_that_can_not_be_read_refuses_the_snapshot(String Unreadable, String Error)
        {

            var server        = NewHistory();
            var snapshot      = server.ToSnapshotJSON(IncludeAttestations: true);
            var attestations  = (JArray) snapshot[RoamingNetworkHistory.PropertyKeys.Attestations]!;
            var attestation   = (JObject) attestations.Single();

            switch (Unreadable)
            {

                case "text":
                    attestations.Add(new JValue(attestation.ToString(Formatting.None)));
                    break;

                case "unsigned":
                    var unsigned = (JObject) attestation.DeepClone();
                    unsigned.Remove(RoamingNetworkAttestation.PropertyKeys.SignedAt);
                    attestations.Add(unsigned);
                    break;

                default:
                    snapshot[RoamingNetworkHistory.PropertyKeys.Attestations] = attestation.DeepClone();
                    break;

            }

            Assert.That(RoamingNetworkHistory.TryFromAnchoredSnapshot(snapshot, server.Head.RoamingNetwork.TrustAnchor(), out _, out var error), Is.False);
            Assert.That(error,  Does.StartWith(Error));

        }

        #endregion

        #region A_snapshot_of_a_checkpoint_signed_by_the_operator_is_anchored()

        [Test]
        public void A_snapshot_of_a_checkpoint_signed_by_the_operator_is_anchored()
        {

            var server = NewHistory();

            Assert.That(server.TryMakeCheckpoint(out var checkpoint, out var error),                        Is.True, error);
            Assert.That(server.TryGetCheckpointSnapshotJSON(checkpoint!.Id, out var snapshot, out error),   Is.True, error);

            Assert.That(RoamingNetworkHistory.TryFromAnchoredCheckpointSnapshot(snapshot!,
                                                                                server.Head.RoamingNetwork.TrustAnchor(),
                                                                                out var joined,
                                                                                out error,
                                                                                TestParties.VerifyOperatorCheckpointSignature(server.Head.RoamingNetwork)),  Is.True, error);

            Assert.That(joined!.Checkpoints[0].Id,  Is.EqualTo(checkpoint.Id));

        }

        #endregion

    }

}
