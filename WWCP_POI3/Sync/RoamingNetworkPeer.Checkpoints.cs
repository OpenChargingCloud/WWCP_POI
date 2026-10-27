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

                    var payload = new JObject(new JProperty(POI_API.PropertyKeys.Max, options.MaxCommitsPerRequest));

                    if (since.HasValue)
                        payload.Add(POI_API.PropertyKeys.Since, since.Value);

                    var answer = await Connection.RequestAsync("checkpoints", payload, CancellationToken);

                    if (answer.IsError)
                    {
                        RaiseError($"The other server tells no checkpoints: {answer.ErrorDescription}", null);
                        return;
                    }

                    var checkpoints = answer.GetPayloadJSON()?[POI_API.PropertyKeys.Checkpoints]?.OfType<JObject>().ToArray() ?? [];

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

                        if (!TakeCheckpoint(checkpoint, await TryGetCheckpointPriceStatementsAsync(Connection, checkpoint, CancellationToken)))
                            return;

                    }

                }

            }

            if (options.PushCheckpoints)
            {

                // Only those after its latest one: another one of a sequence it has is a split view to it.
                var answer = await Connection.RequestAsync("checkpoints", new JObject(new JProperty(POI_API.PropertyKeys.Latest, true)), CancellationToken);

                if (answer.IsError)
                {
                    RaiseError($"The other server tells no checkpoints: {answer.ErrorDescription}", null);
                    return;
                }

                UInt64? theirLatest = answer.GetPayloadJSON()?[POI_API.PropertyKeys.Checkpoints]?.OfType<JObject>().FirstOrDefault()?[RoamingNetworkCheckpoint.PropertyKeys.Sequence]?.Value<UInt64>();

                var missing = currentHistory.GetCheckpointsSince(theirLatest, Int32.MaxValue).
                                             Where(checkpoint => !receivedCheckpoints.ContainsKey(checkpoint.Id)).
                                             ToArray();

                if (missing.Length > 0)
                    await TryPushCheckpointsAsync(Connection, missing, CancellationToken);

            }

        }

        #endregion

        #region (private) TryJoinFromCheckpointAsync(Connection, TrustAnchor, CancellationToken)

        /// <summary>
        /// A new history from the version of the latest checkpoint of the other server carrying the signatures required, or
        /// of the latest one before that does, with the price statements it names (SPECIFICATION.md, section 10.5), and that
        /// checkpoint; it must be signed as the given trust anchor requires: in the role of the roaming network operator by
        /// signing keys of as many holders as the trust anchor and the key statement of its version require, each certified
        /// by an identity key the trust anchor names. Nothing when the peer does not join from checkpoints, or there is no
        /// such checkpoint: it starts from the head then.
        /// </summary>
        private async Task<(RoamingNetworkHistory? History, RoamingNetworkCheckpoint? Checkpoint)> TryJoinFromCheckpointAsync(Connection                 Connection,
                                                                                                                               RoamingNetworkTrustAnchor  TrustAnchor,
                                                                                                                               CancellationToken          CancellationToken)
        {

            if (!options.JoinFromCheckpoint || !options.FollowCheckpoints || options.VerifyCheckpointSignature is not VerifyCheckpointSignatureDelegate verify)
                return (null, null);

            var latestAnswer = await Connection.RequestAsync("checkpoints", new JObject(new JProperty(POI_API.PropertyKeys.Latest, true)), CancellationToken);

            if (latestAnswer.IsError ||
                latestAnswer.GetPayloadJSON()?[POI_API.PropertyKeys.Checkpoints]?.OfType<JObject>().FirstOrDefault() is not JObject latestJSON ||
                !RoamingNetworkCheckpoint.TryParse(latestJSON, out var latest, out _))
            {
                return (null, null);
            }

            // The latest one first; when it does not carry the signatures required, e.g. of witnesses not there yet, those before.
            var candidates = new List<RoamingNetworkCheckpoint> { latest };

            if (latest.Sequence > 0 && !CarriesRequiredSignatures(latest, verify))
            {

                var max      = (UInt64) Math.Max(options.MaxCommitsPerRequest, 1);
                var payload  = new JObject(new JProperty(POI_API.PropertyKeys.Max, max));

                if (latest.Sequence > max)
                    payload.Add(POI_API.PropertyKeys.Since, latest.Sequence - max - 1);

                var before   = await Connection.RequestAsync("checkpoints", payload, CancellationToken);

                foreach (var checkpointJSON in before.IsError ? [] : before.GetPayloadJSON()?[POI_API.PropertyKeys.Checkpoints]?.OfType<JObject>() ?? [])
                    if (RoamingNetworkCheckpoint.TryParse(checkpointJSON, out var checkpoint, out _) && checkpoint.Sequence < latest.Sequence)
                        candidates.Add(checkpoint);

                candidates = [ .. candidates.OrderByDescending(checkpoint => checkpoint.Sequence) ];

            }

            foreach (var checkpoint in candidates)
            {

                if (!CarriesRequiredSignatures(checkpoint, verify))
                    continue;

                var snapshot = await Connection.RequestAsync("checkpointSnapshot",
                                                             new JObject(new JProperty(POI_API.PropertyKeys.CheckpointId, checkpoint.Id.ToString())),
                                                             CancellationToken);

                if (snapshot.IsError || snapshot.GetPayloadJSON() is not JObject snapshotJSON)
                {
                    RaiseError($"The other server gives no snapshot of the checkpoint '{checkpoint.Id}': {snapshot.ErrorDescription}", null);
                    continue;
                }

                var priceStatements = await TryGetCheckpointPriceStatementsAsync(Connection, checkpoint, CancellationToken);

                // Signed by the roaming network operator as the trust anchor requires, with the keys its version gives.
                if (!RoamingNetworkHistory.TryFromAnchoredCheckpointSnapshot(snapshotJSON,
                                                                             TrustAnchor,
                                                                             out var joined,
                                                                             out var errorResponse,
                                                                             verify,
                                                                             options.RequiredCheckpointSignatureRoles,
                                                                             priceStatements,
                                                                             Limits: options.ChangeSetLimits))
                {
                    RaiseError($"The checkpoint '{checkpoint.Id}' of the other server is not anchored in the trust anchor of the roaming network '{TrustAnchor.Id}': {errorResponse}", null);
                    continue;
                }

                if (joined.Checkpoints[0].Id != checkpoint.Id)
                {
                    RaiseError($"The other server gives the snapshot of the checkpoint '{joined.Checkpoints[0].Id}' for '{checkpoint.Id}'!", null);
                    continue;
                }

                // From there: not to be pushed back.
                receivedCheckpoints.TryAdd(checkpoint.Id, 0);

                foreach (var signature in checkpoint.Signatures)
                    receivedCheckpointSignatures.TryAdd((checkpoint.Id, signature), 0);

                return (joined, checkpoint);

            }

            return (null, null);

        }

        #endregion

        #region (private) WhyNotFollowingAsync(Connection, Current, New, Checkpoint, CancellationToken)

        /// <summary>
        /// Why the given new history of the other server is not proven to follow the head and every tip of the given one held
        /// here, or null: each must be a commit the new one holds, or one of the commits of the checkpoint it joined from or
        /// of one before it in its line, as the proof of the commit there tells (SPECIFICATION.md, sections 10.4 and 10.5),
        /// every checkpoint between named by the one after it, as its previousId. A snapshot of the head proves nothing of the
        /// commits before it, and a commit not pushed yet is known nowhere else: neither is proven to be followed.
        /// </summary>
        private async Task<String?> WhyNotFollowingAsync(Connection                 Connection,
                                                         RoamingNetworkHistory      Current,
                                                         RoamingNetworkHistory      New,
                                                         RoamingNetworkCheckpoint?  Checkpoint,
                                                         CancellationToken          CancellationToken)
        {

            var unknown = Current.Tips.Select(tip => tip.Commit.Id).
                                       Append(Current.Head.Commit.Id).
                                       Distinct().
                                       Where (commitId => !New.Contains(commitId)).
                                       ToArray();

            if (unknown.Length == 0)
                return null;

            if (Checkpoint is null)
                return $"The other server knows none of the commits of this history, and its snapshot of the head '{New.Head.Commit.Id}' does not prove to follow the head '{Current.Head.Commit.Id}' held here: " +
                       "it is kept, with its commits not pushed yet. Joining from checkpoints proves it, a reset replaces it regardless.";

            // The line of checkpoints up to the one joined from, as far as it is checked: each named by the one after it.
            var line = new Dictionary<RoamingNetworkCheckpoint_Id, RoamingNetworkCheckpoint> { { Checkpoint.Id, Checkpoint } };

            foreach (var commitId in unknown)
            {

                var proofAnswer = await Connection.RequestAsync("commitProof",
                                                                new JObject(new JProperty(RoamingNetworkCommitSignature.PropertyKeys.CommitId, commitId.ToString())),
                                                                CancellationToken);

                if (proofAnswer.IsError ||
                    proofAnswer.GetPayloadJSON() is not JObject proofJSON ||
                    !RoamingNetworkCheckpointProof.TryParse(proofJSON, out var proof, out _) ||
                    proof.CommitId != commitId)
                {
                    return $"The other server proves not that the commit '{commitId}' held here is one of its checkpoints: it is kept, with its commits not pushed yet. {proofAnswer.ErrorDescription}".TrimEnd();
                }

                if (!line.TryGetValue(proof.CheckpointId, out var ofCommit))
                {

                    var checkpointAnswer = await Connection.RequestAsync("checkpoint",
                                                                         new JObject(new JProperty(POI_API.PropertyKeys.CheckpointId, proof.CheckpointId.ToString())),
                                                                         CancellationToken);

                    if (checkpointAnswer.IsError ||
                        checkpointAnswer.GetPayloadJSON() is not JObject checkpointJSON ||
                        !RoamingNetworkCheckpoint.TryParse(checkpointJSON, out ofCommit, out _) ||
                        ofCommit.Id    != proof.CheckpointId ||
                        ofCommit.Epoch != Checkpoint.Epoch   ||
                        ofCommit.Sequence > Checkpoint.Sequence)
                    {
                        return $"The checkpoint '{proof.CheckpointId}' of the commit '{commitId}' held here is none of the line of the checkpoint '{Checkpoint.Id}'!";
                    }

                    // From it up to the one joined from, each checkpoint naming the one before.
                    var between   = new List<RoamingNetworkCheckpoint> { ofCommit };
                    var previous  = ofCommit;

                    while (previous.Sequence < Checkpoint.Sequence)
                    {

                        var pageAnswer = await Connection.RequestAsync("checkpoints",
                                                                       new JObject(
                                                                           new JProperty(POI_API.PropertyKeys.Since,  previous.Sequence),
                                                                           new JProperty(POI_API.PropertyKeys.Max,    Math.Max(options.MaxCommitsPerRequest, 1))
                                                                       ),
                                                                       CancellationToken);

                        var page = pageAnswer.IsError ? [] : pageAnswer.GetPayloadJSON()?[POI_API.PropertyKeys.Checkpoints]?.OfType<JObject>().ToArray() ?? [];

                        if (page.Length == 0)
                            return $"The other server gives not the line of checkpoints from '{ofCommit.Id}' to '{Checkpoint.Id}'!";

                        foreach (var nextJSON in page)
                        {

                            if (!RoamingNetworkCheckpoint.TryParse(nextJSON, out var next, out _) ||
                                next.Sequence   != previous.Sequence + 1 ||
                                next.PreviousId != previous.Id)
                            {
                                return $"The checkpoint after '{previous.Id}' of the other server does not name it before it!";
                            }

                            previous = next;
                            between.Add(next);

                            if (next.Sequence == Checkpoint.Sequence)
                                break;

                        }

                    }

                    if (previous.Id != Checkpoint.Id)
                        return $"The line of checkpoints from '{ofCommit.Id}' of the other server ends in '{previous.Id}', not in '{Checkpoint.Id}'!";

                    foreach (var checkedCheckpoint in between)
                        line[checkedCheckpoint.Id] = checkedCheckpoint;

                }

                if (!proof.Verify(ofCommit, out var proofError))
                    return $"The proof of the commit '{commitId}' held here in the checkpoint '{ofCommit.Id}' is invalid: {proofError}";

            }

            return null;

        }

        #endregion

        #region (private) CarriesRequiredSignatures(Checkpoint, Verify)

        /// <summary>
        /// Whether every signature of the given checkpoint is valid and it is signed in every role required.
        /// </summary>
        private Boolean CarriesRequiredSignatures(RoamingNetworkCheckpoint           Checkpoint,
                                                  VerifyCheckpointSignatureDelegate  Verify)

            => Checkpoint.Signatures.All(signature => Verify(Checkpoint, signature)) &&
               (options.RequiredCheckpointSignatureRoles ?? []).All(role => Checkpoint.Signatures.Any(signature => signature.Role == role));

        #endregion

        #region (private) TakeCheckpointAsync(Connection, CheckpointJSON, CancellationToken)

        /// <summary>
        /// Take a checkpoint of the other server, with the price statements it names; one this history refuses is told by
        /// an error.
        /// </summary>
        private async Task<Boolean> TakeCheckpointAsync(Connection         Connection,
                                                        JObject            CheckpointJSON,
                                                        CancellationToken  CancellationToken)
        {

            if (!RoamingNetworkCheckpoint.TryParse(CheckpointJSON, out var checkpoint, out var errorResponse))
            {
                RaiseError("Invalid checkpoint: " + errorResponse, null);
                return false;
            }

            return TakeCheckpoint(checkpoint, await TryGetCheckpointPriceStatementsAsync(Connection, checkpoint, CancellationToken));

        }

        private Boolean TakeCheckpoint(RoamingNetworkCheckpoint                      Checkpoint,
                                       IReadOnlyList<RoamingNetworkPriceStatement>?  PriceStatements)
        {

            var checkpoint = Checkpoint;

            receivedCheckpoints.TryAdd(checkpoint.Id, 0);

            foreach (var signature in checkpoint.Signatures)
                receivedCheckpointSignatures.TryAdd((checkpoint.Id, signature), 0);

            if (!History!.TryAddCheckpoint(checkpoint, out var errorResponse, options.VerifyCheckpointSignature, options.RequiredCheckpointSignatureRoles, PriceStatements))
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

            if (!RoamingNetworkCheckpoint_Id.TryParse(SignaturesJSON[POI_API.PropertyKeys.CheckpointId]?.Value<String>() ?? "", out var checkpointId) ||
                !RoamingNetworkCommitSignature.TryParseCommitSignatures(new JObject(
                                                                            new JProperty(RoamingNetworkCommitSignature.PropertyKeys.CommitId,    checkpointId.ToString()),
                                                                            new JProperty(RoamingNetworkCommitSignature.PropertyKeys.Signatures,  SignaturesJSON[RoamingNetworkCommitSignature.PropertyKeys.Signatures])
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

            var currentHistory   = History!;
            var priceStatements  = new JArray(Checkpoints.Select(checkpoint => (checkpoint.Id, PriceStatements: currentHistory.PriceStatementsOf(checkpoint.Id))).
                                                          Where (ofCheckpoint => ofCheckpoint.PriceStatements is not null).
                                                          Select(ofCheckpoint => new JObject(
                                                                                     new JProperty(POI_API.PropertyKeys.CheckpointId,     ofCheckpoint.Id.ToString()),
                                                                                     new JProperty(POI_API.PropertyKeys.PriceStatements,  new JArray(ofCheckpoint.PriceStatements!.Select(priceStatement => priceStatement.ToJSON(Embedded: true))))
                                                                                 )));

            var payload          = new JObject(new JProperty(POI_API.PropertyKeys.Checkpoints, new JArray(Checkpoints.Select(checkpoint => checkpoint.ToJSON(Embedded: true)))));

            // With the price statements they name, so that the other server checks them as well.
            if (priceStatements.Count > 0)
                payload.Add(POI_API.PropertyKeys.PriceStatements, priceStatements);

            var answer = await Connection.RequestAsync("pushCheckpoints",
                                                       payload,
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
                                                           new JProperty(POI_API.PropertyKeys.CheckpointId,  Push.CheckpointId.ToString()),
                                                           new JProperty(RoamingNetworkCommitSignature.PropertyKeys.Signatures,    new JArray(Push.Signatures.Select(signature => signature.ToJSON(Embedded: true))))
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
                                new JProperty(POI_API.PropertyKeys.CheckpointId,  CheckpointId.ToString()),
                                new JProperty(RoamingNetworkCommitSignature.PropertyKeys.Signatures,    new JArray(Signatures.Select(signature => signature.ToJSON(Embedded: true))))
                            ),
                            CancellationToken);

        #endregion

    }

}
