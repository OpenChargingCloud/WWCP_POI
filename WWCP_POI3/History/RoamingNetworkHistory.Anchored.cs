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

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A history started from another server, anchored in the trust anchor of its roaming network (SPECIFICATION.md,
    /// section 10.5, Anchored): from a snapshot of its head, attested by the roaming network operator, or from a snapshot of
    /// a checkpoint, signed by it. The keys of the operator a version names are public and prove nothing: whoever forged
    /// the version names them as well. RoamingNetworkPeer starts so, and so does whoever starts from a snapshot it fetched.
    /// </summary>
    public sealed partial class RoamingNetworkHistory
    {

        #region (static) TryFromAnchoredSnapshot          (Snapshot, TrustAnchor, out History, out ErrorResponse, At = null, Limits = null)

        /// <summary>
        /// Try to start a history from a snapshot of the head of another server, anchored in the given trust anchor of its
        /// roaming network (SPECIFICATION.md, sections 8.5 and 10.5): the snapshot as TryFromSnapshot takes it, its head
        /// attested by the attestations the snapshot carries, as VerifyHead verifies them, valid at the given time, now by
        /// default. The attestations are read as those of a proof: a snapshot whose attestations are not a JSON array of
        /// attestations that can all be read is refused, as one whose attestations do not attest its head.
        /// </summary>
        /// <param name="Snapshot">A snapshot of the head: { commit, roamingNetwork, attestations, … }, see ToSnapshotJSON().</param>
        /// <param name="TrustAnchor">The trust anchor of the roaming network.</param>
        /// <param name="History">The new history.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="At">When the attestations and their keys must be valid, now by default.</param>
        /// <param name="Limits">Optional limits for the change set of the commit, the default ones otherwise.</param>
        public static Boolean TryFromAnchoredSnapshot(JObject                                          Snapshot,
                                                      RoamingNetworkTrustAnchor                        TrustAnchor,
                                                      [NotNullWhen(true)]  out RoamingNetworkHistory?  History,
                                                      [NotNullWhen(false)] out String?                 ErrorResponse,
                                                      DateTimeOffset?                                  At       = null,
                                                      RoamingNetworkChangeSetLimits?                   Limits   = null)
        {

            History = null;

            if (!TryFromSnapshot(Snapshot, out var history, out ErrorResponse, Limits))
                return false;

            if (!TryParseSnapshotAttestations(Snapshot[PropertyKeys.Attestations], out var attestations, out ErrorResponse))
                return false;

            if (!history.VerifyHead(TrustAnchor, out ErrorResponse, attestations, At))
                return false;

            History = history;
            return true;

        }

        #endregion

        #region (static) TryFromAnchoredCheckpointSnapshot(Snapshot, TrustAnchor, out History, out ErrorResponse, VerifyCheckpointSignature = null, RequiredSignatureRoles = null, PriceStatements = null, At = null, Limits = null)

        /// <summary>
        /// Try to start a history from the snapshot of a checkpoint of another server, anchored in the given trust anchor of
        /// its roaming network (SPECIFICATION.md, section 10.5): the snapshot as TryFromCheckpointSnapshot takes it, and its
        /// checkpoint signed in the role of the roaming network operator as the trust anchor and the key statement of its
        /// version require, as RoamingNetworkProof.Verify(Checkpoint, TrustAnchor, …) verifies it with the proof of the root
        /// of that version, the keys valid at the given time, now by default.
        /// </summary>
        /// <param name="Snapshot">The snapshot of a checkpoint: { commit, roamingNetwork, effectiveLastChanges?, checkpoint }.</param>
        /// <param name="TrustAnchor">The trust anchor of the roaming network.</param>
        /// <param name="History">The new history.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="VerifyCheckpointSignature">A verifier of checkpoint signatures using trusted keys, required for a signed checkpoint.</param>
        /// <param name="RequiredSignatureRoles">The roles the checkpoint must be signed in, e.g. roamingNetworkOperator.</param>
        /// <param name="PriceStatements">The price statements the checkpoint names, if they are given.</param>
        /// <param name="At">When the keys must be valid, now by default.</param>
        /// <param name="Limits">Optional limits for the change set of the commit, the default ones otherwise.</param>
        public static Boolean TryFromAnchoredCheckpointSnapshot(JObject                                          Snapshot,
                                                                RoamingNetworkTrustAnchor                        TrustAnchor,
                                                                [NotNullWhen(true)]  out RoamingNetworkHistory?  History,
                                                                [NotNullWhen(false)] out String?                 ErrorResponse,
                                                                VerifyCheckpointSignatureDelegate?               VerifyCheckpointSignature   = null,
                                                                IReadOnlyCollection<String>?                     RequiredSignatureRoles      = null,
                                                                IEnumerable<RoamingNetworkPriceStatement>?       PriceStatements             = null,
                                                                DateTimeOffset?                                  At                          = null,
                                                                RoamingNetworkChangeSetLimits?                   Limits                      = null)
        {

            History = null;

            if (!TryFromCheckpointSnapshot(Snapshot, out var history, out ErrorResponse, VerifyCheckpointSignature, RequiredSignatureRoles, PriceStatements, Limits))
                return false;

            // Signed by the roaming network operator as the trust anchor requires, with the keys its version gives.
            if (!history.TryGetRootProof(out var rootProof, out ErrorResponse) ||
                !rootProof.Verify(history.Checkpoints[0], TrustAnchor, out ErrorResponse, At))
            {
                return false;
            }

            History = history;
            return true;

        }

        #endregion


        #region (private static) TryParseSnapshotAttestations(JSON, out Attestations, out ErrorResponse)

        /// <summary>
        /// The attestations of a snapshot of the head, read as those of a proof (RoamingNetworkProof.TryParse): none when it
        /// has none, else a JSON array of attestations that can all be read. One that can not be read refuses the snapshot,
        /// even beside others attesting its head: what can not be read is never passed over.
        /// </summary>
        /// <param name="JSON">The attestations of the snapshot, if any.</param>
        /// <param name="Attestations">The attestations.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        private static Boolean TryParseSnapshotAttestations(JToken?                                                          JSON,
                                                            [NotNullWhen(true)]  out IReadOnlyList<RoamingNetworkAttestation>?  Attestations,
                                                            [NotNullWhen(false)] out String?                                    ErrorResponse)
        {

            Attestations = null;

            if (JSON is null)
            {
                Attestations   = [];
                ErrorResponse  = null;
                return true;
            }

            if (JSON is not JArray array)
            {
                ErrorResponse = "The attestations of the snapshot must be a JSON array!";
                return false;
            }

            var attestations = new List<RoamingNetworkAttestation>();

            foreach (var attestationJSON in array)
            {

                if (attestationJSON is not JObject attestationObject)
                {
                    ErrorResponse = $"Invalid attestation {attestations.Count} of the snapshot: it must be a JSON object!";
                    return false;
                }

                if (!RoamingNetworkAttestation.TryParse(attestationObject, out var attestation, out var attestationError))
                {
                    ErrorResponse = $"Invalid attestation {attestations.Count} of the snapshot: {attestationError}";
                    return false;
                }

                attestations.Add(attestation);

            }

            Attestations   = attestations;
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }

}
