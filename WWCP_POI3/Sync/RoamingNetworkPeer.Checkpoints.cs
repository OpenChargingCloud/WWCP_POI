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

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The checkpoints of a peer: taken from the other server, checked against this history, and pushed to it,
    /// e.g. those the roaming network operator makes here, and the signatures of witnesses (SPECIFICATION.md, section 10).
    /// </summary>
    public sealed partial class RoamingNetworkPeer
    {

        #region Data

        /// <summary>
        /// New signatures of a checkpoint to push.
        /// </summary>
        private sealed record CheckpointSignaturesPush(RoamingNetworkCheckpoint_Id                   CheckpointId,
                                                       IReadOnlyList<RoamingNetworkCommitSignature>  Signatures);

        private readonly ConcurrentDictionary<RoamingNetworkCheckpoint_Id, Byte>                                   receivedCheckpoints           = [];
        private readonly ConcurrentDictionary<(RoamingNetworkCheckpoint_Id, RoamingNetworkCommitSignature), Byte>  receivedCheckpointSignatures  = [];

        #endregion


        #region (private) SynchronizeCheckpointsAsync(Connection, CancellationToken)

        /// <summary>
        /// Take the checkpoints of the other server since the latest one here, then push those it misses.
        /// A server without checkpoints is told so once, by an error.
        /// </summary>
        private async Task SynchronizeCheckpointsAsync(Connection         Connection,
                                                       CancellationToken  CancellationToken)
        {

            var currentHistory  = History!;

            if (options.FollowCheckpoints)
            {

                var since = currentHistory.LatestCheckpoint?.Sequence;

                while (true)
                {

                    var payload = new JObject(new JProperty("max", options.MaxCommitsPerRequest));

                    if (since.HasValue)
                        payload.Add("since", since.Value);

                    var answer = await Connection.RequestAsync("checkpoints", payload, CancellationToken);

                    if (answer.IsError)
                    {
                        RaiseError($"The other server tells no checkpoints: {answer.ErrorDescription}", null);
                        return;
                    }

                    var checkpoints = answer.GetPayloadJSON()?["checkpoints"]?.OfType<JObject>().ToArray() ?? [];

                    if (checkpoints.Length == 0)
                        break;

                    foreach (var checkpointJSON in checkpoints)
                    {

                        if (!RoamingNetworkCheckpoint.TryParse(checkpointJSON, out var checkpoint, out var errorResponse))
                        {
                            RaiseError("Invalid checkpoint: " + errorResponse, null);
                            return;
                        }

                        since = checkpoint.Sequence;

                        // A history started from a snapshot begins its line with the first checkpoint of a commit it holds.
                        if (currentHistory.LatestCheckpoint is null && !currentHistory.Contains(checkpoint.CommitId))
                            continue;

                        if (!TakeCheckpoint(checkpoint))
                            return;

                    }

                }

            }

            if (options.PushCheckpoints)
            {

                // Only those after its latest one: another one of a sequence it has is a split view to it.
                var answer = await Connection.RequestAsync("checkpoints", new JObject(new JProperty("latest", true)), CancellationToken);

                if (answer.IsError)
                {
                    RaiseError($"The other server tells no checkpoints: {answer.ErrorDescription}", null);
                    return;
                }

                UInt64? theirLatest = answer.GetPayloadJSON()?["checkpoints"]?.OfType<JObject>().FirstOrDefault()?["sequence"]?.Value<UInt64>();

                var missing = currentHistory.GetCheckpointsSince(theirLatest, Int32.MaxValue).
                                             Where(checkpoint => !receivedCheckpoints.ContainsKey(checkpoint.Id)).
                                             ToArray();

                if (missing.Length > 0)
                    await TryPushCheckpointsAsync(Connection, missing, CancellationToken);

            }

        }

        #endregion

        #region (private) TakeCheckpoint(CheckpointJSON)

        /// <summary>
        /// Take a checkpoint of the other server; one this history refuses is told by an error.
        /// </summary>
        private Boolean TakeCheckpoint(JObject CheckpointJSON)
        {

            if (!RoamingNetworkCheckpoint.TryParse(CheckpointJSON, out var checkpoint, out var errorResponse))
            {
                RaiseError("Invalid checkpoint: " + errorResponse, null);
                return false;
            }

            return TakeCheckpoint(checkpoint);

        }

        private Boolean TakeCheckpoint(RoamingNetworkCheckpoint Checkpoint)
        {

            var checkpoint = Checkpoint;

            receivedCheckpoints.TryAdd(checkpoint.Id, 0);

            foreach (var signature in checkpoint.Signatures)
                receivedCheckpointSignatures.TryAdd((checkpoint.Id, signature), 0);

            if (!History!.TryAddCheckpoint(checkpoint, out var errorResponse, options.VerifyCheckpointSignature, options.RequiredCheckpointSignatureRoles))
            {
                RaiseError("The checkpoint of the other server can not be taken: " + errorResponse, null);
                return false;
            }

            return true;

        }

        #endregion

        #region (private) TakeCheckpointSignatures(SignaturesJSON)

        private void TakeCheckpointSignatures(JObject SignaturesJSON)
        {

            if (!RoamingNetworkCheckpoint_Id.TryParse(SignaturesJSON["checkpointId"]?.Value<String>() ?? "", out var checkpointId) ||
                !RoamingNetworkCommitSignature.TryParseCommitSignatures(new JObject(
                                                                            new JProperty("commitId",    checkpointId.ToString()),
                                                                            new JProperty("signatures",  SignaturesJSON["signatures"])
                                                                        ),
                                                                        out _,
                                                                        out var signatures,
                                                                        out var errorResponse))
            {
                RaiseError("Invalid checkpoint signatures!", null);
                return;
            }

            foreach (var signature in signatures)
                receivedCheckpointSignatures.TryAdd((checkpointId, signature), 0);

            var currentHistory = History!;

            if (currentHistory.TryGetCheckpoint(checkpointId, out _) &&
                !currentHistory.TryAddCheckpointSignatures(checkpointId, signatures, options.VerifyCheckpointSignature, out _, out _, out errorResponse))
            {
                RaiseError("The checkpoint signatures of the other server can not be taken: " + errorResponse, null);
            }

        }

        #endregion

        #region (private) WatchCheckpoints(History, Watch)

        /// <summary>
        /// Follow the new checkpoints and checkpoint signatures of the given history, to push them, or stop following them.
        /// </summary>
        private void WatchCheckpoints(RoamingNetworkHistory  History,
                                      Boolean                Watch)
        {

            if (!options.PushCheckpoints)
                return;

            if (Watch)
            {
                History.OnCheckpoint            += CheckpointMade;
                History.OnCheckpointSignatures  += CheckpointSigned;
            }
            else
            {
                History.OnCheckpoint            -= CheckpointMade;
                History.OnCheckpointSignatures  -= CheckpointSigned;
            }

        }

        private void CheckpointMade(RoamingNetworkCheckpoint Checkpoint)
        {
            if (!receivedCheckpoints.ContainsKey(Checkpoint.Id))
                Volatile.Read(ref outgoing)?.Writer.TryWrite(Checkpoint);
        }

        private void CheckpointSigned(RoamingNetworkCheckpoint                      Checkpoint,
                                      IReadOnlyList<RoamingNetworkCommitSignature>  NewSignatures)
        {

            var notReceived = NewSignatures.Where(signature => !receivedCheckpointSignatures.ContainsKey((Checkpoint.Id, signature))).ToArray();

            if (notReceived.Length > 0)
                Volatile.Read(ref outgoing)?.Writer.TryWrite(new CheckpointSignaturesPush(Checkpoint.Id, notReceived));

        }

        #endregion

        #region (private) TryPushCheckpointsAsync(Connection, Checkpoints, CancellationToken)

        /// <summary>
        /// Push the given checkpoints; false when the other server does not allow it.
        /// </summary>
        private async Task<Boolean> TryPushCheckpointsAsync(Connection                               Connection,
                                                            IReadOnlyList<RoamingNetworkCheckpoint>  Checkpoints,
                                                            CancellationToken                        CancellationToken)
        {

            var answer = await Connection.RequestAsync("pushCheckpoints",
                                                       new JObject(new JProperty("checkpoints", new JArray(Checkpoints.Select(checkpoint => checkpoint.ToJSON())))),
                                                       CancellationToken);

            if (answer.IsError)
            {
                RaiseError($"The other server takes no checkpoints: {answer.ErrorDescription}", null);
                return false;
            }

            return true;

        }

        #endregion

        #region (private) TryPushCheckpointSignaturesAsync(Connection, Push, CancellationToken)

        /// <summary>
        /// Push the given signatures of a checkpoint, e.g. of a witness; false when the other server does not allow it.
        /// </summary>
        private async Task<Boolean> TryPushCheckpointSignaturesAsync(Connection                Connection,
                                                                     CheckpointSignaturesPush  Push,
                                                                     CancellationToken         CancellationToken)
        {

            var answer = await Connection.RequestAsync("signCheckpoint",
                                                       new JObject(
                                                           new JProperty("checkpointId",  Push.CheckpointId.ToString()),
                                                           new JProperty("signatures",    new JArray(Push.Signatures.Select(signature => signature.ToJSON())))
                                                       ),
                                                       CancellationToken);

            if (answer.IsError)
            {
                RaiseError($"The other server takes no checkpoint signatures: {answer.ErrorDescription}", null);
                return false;
            }

            return true;

        }

        #endregion

        #region SignCheckpointAsync(CheckpointId, Signatures, CancellationToken = default)

        /// <summary>
        /// Send signatures of a checkpoint to the other server, e.g. of a witness that checked it.
        /// </summary>
        /// <returns>The answer: { checkpointId, signatures, added }.</returns>
        public Task<JObject> SignCheckpointAsync(RoamingNetworkCheckpoint_Id                 CheckpointId,
                                                 IEnumerable<RoamingNetworkCommitSignature>  Signatures,
                                                 CancellationToken                           CancellationToken   = default)

            => RequestAsync("signCheckpoint",
                            new JObject(
                                new JProperty("checkpointId",  CheckpointId.ToString()),
                                new JProperty("signatures",    new JArray(Signatures.Select(signature => signature.ToJSON())))
                            ),
                            CancellationToken);

        #endregion

    }

}
