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

using System.Net.WebSockets;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// How a peer keeps its history in sync with another server.
    /// </summary>
    public sealed class RoamingNetworkPeerOptions
    {

        /// <summary>
        /// Whether to subscribe to complete commits ("commits") rather than to commit announcements
        /// ("commitAnnouncements") followed by fetching the commits. Default: complete commits.
        /// </summary>
        public Boolean                             PushCompleteCommits         { get; init; } = true;

        /// <summary>
        /// Whether to follow the admin status and status too. Default: yes.
        /// </summary>
        public Boolean                             FollowStatus                { get; init; } = true;

        /// <summary>
        /// Whether to push the commits of this history the other server misses: after connecting, and every new one.
        /// The other server must allow it (AuthorizeCommitPush). Default: yes.
        /// </summary>
        public Boolean                             PushCommits                 { get; init; } = true;

        /// <summary>
        /// The identifications of keys of the roaming network operator a snapshot must name, e.g. its identity key:
        /// the trust anchor of a history started from the snapshot of another server. Not checked, unless given.
        /// </summary>
        public IReadOnlyCollection<String>?        TrustedRoamingNetworkOperatorKeyIds  { get; init; }

        /// <summary>
        /// The number of commits asked for at once while catching up.
        /// </summary>
        public Int32                               MaxCommitsPerRequest        { get; init; } = RoamingNetworkHistory.DefaultMaxCommits;

        /// <summary>
        /// How long to wait for the answer to a request, before the connection counts as broken.
        /// </summary>
        public TimeSpan                            RequestTimeout              { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The first delay before connecting again; it doubles with every failure, up to MaxReconnectDelay.
        /// </summary>
        public TimeSpan                            MinReconnectDelay           { get; init; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The longest delay before connecting again.
        /// </summary>
        public TimeSpan                            MaxReconnectDelay           { get; init; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The largest message accepted from the other server, e.g. a snapshot: 64 MiB.
        /// </summary>
        public Int64                               MaxMessageBytes             { get; init; } = 64 * 1024 * 1024;

        /// <summary>
        /// Configure the WebSocket before it connects, e.g. with credentials or a client certificate.
        /// </summary>
        public Action<ClientWebSocketOptions>?     ConfigureWebSocket          { get; init; }

        /// <summary>
        /// Verifies the signatures of change sets in addition to their owners' keys, if given.
        /// </summary>
        public VerifyChangeSetSignatureDelegate?   VerifyChangeSetSignature    { get; init; }

        /// <summary>
        /// Verifies the signatures of signed commits with trusted keys; without it, signed commits are not taken.
        /// </summary>
        public VerifyCommitSignatureDelegate?      VerifyCommitSignature       { get; init; }

        /// <summary>
        /// The roles every commit must be signed in; none by default.
        /// </summary>
        public IReadOnlyCollection<String>?        RequiredSignatureRoles      { get; init; }

        /// <summary>
        /// The limits for reading change sets, the default ones otherwise.
        /// </summary>
        public RoamingNetworkChangeSetLimits?      ChangeSetLimits             { get; init; }

    }

}
