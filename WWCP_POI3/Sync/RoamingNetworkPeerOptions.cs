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
        /// Whether to push the status set at this history to the other server, set here or taken from elsewhere, but not from
        /// the other server: after connecting the status it has differently, then every change. The other server must allow it
        /// (AuthorizeStatusPush). Default: yes.
        /// </summary>
        public Boolean                             PushStatus                  { get; init; } = true;

        /// <summary>
        /// The number of status updates pushed at once, at most the batch size of the other server.
        /// </summary>
        public Int32                               MaxStatusUpdatesPerRequest  { get; init; } = POIWebSocketServer.DefaultMaxBatchSize;

        /// <summary>
        /// The store to keep a history started from a snapshot in, e.g. at first or when the other server knew none of its commits:
        /// its content is replaced then. A history given to the peer keeps the store it has.
        /// </summary>
        public IRoamingNetworkHistoryStore?       Store                       { get; init; }

        /// <summary>
        /// What a history started from a snapshot keeps; everything, unless given.
        /// </summary>
        public RoamingNetworkHistoryRetention?    Retention                   { get; init; }

        /// <summary>
        /// Where the history keeps the commits it drops when compacting, e.g. a RoamingNetworkHistoryFileArchive.
        /// </summary>
        public IRoamingNetworkHistoryArchive?     Archive                     { get; init; }

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
        /// Whether to take the checkpoints of the other server, checked against this history. Default: yes.
        /// </summary>
        public Boolean                             FollowCheckpoints           { get; init; } = true;

        /// <summary>
        /// Whether to push the checkpoints made or taken here, and new signatures of checkpoints, e.g. of a witness. Default: yes.
        /// </summary>
        public Boolean                             PushCheckpoints             { get; init; } = true;

        /// <summary>
        /// Verifies the signatures of checkpoints with trusted keys; without it, signed checkpoints are not taken.
        /// </summary>
        public VerifyCheckpointSignatureDelegate?  VerifyCheckpointSignature   { get; init; }

        /// <summary>
        /// The roles every checkpoint must be signed in: the roaming network operator by default.
        /// </summary>
        public IReadOnlyCollection<String>?        RequiredCheckpointSignatureRoles  { get; init; } = [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ];

        /// <summary>
        /// Verifies the signatures of attestations with keys trusted independently of the network, e.g. the signing keys of
        /// the roaming network operator: with it, the peer takes the attestations of the other server, so that this server
        /// serves proofs of charging stations with them; without it, it takes none.
        /// </summary>
        public VerifyAttestationDelegate?          VerifyAttestation           { get; init; }

        /// <summary>
        /// Whether to take the price statements of the other server, checked against this history, and those of its
        /// checkpoints, so that this server serves them as well. Default: yes.
        /// </summary>
        public Boolean                             FollowPriceStatements       { get; init; } = true;

        /// <summary>
        /// Whether to push the price statements taken here, made here or taken from elsewhere, but not from the other server:
        /// after connecting those it misses, then every new one, e.g. from a server of a charging station operator to the
        /// one of the roaming network operator. Default: yes.
        /// </summary>
        public Boolean                             PushPriceStatements         { get; init; } = true;

        /// <summary>
        /// The limits for reading change sets, the default ones otherwise.
        /// </summary>
        public RoamingNetworkChangeSetLimits?      ChangeSetLimits             { get; init; }

    }

}
