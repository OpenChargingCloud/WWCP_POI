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

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A witness of checkpoints (SPECIFICATION.md, section 10.2): the key it signs with, in the role witness, and the
    /// journal of what it signed, kept beyond every history it signs from. One per witness: every history it witnesses
    /// with, also one started anew from a snapshot or of the next epoch, signs through the same one.
    /// </summary>
    /// <param name="Signer">The key it signs with.</param>
    /// <param name="Journal">The journal of what it signed: in a file, so that it outlives the process.</param>
    public sealed class RoamingNetworkWitness(RoamingNetworkCommitSigner    Signer,
                                              RoamingNetworkWitnessJournal  Journal)
    {

        /// <summary>
        /// The key it signs with.
        /// </summary>
        public RoamingNetworkCommitSigner    Signer     { get; } = Signer  ?? throw new ArgumentNullException(nameof(Signer));

        /// <summary>
        /// The journal of what it signed.
        /// </summary>
        public RoamingNetworkWitnessJournal  Journal    { get; } = Journal ?? throw new ArgumentNullException(nameof(Journal));

    }

}
