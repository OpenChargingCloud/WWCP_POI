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

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Illias.Votes;
using org.GraphDefined.Vanaheimr.Styx.Arrows;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A delegate answering a request for the HTML view of a resource, e.g. with the page of a web frontend.
    /// </summary>
    /// <param name="Request">The HTTP request, preferring text/html.</param>
    public delegate Task<HTTPResponse> HTMLViewDelegate(HTTPRequest Request);


    public partial class POI_API : HTTPAPI
    {

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys of the requests and responses of this API, over HTTP and WebSocket, in JSON and CBOR.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of a list of checkpoints.
            /// </summary>
            public const String  Checkpoints              = "checkpoints";

            /// <summary>
            /// The property of the identification of a checkpoint.
            /// </summary>
            public const String  CheckpointId             = "checkpointId";

            /// <summary>
            /// The property of the identification of the final checkpoint of an epoch.
            /// </summary>
            public const String  FinalCheckpointId        = "finalCheckpointId";

            /// <summary>
            /// The property of a list of price statements.
            /// </summary>
            public const String  PriceStatements          = "priceStatements";

            /// <summary>
            /// The property of a list of commits.
            /// </summary>
            public const String  Commits                  = "commits";

            /// <summary>
            /// The property of a list of commit metadata.
            /// </summary>
            public const String  CommitInfos              = "commitInfos";

            /// <summary>
            /// The property of a change set.
            /// </summary>
            public const String  ChangeSet                = "changeSet";

            /// <summary>
            /// The property of the identification of a change set.
            /// </summary>
            public const String  ChangeSetId              = "changeSetId";

            /// <summary>
            /// The property of the change sets waiting for signatures.
            /// </summary>
            public const String  PendingChangeSets        = "pendingChangeSets";

            /// <summary>
            /// The property of signatures, e.g. of a change set waiting for them.
            /// </summary>
            public const String  Signatures               = "signatures";

            /// <summary>
            /// The property of the withdrawal of a change set waiting for signatures.
            /// </summary>
            public const String  Withdrawal               = "withdrawal";

            /// <summary>
            /// The property of a list of attestations.
            /// </summary>
            public const String  Attestations             = "attestations";

            /// <summary>
            /// The property of a list of status updates.
            /// </summary>
            public const String  StatusUpdates            = "statusUpdates";

            /// <summary>
            /// The property of the runtime status, or whether it is wanted.
            /// </summary>
            public const String  RuntimeStatus            = "runtimeStatus";

            /// <summary>
            /// The property of the current status of an entity.
            /// </summary>
            public const String  Current                  = "current";

            /// <summary>
            /// The property of the position or the commit to start after.
            /// </summary>
            public const String  Since                    = "since";

            /// <summary>
            /// The property of the maximum number of entries wanted.
            /// </summary>
            public const String  Max                      = "max";

            /// <summary>
            /// The property asking for the latest entry only.
            /// </summary>
            public const String  Latest                   = "latest";

            /// <summary>
            /// The property of the commits a peer has.
            /// </summary>
            public const String  Have                     = "have";

            /// <summary>
            /// The property of the identifications of the commits a history has.
            /// </summary>
            public const String  HaveIds                  = "haveIds";

            /// <summary>
            /// The property of the head commit.
            /// </summary>
            public const String  Head                     = "head";

            /// <summary>
            /// The property of the tips of a history.
            /// </summary>
            public const String  Tips                     = "tips";

            /// <summary>
            /// The property of the conflicts between the tips of a history.
            /// </summary>
            public const String  Conflicts                = "conflicts";

            /// <summary>
            /// The property of the status of an outcome or of an epoch.
            /// </summary>
            public const String  Status                   = "status";

            /// <summary>
            /// The property of the result of one entry of a batch.
            /// </summary>
            public const String  Result                   = "result";

            /// <summary>
            /// The property of the results of a batch.
            /// </summary>
            public const String  Results                  = "results";

            /// <summary>
            /// The property of a description, e.g. of an error.
            /// </summary>
            public const String  Description              = "description";

            /// <summary>
            /// The property of the number of entries taken.
            /// </summary>
            public const String  Taken                    = "taken";

            /// <summary>
            /// The property of the number of entries already known.
            /// </summary>
            public const String  Known                    = "known";

            /// <summary>
            /// The property of the number of outdated entries.
            /// </summary>
            public const String  Outdated                 = "outdated";

            /// <summary>
            /// The property of the number of entries refused.
            /// </summary>
            public const String  Refused                  = "refused";

            /// <summary>
            /// The property of why entries were refused.
            /// </summary>
            public const String  Refusals                 = "refusals";

            /// <summary>
            /// The property of the index of an entry.
            /// </summary>
            public const String  Index                    = "index";

            /// <summary>
            /// The property of the number of signatures added.
            /// </summary>
            public const String  Added                    = "added";

            /// <summary>
            /// The property of the number of commits appended.
            /// </summary>
            public const String  Appended                 = "appended";

            /// <summary>
            /// The property of the segments of an archive.
            /// </summary>
            public const String  Segments                 = "segments";

            /// <summary>
            /// The property of the number of a segment of an archive.
            /// </summary>
            public const String  Number                   = "number";

            /// <summary>
            /// The property of the identification of a charging station.
            /// </summary>
            public const String  ChargingStationId        = "chargingStationId";

            /// <summary>
            /// The property of the identification of an EVSE.
            /// </summary>
            public const String  EVSEId                   = "EVSEId";

            /// <summary>
            /// The property of the digest of a price statement.
            /// </summary>
            public const String  Digest                   = "digest";

            /// <summary>
            /// The property of a hash algorithm.
            /// </summary>
            public const String  HashAlgorithm            = "hashAlgorithm";

            /// <summary>
            /// The property of the identification of a roaming network.
            /// </summary>
            public const String  RoamingNetworkId         = "roamingNetworkId";

            /// <summary>
            /// The property of the identifications of the identity keys of a trust anchor.
            /// </summary>
            public const String  IdentityKeyIds           = "identityKeyIds";

            /// <summary>
            /// The property of the URI of a trust anchor.
            /// </summary>
            public const String  URI                      = "uri";

            /// <summary>
            /// The property of the minConsensus of a trust anchor: how many holders of the identity keys sign.
            /// </summary>
            public const String  MinConsensus             = POIPartyKeys.MinConsensusProperty;

            /// <summary>
            /// The property of the text of the QR code of a trust anchor: WWCP/TA-2: and the Base45 of its CBOR.
            /// </summary>
            public const String  QRCode                   = "qrCode";

            /// <summary>
            /// The property of a trust anchor in its JSON representation.
            /// </summary>
            public const String  TrustAnchor              = "trustAnchor";

            /// <summary>
            /// The property of the charging station operator of a charging pool found by a query.
            /// </summary>
            public const String  ChargingStationOperator  = "chargingStationOperator";

            /// <summary>
            /// The property of the grid connection point of a charging pool, when expanded.
            /// </summary>
            public const String  GridConnectionPoint      = "gridConnectionPoint";

            /// <summary>
            /// The property of the identification of the grid operator of an expanded grid connection point.
            /// </summary>
            public const String  GridOperatorId           = "gridOperatorId";

        }

        #endregion


        public TimeProvider       TimeProvider    { get; }

        /// <summary>
        /// Answers the requests for the HTML view of the lists and the entities, e.g. with the page of a web frontend,
        /// when a request prefers text/html to JSON and CBOR. Without it, every request gets JSON or CBOR.
        /// </summary>
        public HTMLViewDelegate?  HTMLView        { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new API of roaming networks: their histories for synchronization, and their entities and lists to query.
        /// </summary>
        /// <param name="HTTPServer">The HTTP server.</param>
        /// <param name="TimeProvider">The time provider.</param>
        /// <param name="RootPath">The path of the API on the HTTP server, e.g. /poi; / by default.</param>
        /// <param name="HTMLView">Answers the requests for the HTML view of the lists and the entities, e.g. with the page of a web frontend.</param>
        public POI_API(HTTPServer         HTTPServer,
                       TimeProvider?      TimeProvider   = null,
                       HTTPPath?          RootPath       = null,
                       HTMLViewDelegate?  HTMLView       = null)

            : base(HTTPServer,
                   RootPath: RootPath)

        {

            this.TimeProvider            = TimeProvider ?? TimeProvider.System;
            this.HTMLView                = HTMLView;

            this.roamingNetworkAddition  = new VotingNotificator<POI_API, RoamingNetwork, Boolean>(() => new VetoVote(), true);
            this.roamingNetworkRemoval   = new VotingNotificator<POI_API, RoamingNetwork, Boolean>(() => new VetoVote(), true);

            RegisterEntityURLTemplates();
            RegisterQueryURLTemplates();
            RegisterLocationURLTemplates();
            RegisterHistoryURLTemplates();
            RegisterCoSigningURLTemplates();
            RegisterProofURLTemplates();
            RegisterPriceStatementURLTemplates();
            RegisterEVTalerURLTemplates();
            RegisterPeerURLTemplates();
            RegisterWebSocketURLTemplates();

        }

        #endregion


        #region RoamingNetworks

        #region Data

        private readonly ConcurrentDictionary<RoamingNetwork_Id, RoamingNetwork> roamingNetworks = [];

        #endregion

        #region Events

        #region OnRoamingNetworkAddition

        private readonly IVotingNotificator<POI_API, RoamingNetwork, Boolean> roamingNetworkAddition;

        /// <summary>
        /// Called whenever a roaming network will be or was added.
        /// </summary>
        public IVotingSender<POI_API, RoamingNetwork, Boolean> OnRoamingNetworkAddition
            => roamingNetworkAddition;

        #endregion

        #region OnRoamingNetworkRemoval

        private readonly IVotingNotificator<POI_API, RoamingNetwork, Boolean> roamingNetworkRemoval;

        /// <summary>
        /// Called whenever a roaming network will be or was removed.
        /// </summary>
        public IVotingSender<POI_API, RoamingNetwork, Boolean> OnRoamingNetworkRemoval
            => roamingNetworkRemoval;

        #endregion

        #endregion


        #region AddRoamingNetwork(RoamingNetwork)

        /// <summary>
        /// Register the given roaming network.
        /// </summary>
        /// <param name="RoamingNetwork">The roaming network to add.</param>
        public AddRoamingNetworkResult AddRoamingNetwork(RoamingNetwork    RoamingNetwork,
                                                         EventTracking_Id  EventTrackingId)
        {

            if (roamingNetworkAddition.SendVoting(EventTrackingId, this, RoamingNetwork) &&
                roamingNetworks.TryAdd(RoamingNetwork.Id, RoamingNetwork))
            {
                roamingNetworkAddition.SendNotification(EventTrackingId, this, RoamingNetwork);
                return AddRoamingNetworkResult.Success(RoamingNetwork, EventTrackingId);
            }

            return AddRoamingNetworkResult.Error(
                       RoamingNetwork,
                       I18NString.Create(Languages.en, "Could not add roaming network!"),
                       EventTrackingId
                   );

        }

        #endregion


        #region Contains(RoamingNetworkId)

        /// <summary>
        /// Return the roaming network identified by the given unique roaming network identification.
        /// </summary>
        /// <param name="RoamingNetworkId">The unique identification of a roaming network.</param>
        public Boolean Contains(RoamingNetwork_Id RoamingNetworkId)

            => roamingNetworks.ContainsKey(RoamingNetworkId);

        #endregion

        #region GetRoamingNetwork(RoamingNetworkId)

        /// <summary>
        /// Return the roaming network identified by the given unique roaming network identification.
        /// </summary>
        /// <param name="RoamingNetworkId">The unique identification of a roaming network.</param>
        public RoamingNetwork? GetRoamingNetwork(RoamingNetwork_Id RoamingNetworkId)
        {

            if (roamingNetworks.TryGetValue(RoamingNetworkId, out var roamingNetwork))
                return roamingNetwork;

            return null;

        }

        #endregion

        #region TryGetRoamingNetwork(RoamingNetworkId, out RoamingNetwork)

        /// <summary>
        /// Try to return the roaming network identified by the given unique roaming network identification.
        /// </summary>
        /// <param name="RoamingNetworkId">The unique identification of a roaming network.</param>
        /// <param name="RoamingNetwork">The roaming network.</param>
        /// <returns>True, when the roaming network was found; false else.</returns>
        public Boolean TryGetRoamingNetwork(RoamingNetwork_Id                        RoamingNetworkId,
                                            [NotNullWhen(true)] out RoamingNetwork?  RoamingNetwork)

            => roamingNetworks.TryGetValue(RoamingNetworkId, out RoamingNetwork);

        #endregion

        #region RemoveRoamingNetwork(RoamingNetworkId)

        /// <summary>
        /// Try to return the roaming network identified by the given unique roaming network identification.
        /// </summary>
        /// <param name="RoamingNetworkId">The unique identification of a roaming network.</param>
        /// <returns>True, when the roaming network was found; false else.</returns>
        public RoamingNetwork? RemoveRoamingNetwork(RoamingNetwork_Id RoamingNetworkId)
        {

            if (roamingNetworks.TryRemove(RoamingNetworkId, out var roamingNetwork))
                return roamingNetwork;

            return null;

        }

        #endregion

        #region RemoveRoamingNetwork(RoamingNetworkId, out RoamingNetwork)

        /// <summary>
        /// Try to return the roaming network identified by the given unique roaming network identification.
        /// </summary>
        /// <param name="RoamingNetworkId">The unique identification of a roaming network.</param>
        /// <param name="RoamingNetwork">The roaming network.</param>
        /// <returns>True, when the roaming network was found; false else.</returns>
        public Boolean RemoveRoamingNetwork(RoamingNetwork_Id RoamingNetworkId, out RoamingNetwork? RoamingNetwork)

            => roamingNetworks.TryRemove(RoamingNetworkId, out RoamingNetwork);

        #endregion

        #endregion


    }

}
