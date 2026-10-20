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
    /// The epochs of a peer (SPECIFICATION.md, section 11): when the other server serves the next epoch, a history whose
    /// epoch its final checkpoint closed starts the next one itself, from the version it has, and catches up there; one
    /// without the final checkpoint starts from the other server. A history in an epoch the other server has not reached
    /// yet waits for it.
    /// </summary>
    public sealed partial class RoamingNetworkPeer
    {

        #region (private) FollowEpochAsync(Connection, CancellationToken)

        /// <summary>
        /// Move to the epoch of the other server, before catching up.
        /// </summary>
        private async Task FollowEpochAsync(Connection         Connection,
                                            CancellationToken  CancellationToken)
        {

            var currentHistory  = History!;
            var answer          = await Connection.RequestAsync("epoch", [], CancellationToken);

            // A server without epochs is in the first one.
            var theirs          = answer.IsError ? 1 : answer.GetPayloadJSON()?["epoch"]?.Value<UInt32>() ?? 1;
            var ours            = currentHistory.Epoch.Number;

            if (theirs == ours)
                return;

            if (theirs < ours)
                throw new InvalidOperationException($"The other server is in the epoch {theirs} still, this history in the epoch {ours}: it is asked again later.");

            if (!options.FollowEpochs)
                throw new InvalidOperationException($"The other server is in the epoch {theirs}, this history in the epoch {ours}, and the peer does not follow epochs!");

            // From the final checkpoint held here, as every server does; else as from the start.
            if (theirs == ours + 1 &&
                currentHistory.FinalCheckpoint is not null &&
                currentHistory.TryStartNextEpoch(out var nextHistory,
                                                 out var errorResponse,
                                                 Store:      options.EpochStore?.Invoke(theirs),
                                                 Retention:  options.Retention))
            {
                ReplaceHistory(nextHistory);
                return;
            }

            await StartFromSnapshotAsync(Connection, CancellationToken);

        }

        #endregion

        #region (private) ReplaceHistory(NewHistory)

        /// <summary>
        /// Keep the given history of the next epoch in sync from now on; the one of the epoch before stays as it is.
        /// </summary>
        private void ReplaceHistory(RoamingNetworkHistory NewHistory)
        {

            // The status taken along is the one known there.
            receivedStatus.Clear();

            foreach (var statusUpdate in NewHistory.RuntimeStatus.All)
                Received(statusUpdate);

            RoamingNetworkHistory? oldHistory;

            lock (watchLock)
            {
                oldHistory = Interlocked.Exchange(ref history, NewHistory);
                Watch(NewHistory);
            }

            statusFromSnapshot = false;

            OnHistoryReplaced?.Invoke(this, oldHistory, NewHistory);

        }

        #endregion

    }

}
