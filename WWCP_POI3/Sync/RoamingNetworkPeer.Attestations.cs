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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The attestations of a peer: taken from the other server, e.g. one of the roaming network operator, when the peer
    /// verifies them with trusted keys (VerifyAttestation), so that this server serves proofs of charging stations with
    /// them as well (SPECIFICATION.md, sections 8.4 and 8.5).
    /// </summary>
    public sealed partial class RoamingNetworkPeer
    {

        #region (private) SynchronizeAttestationsAsync(Connection, CancellationToken)

        /// <summary>
        /// Take the attestations of the head of the other server, after its commits.
        /// </summary>
        private async Task SynchronizeAttestationsAsync(Connection         Connection,
                                                        CancellationToken  CancellationToken)
        {

            var answer = await Connection.RequestAsync("attestations", [], CancellationToken);

            if (answer.IsError)
            {
                RaiseError($"The other server tells no attestations: {answer.ErrorDescription}", null);
                return;
            }

            foreach (var attestationJSON in answer.GetPayloadJSON()?["attestations"]?.OfType<JObject>() ?? [])
                TakeAttestation(attestationJSON);

        }

        #endregion

        #region (private) TakeAttestation(AttestationJSON)

        /// <summary>
        /// Take an attestation of the other server; one this history refuses is told by an error.
        /// </summary>
        private void TakeAttestation(JObject AttestationJSON)
        {

            if (options.VerifyAttestation is not VerifyAttestationDelegate verifyAttestation)
                return;

            if (!RoamingNetworkAttestation.TryParse(AttestationJSON, out var attestation, out var errorResponse))
            {
                RaiseError("Invalid attestation: " + errorResponse, null);
                return;
            }

            if (!History!.TryAddAttestation(attestation, verifyAttestation, out errorResponse))
                RaiseError("The attestation of the other server can not be taken: " + errorResponse, null);

        }

        #endregion

    }

}
