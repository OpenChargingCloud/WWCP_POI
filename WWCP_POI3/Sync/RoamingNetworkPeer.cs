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
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The peer has caught up with the other server, after connecting.
    /// </summary>
    /// <param name="Follower">The peer.</param>
    /// <param name="History">Its history.</param>
    public delegate void OnRoamingNetworkPeerSynchronizedDelegate(RoamingNetworkPeer  Follower,
                                                                      RoamingNetworkHistory   History);

    /// <summary>
    /// The peer started its history from a snapshot of the other server: at first, or because the other server
    /// knew none of its commits.
    /// </summary>
    /// <param name="Follower">The peer.</param>
    /// <param name="OldHistory">The history before, if any.</param>
    /// <param name="NewHistory">The new history.</param>
    public delegate void OnRoamingNetworkPeerHistoryReplacedDelegate(RoamingNetworkPeer   Follower,
                                                                         RoamingNetworkHistory?   OldHistory,
                                                                         RoamingNetworkHistory    NewHistory);

    /// <summary>
    /// Something went wrong; the peer connects again.
    /// </summary>
    /// <param name="Follower">The peer.</param>
    /// <param name="Description">What went wrong.</param>
    /// <param name="Exception">The exception, if any.</param>
    public delegate void OnRoamingNetworkPeerErrorDelegate(RoamingNetworkPeer  Follower,
                                                               String                  Description,
                                                               Exception?              Exception);


    /// <summary>
    /// Keeps the history of a roaming network in sync with another server, over the WebSocket of its history:
    /// there is no leader, both commit, and both merge alike.
    /// </summary>
    /// <remarks>
    /// After connecting it subscribes first, so that nothing gets lost, then starts from a snapshot when it has
    /// no history yet, fetches the commits it misses, naming those it holds, and takes the current runtime status:
    /// from the snapshot, or asked for when it continued its history. Then it pushes the commits the other server
    /// misses, the status it has differently, and every new commit and status update of its own history, if allowed there,
    /// in their order; nothing that came from the other server goes back. The latest status of an entity wins everywhere.
    /// Afterwards it appends the pushed commits; one whose parents it misses, or one only announced,
    /// makes it fetch again. None of its commits known there makes it start again from a snapshot.
    /// When the connection breaks it connects again, waiting longer after every failure, and catches up.
    /// Change sets and status updates can also be sent to the other server, which pushes them back.
    /// </remarks>
    public sealed partial class RoamingNetworkPeer : IAsyncDisposable
    {

        #region Data

        private readonly RoamingNetworkPeerOptions  options;
        private readonly TaskCompletionSource           firstSynchronization  = new (TaskCreationOptions.RunContinuationsAsynchronously);
        private          RoamingNetworkHistory?         history;
        private          Connection?                    connection;
        private          CancellationTokenSource?       stop;
        private          Task?                          runner;
        private          Int64                          reconnects;
        private          Boolean                        statusFromSnapshot;

        // The commits taken from the other server: not to be pushed back.
        private readonly ConcurrentDictionary<RoamingNetworkCommit_Id, Byte>  receivedCommits  = [];

        // The latest status of every entity known at the other server: not to be pushed back.
        private readonly ConcurrentDictionary<(POIRuntimeStatusKind Kind, String EntityType, String EntityId), (DateTimeOffset Timestamp, String Value)>  receivedStatus  = [];

        // While connected: the commits and status updates of the watched history to push, in their order.
        private readonly Lock                                                 watchLock        = new ();
        private          Channel<Object>?                                     outgoing;
        private          RoamingNetworkHistory?                               watched;

        #endregion

        #region Properties

        /// <summary>
        /// The URL of the WebSocket of the history of the other server, e.g. ws://server2/RNs/rn1/history/ws.
        /// </summary>
        public Uri                     PeerURL      { get; }

        /// <summary>
        /// The history kept in sync; null until the first snapshot. It is replaced when the other server knows none of its commits.
        /// </summary>
        public RoamingNetworkHistory?  History
            => Volatile.Read(ref history);

        /// <summary>
        /// Whether the peer is connected to the other server right now.
        /// </summary>
        public Boolean                 IsConnected
            => Volatile.Read(ref connection)?.IsOpen == true;

        /// <summary>
        /// How often the follower connected again.
        /// </summary>
        public Int64                   Reconnects
            => Interlocked.Read(ref reconnects);

        #endregion

        #region Events

        /// <summary>
        /// The follower has caught up with its leader, after connecting.
        /// </summary>
        public event OnRoamingNetworkPeerSynchronizedDelegate?     OnSynchronized;

        /// <summary>
        /// The follower started its history from a snapshot.
        /// </summary>
        public event OnRoamingNetworkPeerHistoryReplacedDelegate?  OnHistoryReplaced;

        /// <summary>
        /// Something went wrong; the follower connects again.
        /// </summary>
        public event OnRoamingNetworkPeerErrorDelegate?            OnError;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a follower of the history behind the given WebSocket URL.
        /// </summary>
        /// <param name="PeerURL">The URL of the WebSocket of the leader's history.</param>
        /// <param name="History">An optional history to continue, e.g. one persisted before; a snapshot otherwise.</param>
        /// <param name="Options">Optional options.</param>
        public RoamingNetworkPeer(Uri                             PeerURL,
                                      RoamingNetworkHistory?          History   = null,
                                      RoamingNetworkPeerOptions?  Options   = null)
        {

            this.PeerURL  = PeerURL;
            this.history    = History;
            this.options    = Options ?? new RoamingNetworkPeerOptions();

        }

        #endregion


        #region StartAsync(CancellationToken = default)

        /// <summary>
        /// Start following, and return once the follower caught up for the first time.
        /// It keeps trying until then; cancel the token to stop waiting, not following.
        /// </summary>
        public async Task StartAsync(CancellationToken CancellationToken = default)
        {

            if (runner is not null)
                throw new InvalidOperationException("The follower has been started already!");

            stop    = new CancellationTokenSource();
            runner  = Task.Run(() => RunAsync(stop.Token));

            await firstSynchronization.Task.WaitAsync(CancellationToken);

        }

        #endregion

        #region StopAsync()

        /// <summary>
        /// Stop following and close the connection.
        /// </summary>
        public async Task StopAsync()
        {

            stop?.Cancel();

            if (runner is not null)
            {
                try
                {
                    await runner;
                }
                catch
                { }
            }

        }

        #endregion

        #region DisposeAsync()

        /// <summary>
        /// Stop following.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            stop?.Dispose();
        }

        #endregion


        #region RequestAsync(Action, Payload = null, CancellationToken = default)

        /// <summary>
        /// Send a request to the leader and return its response; a request error is thrown as a POIWebSocketRequestException.
        /// </summary>
        /// <param name="Action">The action.</param>
        /// <param name="Payload">The optional payload.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task<JObject> RequestAsync(String             Action,
                                                JObject?           Payload             = null,
                                                CancellationToken  CancellationToken   = default)
        {

            var currentConnection = Volatile.Read(ref connection)
                                        ?? throw new InvalidOperationException("The follower is not connected to its leader!");

            var answer = await currentConnection.RequestAsync(Action, Payload ?? [], CancellationToken);

            return answer.IsError
                       ? throw new POIWebSocketRequestException(Action, answer)
                       : answer.GetPayloadJSON() ?? [];

        }

        #endregion

        #region CommitChangeSetAsync(ChangeSet, CancellationToken = default)

        /// <summary>
        /// Submit the given change set, signed by its owners, to the server followed;
        /// it commits it to the version it was made for, and it comes back as a commit.
        /// </summary>
        /// <param name="ChangeSet">A change set, e.g. made against the head of this follower.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        /// <returns>The answer of the server: { status, … }.</returns>
        public Task<JObject> CommitChangeSetAsync(RoamingNetworkChangeSet  ChangeSet,
                                                  CancellationToken        CancellationToken   = default)

            => RequestAsync("commitChangeSet",
                            new JObject(new JProperty("changeSet", ChangeSet.ToJSON())),
                            CancellationToken);

        #endregion

        #region SignCommitAsync(CommitId, Signatures, CancellationToken = default)

        /// <summary>
        /// Send signatures of a commit to the leader, e.g. the one of a roaming network operator
        /// that checked the data of the commit; they come back as commit signatures.
        /// </summary>
        /// <param name="CommitId">The identification of the commit.</param>
        /// <param name="Signatures">Signatures of the commit, e.g. made by its CreateSignature().</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        /// <returns>The answer of the leader: { commitId, signatures, added }.</returns>
        public Task<JObject> SignCommitAsync(RoamingNetworkCommit_Id                     CommitId,
                                             IEnumerable<RoamingNetworkCommitSignature>  Signatures,
                                             CancellationToken                           CancellationToken   = default)

            => RequestAsync("signCommit",
                            new JObject(
                                new JProperty("commitId",    CommitId.ToString()),
                                new JProperty("signatures",  new JArray(Signatures.Select(signature => signature.ToJSON())))
                            ),
                            CancellationToken);

        #endregion

        #region SetStatusAsync(Kind, EntityType, EntityId, Value, Timestamp = null, DataSource = null, CancellationToken = default)

        /// <summary>
        /// Set a status at the leader; it comes back as a status update.
        /// </summary>
        /// <returns>The answer of the leader: { result, description?, current? }.</returns>
        public Task<JObject> SetStatusAsync(POIRuntimeStatusKind  Kind,
                                            String                EntityType,
                                            String                EntityId,
                                            String                Value,
                                            DateTimeOffset?       Timestamp           = null,
                                            String?               DataSource          = null,
                                            CancellationToken     CancellationToken   = default)
        {

            var payload = new JObject(
                              new JProperty("kind",        POIStatusUpdate.KindText(Kind)),
                              new JProperty("entityType",  EntityType),
                              new JProperty("entityId",    EntityId),
                              new JProperty("value",       POIStatusUpdate.ValueToJSON(Kind, Value))
                          );

            if (Timestamp.HasValue)
                payload.Add("timestamp",   Timestamp.Value.ToISO8601());

            if (DataSource is not null)
                payload.Add("dataSource",  DataSource);

            return RequestAsync("setStatus", payload, CancellationToken);

        }

        #endregion


        #region SetPriceStatusAsync(EntityType, EntityId, PriceStatus, Timestamp = null, DataSource = null, CancellationToken = default)

        /// <summary>
        /// Set the price status of a charging pool, charging station or EVSE at the leader; it comes back as a price status update.
        /// </summary>
        /// <returns>The answer of the leader: { result, description?, current? }.</returns>
        public Task<JObject> SetPriceStatusAsync(String             EntityType,
                                                 String             EntityId,
                                                 ChargingPrices     PriceStatus,
                                                 DateTimeOffset?    Timestamp           = null,
                                                 String?            DataSource          = null,
                                                 CancellationToken  CancellationToken   = default)

            => SetStatusAsync(POIRuntimeStatusKind.PriceStatus,
                              EntityType,
                              EntityId,
                              PriceStatus.Text,
                              Timestamp,
                              DataSource,
                              CancellationToken);

        #endregion

        #region SetEnergyStatusAsync(EntityType, EntityId, EnergyStatus, Timestamp = null, DataSource = null, CancellationToken = default)

        /// <summary>
        /// Set the energy status of an EVSE or a charging station at the leader; it comes back as an energy status update.
        /// </summary>
        /// <returns>The answer of the leader: { result, description?, current? }.</returns>
        public Task<JObject> SetEnergyStatusAsync(String             EntityType,
                                                  String             EntityId,
                                                  EnergyInfo         EnergyStatus,
                                                  DateTimeOffset?    Timestamp           = null,
                                                  String?            DataSource          = null,
                                                  CancellationToken  CancellationToken   = default)

            => SetStatusAsync(POIRuntimeStatusKind.EnergyStatus,
                              EntityType,
                              EntityId,
                              POIEnergyStatus.ToText(EnergyStatus),
                              Timestamp,
                              DataSource,
                              CancellationToken);

        #endregion


        #region (private) RunAsync(CancellationToken)

        /// <summary>
        /// Connect, catch up and follow, again and again, until stopped.
        /// </summary>
        private async Task RunAsync(CancellationToken CancellationToken)
        {

            var delay = options.MinReconnectDelay;

            while (!CancellationToken.IsCancellationRequested)
            {

                try
                {

                    await using var currentConnection = await Connection.ConnectAsync(this, CancellationToken);

                    Volatile.Write(ref connection, currentConnection);

                    await SynchronizeAsync(currentConnection, CancellationToken);

                    delay = options.MinReconnectDelay;
                    firstSynchronization.TrySetResult();
                    OnSynchronized?.Invoke(this, History!);

                    // New commits and status updates of this history, made here or taken from elsewhere, go to the other server.
                    var pushes = Channel.CreateUnbounded<Object>(new UnboundedChannelOptions { SingleReader = true });

                    using var following = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

                    try
                    {

                        lock (watchLock)
                        {
                            outgoing = pushes;
                            Watch(History);
                        }

                        // The status set here while not connected, or differently than there.
                        if (options.PushStatus)
                            foreach (var statusUpdate in History!.RuntimeStatus.All)
                                if (!IsReceived(statusUpdate))
                                    pushes.Writer.TryWrite(statusUpdate);

                        var pushing = options.PushCommits || options.PushStatus || options.PushCheckpoints
                                          ? PushAsync(currentConnection, pushes.Reader, following.Token)
                                          : Task.CompletedTask;

                        try
                        {
                            await FollowAsync(currentConnection, CancellationToken);
                        }
                        finally
                        {
                            following.Cancel();
                            try { await pushing; } catch { }
                        }

                    }
                    finally
                    {
                        lock (watchLock)
                        {
                            outgoing = null;
                            Watch(null);
                        }
                    }

                }
                catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    RaiseError($"Following '{PeerURL}' failed: {e.Message}", e);
                }
                finally
                {
                    Volatile.Write(ref connection, null);
                }

                if (CancellationToken.IsCancellationRequested)
                    break;

                Interlocked.Increment(ref reconnects);

                try
                {
                    await Task.Delay(delay, CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, options.MaxReconnectDelay.Ticks));

            }

            firstSynchronization.TrySetCanceled();

        }

        #endregion

        #region (private) SynchronizeAsync(Connection, CancellationToken)

        /// <summary>
        /// Subscribe, start from a snapshot if needed, catch up, and take the current runtime status.
        /// </summary>
        private async Task SynchronizeAsync(Connection         Connection,
                                            CancellationToken  CancellationToken)
        {

            // First, so that nothing published from now on gets lost:
            // pushes wait in the connection until they are taken below.
            var topics = new JArray(options.PushCompleteCommits
                                        ? POI_API.CommitsTopic
                                        : POI_API.CommitAnnouncementsTopic,
                                    POI_API.CommitSignaturesTopic);

            if (options.FollowStatus)
            {
                topics.Add(POI_API.AdminStatusUpdatesTopic);
                topics.Add(POI_API.StatusUpdatesTopic);
                topics.Add(POI_API.EnergyStatusUpdatesTopic);
                topics.Add(POI_API.PriceStatusUpdatesTopic);
            }

            if (options.FollowCheckpoints)
            {
                topics.Add(POI_API.CheckpointsTopic);
                topics.Add(POI_API.CheckpointSignaturesTopic);
            }

            await Expect(Connection, "subscribe", new JObject(new JProperty("topics", topics)), CancellationToken);

            statusFromSnapshot = false;

            if (History is null)
                await StartFromSnapshotAsync(Connection, CancellationToken);

            await CatchUpAsync(Connection, CancellationToken);

            if (options.PushCommits)
                await PushMissingCommitsAsync(Connection, CancellationToken);

            // After the commits: a checkpoint names commits, which must be there first.
            if (options.FollowCheckpoints || options.PushCheckpoints)
                await SynchronizeCheckpointsAsync(Connection, CancellationToken);

            // A snapshot brought the runtime status of its moment; what changed since was pushed.
            // Without following it, the status there is still asked for: to push only what differs.
            if (options.FollowStatus ? !statusFromSnapshot : options.PushStatus)
            {

                var runtimeStatus = await Expect(Connection, "runtimeStatus", [], CancellationToken);

                foreach (var statusJSON in runtimeStatus["runtimeStatus"]?.OfType<JObject>() ?? [])
                {
                    if (options.FollowStatus)
                        TakeStatusUpdate(statusJSON);

                    else if (POIStatusUpdate.TryParse(statusJSON, out var statusUpdate, out _))
                        Received(statusUpdate);
                }

            }

        }

        #endregion

        #region (private) FollowAsync(Connection, CancellationToken)

        /// <summary>
        /// Take the pushes of the leader, in their order, until the connection ends.
        /// </summary>
        private async Task FollowAsync(Connection         Connection,
                                       CancellationToken  CancellationToken)
        {

            await foreach (var push in Connection.Pushes.Reader.ReadAllAsync(CancellationToken))
            {

                var payloads = push.GetPayloadJSON()?["batch"] is JArray batch
                                   ? batch.OfType<JObject>().ToArray()
                                   : [ push.GetPayloadJSON() ?? [] ];

                foreach (var payload in payloads)
                {
                    switch (push.Action)
                    {

                        case "commit":
                            await TakeCommitAsync(Connection, payload, CancellationToken);
                            break;

                        case "commitSignatures":
                            TakeCommitSignatures(payload);
                            break;

                        case "checkpoint":
                            TakeCheckpoint(payload);
                            break;

                        case "checkpointSignatures":
                            TakeCheckpointSignatures(payload);
                            break;

                        case "commitAnnouncement":
                            if (payload["commitId"]?.Value<String>() is not String commitIdText ||
                                !RoamingNetworkCommit_Id.TryParse(commitIdText, out var commitId) ||
                                !History!.Contains(commitId))
                            {
                                await CatchUpAsync(Connection, CancellationToken);
                            }
                            break;

                        case "statusUpdate":
                        case "adminStatusUpdate":
                        case "energyStatusUpdate":
                        case "priceStatusUpdate":
                            TakeStatusUpdate(payload);
                            break;

                    }
                }

            }

        }

        #endregion

        #region (private) TakeCommitAsync(Connection, CommitJSON, CancellationToken)

        /// <summary>
        /// Append a pushed commit, or catch up when it does not follow the head.
        /// </summary>
        private async Task TakeCommitAsync(Connection         Connection,
                                           JObject            CommitJSON,
                                           CancellationToken  CancellationToken)
        {

            var currentHistory = History!;

            if (RoamingNetworkCommit.TryParse(CommitJSON, out var commit, out _, options.ChangeSetLimits))
            {

                receivedCommits.TryAdd(commit.Id, 0);

                // A commit held already only brings signatures in addition; one whose parents are all held is appended.
                if ((currentHistory.Contains(commit.Id) || commit.ParentIds.All(currentHistory.Contains)) &&
                    currentHistory.TryAppend(commit, out _, options.VerifyChangeSetSignature, options.VerifyCommitSignature, options.RequiredSignatureRoles))
                {
                    return;
                }

            }

            // Missed, unreadable or not appendable: ask for everything missing here.
            await CatchUpAsync(Connection, CancellationToken);

        }

        #endregion

        #region (private) TakeCommitSignatures(SignaturesJSON)

        /// <summary>
        /// Take the new signatures of a commit, e.g. of a roaming network operator.
        /// A commit not held yet brings them along when it is fetched.
        /// </summary>
        private void TakeCommitSignatures(JObject SignaturesJSON)
        {

            if (!RoamingNetworkCommitSignature.TryParseCommitSignatures(SignaturesJSON, out var commitId, out var signatures, out var errorResponse))
            {
                RaiseError("Invalid commit signatures: " + errorResponse, null);
                return;
            }

            var currentHistory = History!;

            if (currentHistory.Contains(commitId) &&
                !currentHistory.TryAddSignatures(commitId, signatures, options.VerifyCommitSignature, out _, out _, out errorResponse))
            {
                RaiseError("The commit signatures of the leader can not be taken: " + errorResponse, null);
            }

        }

        #endregion

        #region (private) TakeStatusUpdate(StatusJSON)

        private void TakeStatusUpdate(JObject StatusJSON)
        {

            if (POIStatusUpdate.TryParse(StatusJSON, out var statusUpdate, out var errorResponse))
            {
                // First: taking it tells of it, and what came from there does not go back.
                Received(statusUpdate);
                History!.RuntimeStatus.TryUpdate(statusUpdate, out _);
            }

            else
                RaiseError("Invalid status update: " + errorResponse, null);

        }

        #endregion

        #region (private) Received(StatusUpdate)

        /// <summary>
        /// Remember the given status update as known at the other server, unless a later one of the entity is.
        /// </summary>
        private void Received(POIStatusUpdate StatusUpdate)
        {

            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(StatusUpdate.Timestamp.ToUnixTimeMilliseconds());

            receivedStatus.AddOrUpdate((StatusUpdate.Kind, StatusUpdate.EntityType, StatusUpdate.EntityId),
                                       (timestamp, StatusUpdate.Value),
                                       (_, known) => timestamp >= known.Timestamp ? (timestamp, StatusUpdate.Value) : known);

        }

        #endregion

        #region (private) IsReceived(StatusUpdate)

        /// <summary>
        /// Whether the other server knows the given status of the entity already, e.g. as it came from there.
        /// </summary>
        private Boolean IsReceived(POIStatusUpdate StatusUpdate)

            => receivedStatus.TryGetValue((StatusUpdate.Kind, StatusUpdate.EntityType, StatusUpdate.EntityId), out var known) &&
               known.Timestamp.ToUnixTimeMilliseconds() == StatusUpdate.Timestamp.ToUnixTimeMilliseconds() &&
               String.Equals(known.Value, StatusUpdate.Value, StringComparison.OrdinalIgnoreCase);

        #endregion

        #region (private) Watch(History)

        /// <summary>
        /// Push the new commits and status updates of the given history from now on, none of the one before; called with watchLock held.
        /// </summary>
        private void Watch(RoamingNetworkHistory? History)
        {

            if (watched is not null)
            {
                watched.OnCommitAnnouncement          -= Announced;
                watched.RuntimeStatus.OnStatusUpdate  -= StatusUpdated;
                WatchCheckpoints(watched, false);
            }

            watched = outgoing is not null ? History : null;

            if (watched is not null)
            {

                if (options.PushCommits)
                    watched.OnCommitAnnouncement          += Announced;

                if (options.PushStatus)
                    watched.RuntimeStatus.OnStatusUpdate  += StatusUpdated;

                WatchCheckpoints(watched, true);

            }

        }

        private void Announced(RoamingNetworkCommitAnnouncement Announcement)
        {
            if (!receivedCommits.ContainsKey(Announcement.CommitId))
                Volatile.Read(ref outgoing)?.Writer.TryWrite(Announcement.Commit);
        }

        private void StatusUpdated(POIStatusUpdate StatusUpdate)
        {
            if (!IsReceived(StatusUpdate))
                Volatile.Read(ref outgoing)?.Writer.TryWrite(StatusUpdate);
        }

        #endregion

        #region (private) CatchUpAsync(Connection, CancellationToken)

        /// <summary>
        /// Append all commits of the other server missing here, page by page, naming the commits held here,
        /// until there are no more. None of them known there makes the peer start again from a snapshot.
        /// </summary>
        private async Task CatchUpAsync(Connection         Connection,
                                        CancellationToken  CancellationToken)
        {

            while (true)
            {

                var currentHistory  = History!;
                var answer          = await Connection.RequestAsync("commits",
                                                                    new JObject(
                                                                        new JProperty("have",  new JArray(currentHistory.HaveIds().Select(commitId => commitId.ToString()))),
                                                                        new JProperty("max",   options.MaxCommitsPerRequest)
                                                                    ),
                                                                    CancellationToken);

                if (answer.IsError)
                {

                    if (answer.ErrorCode == POIWebSocketErrorCodes.NotFound)
                    {
                        await StartFromSnapshotAsync(Connection, CancellationToken);
                        continue;
                    }

                    throw new POIWebSocketRequestException("commits", answer);

                }

                var commits = new List<RoamingNetworkCommit>();

                foreach (var commitJSON in answer.GetPayloadJSON()?["commits"]?.OfType<JObject>() ?? [])
                {

                    if (!RoamingNetworkCommit.TryParse(commitJSON, out var commit, out var commitError, options.ChangeSetLimits))
                        throw new InvalidOperationException("Invalid commit of the other server: " + commitError);

                    receivedCommits.TryAdd(commit.Id, 0);
                    commits.Add(commit);

                }

                // Until a page brings nothing new: the other server may send fewer than asked for, and some held here already.
                if (commits.All(commit => currentHistory.Contains(commit.Id)))
                    return;

                if (!currentHistory.TryAppend(commits, out _, out var appendError, options.VerifyChangeSetSignature, options.VerifyCommitSignature, options.RequiredSignatureRoles))
                    throw new InvalidOperationException("The commits of the other server can not be appended: " + appendError);

            }

        }

        #endregion

        #region (private) StartFromSnapshotAsync(Connection, CancellationToken)

        private async Task StartFromSnapshotAsync(Connection         Connection,
                                                  CancellationToken  CancellationToken)
        {

            var snapshot = await Expect(Connection,
                                        "snapshot",
                                        new JObject(new JProperty("runtimeStatus", options.FollowStatus)),
                                        CancellationToken);

            if (!RoamingNetworkHistory.TryFromSnapshot(snapshot, out var newHistory, out var errorResponse, options.ChangeSetLimits))
                throw new InvalidOperationException("Invalid snapshot of the other server: " + errorResponse);

            if (!RoamingNetworkHistory.CheckSignatures(newHistory.Head.Commit, options.VerifyCommitSignature, options.RequiredSignatureRoles, out var signatureError))
                throw new InvalidOperationException("The snapshot of the other server is not signed as required: " + signatureError);

            // The trust anchor: the snapshot must be one of the roaming network the keys of its operator name.
            if (options.TrustedRoamingNetworkOperatorKeyIds is { Count: > 0 } trustedKeyIds &&
                !(newHistory.Head.RoamingNetwork.Operator is RoamingNetworkOperator roamingNetworkOperator &&
                  roamingNetworkOperator.IdentityKeys.Concat(roamingNetworkOperator.SigningKeys).Any(key => trustedKeyIds.Contains(key.Id))))
            {
                throw new InvalidOperationException("The snapshot of the other server names none of the trusted keys of the roaming network operator!");
            }

            if (options.Retention is not null)
                newHistory.Retention = options.Retention;

            // The store, of the history replaced or given, keeps the new one from now on.
            var store = History?.DetachStore() ?? options.Store;

            if (store is not null)
                newHistory.AttachStore(store);

            // The status of the snapshot is the one there.
            receivedStatus.Clear();

            foreach (var statusUpdate in newHistory.RuntimeStatus.All)
                Received(statusUpdate);

            RoamingNetworkHistory? oldHistory;

            lock (watchLock)
            {
                oldHistory = Interlocked.Exchange(ref history, newHistory);
                Watch(newHistory);
            }

            statusFromSnapshot = options.FollowStatus;

            OnHistoryReplaced?.Invoke(this, oldHistory, newHistory);

        }

        #endregion

        #region (private) PushMissingCommitsAsync(Connection, CancellationToken)

        /// <summary>
        /// Push the commits of this history the other server misses, as it names the commits it holds, page by page.
        /// A server not allowing it is told so once, by an error.
        /// </summary>
        private async Task PushMissingCommitsAsync(Connection         Connection,
                                                   CancellationToken  CancellationToken)
        {

            var currentHistory  = History!;
            var haveIds         = await Expect(Connection, "haveIds", [], CancellationToken);

            var have            = (haveIds["haveIds"] as JArray ?? []).
                                      Select (item     => RoamingNetworkCommit_Id.TryParse(item.Value<String>() ?? "", out var commitId) ? commitId : (RoamingNetworkCommit_Id?) null).
                                      Where  (commitId => commitId.HasValue).
                                      Select (commitId => commitId!.Value).
                                      ToArray();

            if (!currentHistory.TryGetMissingCommits(have, out var missing, out _, Int32.MaxValue))
                return;

            foreach (var page in missing.Where(commit => !receivedCommits.ContainsKey(commit.Id)).Chunk(options.MaxCommitsPerRequest))
                if (!await TryPushAsync(Connection, page, CancellationToken))
                    return;

        }

        #endregion

        #region (private) PushAsync(Connection, Pushes, CancellationToken)

        /// <summary>
        /// Push the new commits and status updates of this history to the other server, as they come, in their order,
        /// e.g. a new EVSE before its status; those of a kind waiting together at once.
        /// A kind the other server does not take is not pushed any more.
        /// </summary>
        private async Task PushAsync(Connection             Connection,
                                     ChannelReader<Object>  Pushes,
                                     CancellationToken      CancellationToken)
        {

            var pushCommits  = options.PushCommits;
            var pushStatus   = options.PushStatus;
            var pushCheckpoints = options.PushCheckpoints;
            Object? next     = null;

            while (pushCommits || pushStatus || pushCheckpoints)
            {

                if (next is null)
                {

                    if (!await Pushes.WaitToReadAsync(CancellationToken))
                        return;

                    if (!Pushes.TryRead(out next))
                        continue;

                }

                if (next is RoamingNetworkCommit firstCommit)
                {

                    var commits = new List<RoamingNetworkCommit> { firstCommit };
                    next = null;

                    while (commits.Count < options.MaxCommitsPerRequest && Pushes.TryRead(out next) && next is RoamingNetworkCommit commit)
                    {
                        commits.Add(commit);
                        next = null;
                    }

                    if (pushCommits && !await TryPushAsync(Connection, commits, CancellationToken))
                        pushCommits = false;

                }

                else if (next is POIStatusUpdate firstStatusUpdate)
                {

                    var statusUpdates = new List<POIStatusUpdate> { firstStatusUpdate };
                    next = null;

                    while (statusUpdates.Count < options.MaxStatusUpdatesPerRequest && Pushes.TryRead(out next) && next is POIStatusUpdate statusUpdate)
                    {
                        statusUpdates.Add(statusUpdate);
                        next = null;
                    }

                    if (pushStatus && !await TryPushStatusAsync(Connection, statusUpdates, CancellationToken))
                        pushStatus = false;

                }

                else if (next is RoamingNetworkCheckpoint checkpoint)
                {

                    next = null;

                    if (pushCheckpoints && !await TryPushCheckpointsAsync(Connection, [ checkpoint ], CancellationToken))
                        pushCheckpoints = false;

                }

                else if (next is CheckpointSignaturesPush checkpointSignatures)
                {

                    next = null;

                    if (pushCheckpoints && !await TryPushCheckpointSignaturesAsync(Connection, checkpointSignatures, CancellationToken))
                        pushCheckpoints = false;

                }

                else
                    next = null;

            }

        }

        #endregion

        #region (private) TryPushStatusAsync(Connection, StatusUpdates, CancellationToken)

        /// <summary>
        /// Push the given status updates; false when the other server does not allow it.
        /// </summary>
        private async Task<Boolean> TryPushStatusAsync(Connection                      Connection,
                                                       IReadOnlyList<POIStatusUpdate>  StatusUpdates,
                                                       CancellationToken               CancellationToken)
        {

            var answer = await Connection.RequestAsync("pushStatusUpdates",
                                                       new JObject(new JProperty("statusUpdates", new JArray(StatusUpdates.Select(statusUpdate => statusUpdate.ToJSON())))),
                                                       CancellationToken);

            if (answer.IsError)
            {
                RaiseError($"The other server takes no status updates: {answer.ErrorDescription}", null);
                return false;
            }

            var refused = (answer.GetPayloadJSON()?["results"] as JArray ?? []).
                              OfType<JObject>().
                              Where (result => result["result"]?.Value<String>() == nameof(POIStatusUpdateResult.Refused)).
                              ToArray();

            if (refused.Length > 0)
                RaiseError($"The other server refused {refused.Length} of {StatusUpdates.Count} status updates: {refused[0]["description"]?.Value<String>()}", null);

            return true;

        }

        #endregion

        #region (private) TryPushAsync(Connection, Commits, CancellationToken)

        /// <summary>
        /// Push the given commits; false when the other server does not allow it.
        /// </summary>
        private async Task<Boolean> TryPushAsync(Connection                           Connection,
                                                 IReadOnlyList<RoamingNetworkCommit>  Commits,
                                                 CancellationToken                    CancellationToken)
        {

            if (Commits.Count == 0)
                return true;

            var answer = await Connection.RequestAsync("pushCommits",
                                                       new JObject(new JProperty("commits", new JArray(Commits.Select(commit => commit.ToJSON())))),
                                                       CancellationToken);

            if (answer.IsError)
            {
                RaiseError($"The other server takes no commits: {answer.ErrorDescription}", null);
                return false;
            }

            if (answer.GetPayloadJSON()?["description"]?.Value<String>() is String description)
                RaiseError("The other server did not take all commits: " + description, null);

            return true;

        }

        #endregion

        #region (private static) Expect(Connection, Action, Payload, CancellationToken)

        /// <summary>
        /// The payload of the response, or a POIWebSocketRequestException.
        /// </summary>
        private static async Task<JObject> Expect(Connection         Connection,
                                                  String             Action,
                                                  JObject            Payload,
                                                  CancellationToken  CancellationToken)
        {

            var answer = await Connection.RequestAsync(Action, Payload, CancellationToken);

            return answer.IsError
                       ? throw new POIWebSocketRequestException(Action, answer)
                       : answer.GetPayloadJSON() ?? [];

        }

        #endregion

        #region (private) RaiseError(Description, Exception)

        private void RaiseError(String      Description,
                                Exception?  Exception)
        {
            try
            {
                OnError?.Invoke(this, Description, Exception);
            }
            catch
            { }
        }

        #endregion


        #region (private class) Connection

        /// <summary>
        /// One WebSocket connection to the leader: requests matched to their answers, pushes in their order.
        /// </summary>
        private sealed class Connection : IAsyncDisposable
        {

            #region Data

            private readonly RoamingNetworkPeer                                                   follower;
            private readonly ClientWebSocket                                                          webSocket    = new ();
            private readonly SemaphoreSlim                                                            sendLock     = new (1, 1);
            private readonly ConcurrentDictionary<String, TaskCompletionSource<POIWebSocketMessage>>  pending      = new (StringComparer.Ordinal);
            private readonly CancellationTokenSource                                                  closed       = new ();
            private          Task?                                                                    receiver;
            private          Int64                                                                    messageCounter;

            #endregion

            #region Properties

            /// <summary>
            /// The pushes of the leader, in their order; completed when the connection ends.
            /// </summary>
            public Channel<POIWebSocketMessage>  Pushes  { get; } = Channel.CreateUnbounded<POIWebSocketMessage>(new UnboundedChannelOptions { SingleReader = true });

            public Boolean                       IsOpen
                => webSocket.State == WebSocketState.Open && !closed.IsCancellationRequested;

            #endregion

            private Connection(RoamingNetworkPeer Follower)
            {
                this.follower = Follower;
            }

            #region (static) ConnectAsync(Follower, CancellationToken)

            public static async Task<Connection> ConnectAsync(RoamingNetworkPeer  Follower,
                                                              CancellationToken       CancellationToken)
            {

                var connection = new Connection(Follower);

                try
                {

                    Follower.options.ConfigureWebSocket?.Invoke(connection.webSocket.Options);

                    await connection.webSocket.ConnectAsync(Follower.PeerURL, CancellationToken);

                    connection.receiver = Task.Run(connection.ReceiveAsync);

                    return connection;

                }
                catch
                {
                    await connection.DisposeAsync();
                    throw;
                }

            }

            #endregion

            #region RequestAsync(Action, Payload, CancellationToken)

            /// <summary>
            /// Send a request and wait for its response or request error.
            /// </summary>
            public async Task<POIWebSocketMessage> RequestAsync(String             Action,
                                                                JObject            Payload,
                                                                CancellationToken  CancellationToken)
            {

                var messageId  = "f" + Interlocked.Increment(ref messageCounter);
                var answer     = new TaskCompletionSource<POIWebSocketMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

                pending[messageId] = answer;

                try
                {

                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, closed.Token);

                    await sendLock.WaitAsync(linked.Token);

                    try
                    {
                        await webSocket.SendAsync(Encoding.UTF8.GetBytes(POIWebSocketMessage.Request(messageId, Action, Payload).ToJSONText()),
                                                  WebSocketMessageType.Text,
                                                  true,
                                                  linked.Token);
                    }
                    finally
                    {
                        sendLock.Release();
                    }

                    return await answer.Task.WaitAsync(follower.options.RequestTimeout, linked.Token);

                }
                catch (OperationCanceledException) when (closed.IsCancellationRequested && !CancellationToken.IsCancellationRequested)
                {
                    throw new WebSocketException($"The connection closed before the request '{Action}' was answered!");
                }
                finally
                {
                    pending.TryRemove(messageId, out _);
                }

            }

            #endregion

            #region (private) ReceiveAsync()

            /// <summary>
            /// Read the messages of the leader until the connection ends: answers to their requests, pushes into the channel.
            /// </summary>
            private async Task ReceiveAsync()
            {

                var buffer = new Byte[64 * 1024];

                try
                {

                    while (webSocket.State == WebSocketState.Open)
                    {

                        using var message = new MemoryStream();
                        WebSocketReceiveResult result;

                        do
                        {

                            result = await webSocket.ReceiveAsync(buffer, closed.Token);

                            if (message.Length + result.Count > follower.options.MaxMessageBytes)
                            {
                                await webSocket.CloseOutputAsync(WebSocketCloseStatus.MessageTooBig, "Too large", CancellationToken.None);
                                return;
                            }

                            message.Write(buffer, 0, result.Count);

                        }
                        while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            try
                            {
                                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                            }
                            catch
                            { }
                            return;
                        }

                        var parsed = result.MessageType == WebSocketMessageType.Binary
                                         ? POIWebSocketMessage.TryParse(message.ToArray(),                          out var received, out _)
                                         : POIWebSocketMessage.TryParse(Encoding.UTF8.GetString(message.ToArray()), out received,     out _);

                        if (!parsed)
                            continue;

                        if (received!.MessageType == POIWebSocketMessageType.Send)
                            Pushes.Writer.TryWrite(received);

                        else if (pending.TryRemove(received.MessageId, out var answer))
                            answer.TrySetResult(received);

                    }

                }
                catch (Exception e) when (e is WebSocketException or OperationCanceledException or ObjectDisposedException)
                { }
                finally
                {
                    Pushes.Writer.TryComplete();
                    closed.Cancel();
                }

            }

            #endregion

            #region DisposeAsync()

            public async ValueTask DisposeAsync()
            {

                // Only the close frame: the receiver is still reading, and a socket reads in one place at a time.
                if (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                        await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", timeout.Token);
                    }
                    catch
                    { }
                }

                closed.Cancel();
                webSocket.Abort();

                if (receiver is not null)
                {
                    try
                    {
                        await receiver;
                    }
                    catch
                    { }
                }

                webSocket.Dispose();

            }

            #endregion

        }

        #endregion

    }

}
