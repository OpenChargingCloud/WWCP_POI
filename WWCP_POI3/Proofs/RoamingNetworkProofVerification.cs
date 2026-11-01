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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// What a verified proof attests, and by whom (RoamingNetworkProof.TryVerify(…)), as WWCP_POI.TS gives it.
    /// </summary>
    public sealed class RoamingNetworkProofVerification
    {

        #region Properties

        /// <summary>
        /// The root the proof leads to: the effectiveTree ETag of its version.
        /// </summary>
        public ETag                                         ETag              { get; }

        /// <summary>
        /// The roaming network with what the proof gives completely, pruned arrays as they are.
        /// </summary>
        public JObject                                      RoamingNetwork    { get; }

        /// <summary>
        /// The attestations of the root that count: valid, of trusted keys, and as the epoch of a trust context allows.
        /// </summary>
        public ImmutableArray<RoamingNetworkAttestation>    Attestations      { get; }

        /// <summary>
        /// The holders attesting the root, as the verifier knows them, in their ordinal order.
        /// </summary>
        public ImmutableArray<String>                       AttestedBy        { get; }

        /// <summary>
        /// The key statement of the roaming network operator the proof gives, verified.
        /// </summary>
        public RoamingNetworkKeyStatement                   KeyStatement      { get; }

        /// <summary>
        /// The time it was verified at.
        /// </summary>
        public DateTimeOffset                               VerifiedAt        { get; }

        /// <summary>
        /// Until when the observation holds at most: the earliest end of the key statement, of the attestations counted and
        /// of the signing keys and the identity keys certifying them. No lease: a verifier verifies again for every use.
        /// </summary>
        public DateTimeOffset                               ValidUntil        { get; }

        #endregion

        #region (internal) Constructor(s)

        internal RoamingNetworkProofVerification(ETag                                    ETag,
                                                 JObject                                 RoamingNetwork,
                                                 IEnumerable<RoamingNetworkAttestation>  Attestations,
                                                 IEnumerable<String>                     AttestedBy,
                                                 RoamingNetworkKeyStatement              KeyStatement,
                                                 DateTimeOffset                          VerifiedAt,
                                                 DateTimeOffset                          ValidUntil)
        {

            this.ETag            = ETag;
            this.RoamingNetwork  = RoamingNetwork;
            this.Attestations    = [.. Attestations];
            this.AttestedBy      = [.. AttestedBy];
            this.KeyStatement    = KeyStatement;
            this.VerifiedAt      = VerifiedAt;
            this.ValidUntil      = ValidUntil;

        }

        #endregion

    }

}
