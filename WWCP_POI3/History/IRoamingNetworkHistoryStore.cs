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

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Where a history keeps what it holds, so that it can be loaded again after a restart:
    /// a base, a snapshot of its root, and a log of what happened since, in its order.
    /// </summary>
    /// <remarks>
    /// The history writes commits and signatures before it shows them, and fails when the store fails.
    /// Status updates come often; a store may make them durable in batches.
    /// </remarks>
    public interface IRoamingNetworkHistoryStore : IDisposable
    {

        /// <summary>
        /// The base and the log entries since, or null when the store is empty.
        /// </summary>
        RoamingNetworkHistoryStoreContent? Load();

        /// <summary>
        /// Start an empty store with the given base: a snapshot of the root of a history,
        /// as RoamingNetworkHistory.ToSnapshotJSON writes it.
        /// </summary>
        /// <param name="Snapshot">The snapshot of the root.</param>
        void WriteBase(JObject Snapshot);

        /// <summary>
        /// Append the given entry to the log, durably for commits and signatures.
        /// </summary>
        /// <param name="Entry">The log entry.</param>
        void Append(RoamingNetworkHistoryLogEntry Entry);

        /// <summary>
        /// Replace the base and the log at once, e.g. after the history dropped its older commits:
        /// either all of the new content is there afterwards, or the old one is still.
        /// </summary>
        /// <param name="Snapshot">The snapshot of the new root.</param>
        /// <param name="Entries">The log entries since the new root.</param>
        void Compact(JObject                                     Snapshot,
                     IEnumerable<RoamingNetworkHistoryLogEntry>  Entries);

    }


    /// <summary>
    /// What a store holds: the base and the log entries since, in their order.
    /// </summary>
    /// <param name="Base">The snapshot of the root.</param>
    /// <param name="Entries">The log entries.</param>
    public sealed record RoamingNetworkHistoryStoreContent(JObject                                       Base,
                                                           IReadOnlyList<RoamingNetworkHistoryLogEntry>  Entries);


    /// <summary>
    /// The kind of a log entry.
    /// </summary>
    public enum RoamingNetworkHistoryLogEntryType
    {

        /// <summary>
        /// A commit the history took.
        /// </summary>
        Commit,

        /// <summary>
        /// Signatures added to a commit later.
        /// </summary>
        Signatures,

        /// <summary>
        /// A change of the runtime status.
        /// </summary>
        Status,

        /// <summary>
        /// A checkpoint the history took.
        /// </summary>
        Checkpoint,

        /// <summary>
        /// Signatures added to a checkpoint later.
        /// </summary>
        CheckpointSignatures

    }


    /// <summary>
    /// One entry of the log of a history: a commit, signatures added to one, a status update, a checkpoint,
    /// or signatures added to one.
    /// One line of JSON in a file: { type, commit } | { type, commitId, signatures } | { type, statusUpdate } |
    /// { type, checkpoint } | { type, checkpointId, signatures }.
    /// </summary>
    public sealed class RoamingNetworkHistoryLogEntry
    {

        #region Properties

        /// <summary>
        /// The kind of the entry.
        /// </summary>
        public RoamingNetworkHistoryLogEntryType                Type            { get; }

        /// <summary>
        /// The commit of a Commit entry.
        /// </summary>
        public RoamingNetworkCommit?                            Commit          { get; }

        /// <summary>
        /// The commit of a Signatures entry.
        /// </summary>
        public RoamingNetworkCommit_Id?                         CommitId        { get; }

        /// <summary>
        /// The signatures of a Signatures entry.
        /// </summary>
        public IReadOnlyList<RoamingNetworkCommitSignature>     Signatures      { get; }

        /// <summary>
        /// The status update of a Status entry.
        /// </summary>
        public POIStatusUpdate?                                 StatusUpdate    { get; }

        /// <summary>
        /// The checkpoint of a Checkpoint entry.
        /// </summary>
        public RoamingNetworkCheckpoint?                        Checkpoint      { get; }

        /// <summary>
        /// The checkpoint of a CheckpointSignatures entry.
        /// </summary>
        public RoamingNetworkCheckpoint_Id?                     CheckpointId    { get; }

        #endregion

        #region Constructor(s)

        private RoamingNetworkHistoryLogEntry(RoamingNetworkHistoryLogEntryType             Type,
                                              RoamingNetworkCommit?                         Commit         = null,
                                              RoamingNetworkCommit_Id?                      CommitId       = null,
                                              IReadOnlyList<RoamingNetworkCommitSignature>? Signatures     = null,
                                              POIStatusUpdate?                              StatusUpdate   = null,
                                              RoamingNetworkCheckpoint?                     Checkpoint     = null,
                                              RoamingNetworkCheckpoint_Id?                  CheckpointId   = null)
        {
            this.Type          = Type;
            this.Commit        = Commit;
            this.CommitId      = CommitId;
            this.Signatures    = Signatures ?? [];
            this.StatusUpdate  = StatusUpdate;
            this.Checkpoint    = Checkpoint;
            this.CheckpointId  = CheckpointId;
        }

        /// <summary>
        /// The entry of the given commit.
        /// </summary>
        public static RoamingNetworkHistoryLogEntry ForCommit(RoamingNetworkCommit Commit)
            => new (RoamingNetworkHistoryLogEntryType.Commit, Commit: Commit);

        /// <summary>
        /// The entry of the given signatures added to the given commit.
        /// </summary>
        public static RoamingNetworkHistoryLogEntry ForSignatures(RoamingNetworkCommit_Id                       CommitId,
                                                                  IReadOnlyList<RoamingNetworkCommitSignature>  Signatures)
            => new (RoamingNetworkHistoryLogEntryType.Signatures, CommitId: CommitId, Signatures: Signatures);

        /// <summary>
        /// The entry of the given status update.
        /// </summary>
        public static RoamingNetworkHistoryLogEntry ForStatus(POIStatusUpdate StatusUpdate)
            => new (RoamingNetworkHistoryLogEntryType.Status, StatusUpdate: StatusUpdate);

        /// <summary>
        /// The entry of the given checkpoint.
        /// </summary>
        public static RoamingNetworkHistoryLogEntry ForCheckpoint(RoamingNetworkCheckpoint Checkpoint)
            => new (RoamingNetworkHistoryLogEntryType.Checkpoint, Checkpoint: Checkpoint);

        /// <summary>
        /// The entry of the given signatures added to the given checkpoint.
        /// </summary>
        public static RoamingNetworkHistoryLogEntry ForCheckpointSignatures(RoamingNetworkCheckpoint_Id                   CheckpointId,
                                                                           IReadOnlyList<RoamingNetworkCommitSignature>  Signatures)
            => new (RoamingNetworkHistoryLogEntryType.CheckpointSignatures, Signatures: Signatures, CheckpointId: CheckpointId);

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this entry.
        /// </summary>
        public JObject ToJSON()

            => Type switch {

                   RoamingNetworkHistoryLogEntryType.Commit      => new JObject(
                                                                        new JProperty("type",          "commit"),
                                                                        new JProperty("commit",        Commit!.ToJSON())
                                                                    ),

                   RoamingNetworkHistoryLogEntryType.Signatures  => new JObject(
                                                                        new JProperty("type",          "signatures"),
                                                                        new JProperty("commitId",      CommitId!.Value.ToString()),
                                                                        new JProperty("signatures",    new JArray(Signatures.Select(signature => signature.ToJSON())))
                                                                    ),

                   RoamingNetworkHistoryLogEntryType.Checkpoint  => new JObject(
                                                                        new JProperty("type",          "checkpoint"),
                                                                        new JProperty("checkpoint",    Checkpoint!.ToJSON())
                                                                    ),

                   RoamingNetworkHistoryLogEntryType.CheckpointSignatures
                                                                 => new JObject(
                                                                        new JProperty("type",          "checkpointSignatures"),
                                                                        new JProperty("checkpointId",  CheckpointId!.Value.ToString()),
                                                                        new JProperty("signatures",    new JArray(Signatures.Select(signature => signature.ToJSON())))
                                                                    ),

                   _                                             => new JObject(
                                                                        new JProperty("type",          "status"),
                                                                        new JProperty("statusUpdate",  StatusUpdate!.ToJSON())
                                                                    )

               };

        #endregion

        #region (static) TryParse(JSON, out Entry, out ErrorResponse, Limits = null)

        /// <summary>
        /// Try to parse the given JSON representation of a log entry.
        /// </summary>
        public static Boolean TryParse(JObject                                                   JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkHistoryLogEntry?   Entry,
                                       [NotNullWhen(false)] out String?                          ErrorResponse,
                                       RoamingNetworkChangeSetLimits?                            Limits   = null)
        {

            Entry = null;

            switch (JSON["type"]?.Value<String>())
            {

                case "commit":

                    if (JSON["commit"] is not JObject commitJSON ||
                        !RoamingNetworkCommit.TryParse(commitJSON, out var commit, out ErrorResponse, Limits))
                    {
                        ErrorResponse = "Invalid commit entry!";
                        return false;
                    }

                    Entry = ForCommit(commit);
                    return true;

                case "signatures":

                    if (!RoamingNetworkCommitSignature.TryParseCommitSignatures(JSON, out var commitId, out var signatures, out ErrorResponse))
                        return false;

                    Entry = ForSignatures(commitId, signatures);
                    return true;

                case "status":

                    if (JSON["statusUpdate"] is not JObject statusJSON ||
                        !POIStatusUpdate.TryParse(statusJSON, out var statusUpdate, out ErrorResponse))
                    {
                        ErrorResponse = "Invalid status entry!";
                        return false;
                    }

                    Entry = ForStatus(statusUpdate);
                    return true;

                case "checkpoint":

                    if (JSON["checkpoint"] is not JObject checkpointJSON ||
                        !RoamingNetworkCheckpoint.TryParse(checkpointJSON, out var checkpoint, out ErrorResponse))
                    {
                        ErrorResponse = "Invalid checkpoint entry!";
                        return false;
                    }

                    Entry = ForCheckpoint(checkpoint);
                    return true;

                case "checkpointSignatures":

                    if (!RoamingNetworkCheckpoint_Id.TryParse(JSON["checkpointId"]?.Value<String>() ?? "", out var checkpointId))
                    {
                        ErrorResponse = "Invalid checkpoint signatures entry!";
                        return false;
                    }

                    // The same signatures as of a commit, of another checkpoint than one named by its commit.
                    if (!RoamingNetworkCommitSignature.TryParseCommitSignatures(new JObject(
                                                                                    new JProperty("commitId",    checkpointId.ToString()),
                                                                                    new JProperty("signatures",  JSON["signatures"])
                                                                                ),
                                                                                out _,
                                                                                out var checkpointSignatures,
                                                                                out ErrorResponse))
                    {
                        return false;
                    }

                    Entry = ForCheckpointSignatures(checkpointId, checkpointSignatures);
                    return true;

                default:
                    ErrorResponse = $"Unknown type of log entry '{JSON["type"]}'!";
                    return false;

            }

        }

        #endregion

    }

}
