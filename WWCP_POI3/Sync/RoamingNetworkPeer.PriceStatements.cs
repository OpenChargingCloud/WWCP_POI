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
using System.Threading.Channels;

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The price statements of a peer (SPECIFICATION.md, section 12): those of the other server are taken, checked against
    /// this history, and those taken here go to the other server, e.g. from a server of a charging station operator to the
    /// one of the roaming network operator; and the price statements a checkpoint names come with it.
    /// </summary>
    public sealed partial class RoamingNetworkPeer
    {

        #region Data

        // The digests of the price statements taken from the other server or known there: not to be pushed back.
        private readonly ConcurrentDictionary<String, Byte> receivedPriceStatements = [];

        #endregion


        #region (private) SynchronizePriceStatementsAsync(Connection, CancellationToken)

        /// <summary>
        /// Take the current price statements of the other server, after its commits: a price statement names an entity, which
        /// must be there first. Without following them, they are still asked for: to push only those the other server misses.
        /// </summary>
        private async Task SynchronizePriceStatementsAsync(Connection         Connection,
                                                           CancellationToken  CancellationToken)
        {

            var answer = await Connection.RequestAsync("priceStatements", [], CancellationToken);

            if (answer.IsError)
            {
                RaiseError($"The other server tells no price statements: {answer.ErrorDescription}", null);
                return;
            }

            foreach (var priceStatementJSON in answer.GetPayloadJSON()?["priceStatements"]?.OfType<JObject>() ?? [])
            {

                if (options.FollowPriceStatements)
                    TakePriceStatement(priceStatementJSON);

                else if (RoamingNetworkPriceStatement.TryParse(priceStatementJSON, out var priceStatement, out _))
                    receivedPriceStatements.TryAdd(priceStatement.Digest, 0);

            }

        }

        #endregion

        #region (private) TakePriceStatement(PriceStatementJSON)

        /// <summary>
        /// Take a price statement of the other server; one this history refuses is told by an error, one out of date is not.
        /// </summary>
        private void TakePriceStatement(JObject PriceStatementJSON)
        {

            if (!RoamingNetworkPriceStatement.TryParse(PriceStatementJSON, out var priceStatement, out var errorResponse))
            {
                RaiseError("Invalid price statement: " + errorResponse, null);
                return;
            }

            // First: taking it tells of it, and what came from there does not go back.
            receivedPriceStatements.TryAdd(priceStatement.Digest, 0);

            if (History!.TryAddPriceStatement(priceStatement, out errorResponse) == POIPriceStatementResult.Refused)
                RaiseError("The price statement of the other server can not be taken: " + errorResponse, null);

        }

        #endregion

        #region (private) PushMissingPriceStatements(Pushes)

        /// <summary>
        /// The current price statements of this history the other server does not know, e.g. made here while not connected.
        /// </summary>
        private void PushMissingPriceStatements(ChannelWriter<Object> Pushes)
        {
            foreach (var priceStatement in History!.PriceStatements)
                if (!receivedPriceStatements.ContainsKey(priceStatement.Digest))
                    Pushes.TryWrite(priceStatement);
        }

        #endregion

        #region (private) PriceStatementTaken(PriceStatement)

        private void PriceStatementTaken(RoamingNetworkPriceStatement PriceStatement)
        {
            if (!receivedPriceStatements.ContainsKey(PriceStatement.Digest))
                Volatile.Read(ref outgoing)?.Writer.TryWrite(PriceStatement);
        }

        #endregion

        #region (private) TryPushPriceStatementsAsync(Connection, PriceStatements, CancellationToken)

        /// <summary>
        /// Push the given price statements; false when the other server does not take any.
        /// </summary>
        private async Task<Boolean> TryPushPriceStatementsAsync(Connection                                   Connection,
                                                                IReadOnlyList<RoamingNetworkPriceStatement>  PriceStatements,
                                                                CancellationToken                            CancellationToken)
        {

            var answer = await Connection.RequestAsync("addPriceStatements",
                                                       new JObject(new JProperty("priceStatements", new JArray(PriceStatements.Select(priceStatement => priceStatement.ToJSON())))),
                                                       CancellationToken);

            if (answer.IsError)
            {
                RaiseError($"The other server takes no price statements: {answer.ErrorDescription}", null);
                return false;
            }

            foreach (var priceStatement in PriceStatements)
                receivedPriceStatements.TryAdd(priceStatement.Digest, 0);

            if (answer.GetPayloadJSON() is JObject result && result["refused"]?.Value<Int32>() is Int32 refused && refused > 0)
                RaiseError($"The other server refused {refused} of {PriceStatements.Count} price statements: {result["refusals"]?.First?["description"]?.Value<String>()}", null);

            return true;

        }

        #endregion


        #region (private) TryGetCheckpointPriceStatementsAsync(Connection, Checkpoint, CancellationToken)

        /// <summary>
        /// The price statements the given checkpoint of the other server names, asked for when it names some and the peer
        /// follows price statements; null otherwise, or when they can not be had: the checkpoint is taken without them then.
        /// </summary>
        private async Task<IReadOnlyList<RoamingNetworkPriceStatement>?> TryGetCheckpointPriceStatementsAsync(Connection                Connection,
                                                                                                             RoamingNetworkCheckpoint  Checkpoint,
                                                                                                             CancellationToken         CancellationToken)
        {

            if (Checkpoint.PriceStatementsSize == 0 || !options.FollowPriceStatements)
                return null;

            var answer = await Connection.RequestAsync("checkpointPriceStatements",
                                                       new JObject(new JProperty("checkpointId", Checkpoint.Id.ToString())),
                                                       CancellationToken);

            if (answer.IsError)
            {
                RaiseError($"The other server tells not the price statements of the checkpoint '{Checkpoint.Id}': {answer.ErrorDescription}", null);
                return null;
            }

            var priceStatements = new List<RoamingNetworkPriceStatement>();

            foreach (var priceStatementJSON in answer.GetPayloadJSON()?["priceStatements"]?.OfType<JObject>() ?? [])
            {

                if (!RoamingNetworkPriceStatement.TryParse(priceStatementJSON, out var priceStatement, out var errorResponse))
                {
                    RaiseError($"Invalid price statement of the checkpoint '{Checkpoint.Id}': {errorResponse}", null);
                    return null;
                }

                receivedPriceStatements.TryAdd(priceStatement.Digest, 0);
                priceStatements.Add(priceStatement);

            }

            return priceStatements;

        }

        #endregion

    }

}
