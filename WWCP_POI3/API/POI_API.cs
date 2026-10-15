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

        public TimeProvider       TimeProvider    { get; }

        /// <summary>
        /// Answers the requests for the HTML view of the lists, e.g. with the page of a web frontend,
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
        /// <param name="HTMLView">Answers the requests for the HTML view of the lists, e.g. with the page of a web frontend.</param>
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
            RegisterHistoryURLTemplates();
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
