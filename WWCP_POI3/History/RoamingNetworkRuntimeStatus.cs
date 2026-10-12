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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Called for every change of a current admin status or status of the head version, in their order,
    /// while the status schedule of the entity is locked: return quickly and do not set a status from here.
    /// </summary>
    /// <param name="StatusUpdate">The status update.</param>
    public delegate void OnPOIStatusUpdateDelegate(POIStatusUpdate StatusUpdate);


    /// <summary>
    /// The outcome of a status update.
    /// </summary>
    public enum POIStatusUpdateResult
    {

        /// <summary>
        /// The entity has the new status now.
        /// </summary>
        Updated,

        /// <summary>
        /// The entity has this status already, since the same or an earlier time.
        /// </summary>
        Unchanged,

        /// <summary>
        /// The entity has a status since a later time: the update went into its history only.
        /// </summary>
        Outdated,

        /// <summary>
        /// The status is planned for a later time: the entity will have it then.
        /// </summary>
        Scheduled,

        /// <summary>
        /// The entity is unknown in the head version, or the status is invalid.
        /// </summary>
        Refused

    }


    /// <summary>
    /// The admin status and status of the entities of the head version of a history, by their names:
    /// for the WebSocket and for followers. The status itself is kept by the entities, typed, in their
    /// status schedules, which all versions of an entity share. Setting it here or at the entity is the same:
    /// either way OnStatusUpdate tells of it. A planned status is told of when it becomes the current one.
    /// </summary>
    /// <remarks>
    /// An update with an earlier timestamp than the current status goes into the history of the entity only,
    /// so that updates arriving out of order, e.g. at a follower, end with the latest one.
    /// The same status again keeps its timestamp: since when the entity has it. A status update of the
    /// leading server, TryUpdate(StatusUpdate, …), is taken as it is, though: its "since" is the leader's.
    /// </remarks>
    public sealed class RoamingNetworkRuntimeStatus
    {

        #region (private class) POIStatusContext

        /// <summary>
        /// Carries the data source of a status update through the status schedule to its event.
        /// </summary>
        private sealed class POIStatusContext(String DataSource) : Context(DataSource)
        {
            public String Text { get; } = DataSource;
        }

        #endregion

        #region (private delegate) TryParseStatus<T>(Text, out Status)

        private delegate Boolean TryParseStatus<T>(String Text, out T Status);

        #endregion

        #region Data

        /// <summary>
        /// The longest a planned status waits for its timer at once; the timer looks again then.
        /// </summary>
        private static readonly TimeSpan                    maxPlannedStatusWait  = TimeSpan.FromHours(1);

        private readonly RoamingNetworkHistory              history;
        private readonly Lock                               attachLock            = new ();
        private readonly Dictionary<Object, Action>         listeners             = new (ReferenceEqualityComparer.Instance);
        private readonly Lock                               timerLock             = new ();
        private readonly Timer                              plannedStatusTimer;

        #endregion

        #region Events

        /// <summary>
        /// Called for every change of a current admin status or status of the head version, in their order,
        /// while the status schedule of the entity is locked: return quickly and do not set a status from there.
        /// </summary>
        public event OnPOIStatusUpdateDelegate? OnStatusUpdate;

        #endregion

        #region Constructor(s)

        internal RoamingNetworkRuntimeStatus(RoamingNetworkHistory History)
        {

            this.history             = History;
            this.plannedStatusTimer  = new Timer(_ => TakePlannedStatus(), null, Timeout.Infinite, Timeout.Infinite);

        }

        #endregion


        #region TryUpdate(Kind, EntityType, EntityId, Value, Timestamp, out StatusUpdate, out ErrorResponse, DataSource = null)

        /// <summary>
        /// Set the admin status or the status of an entity of the head version.
        /// </summary>
        /// <param name="Kind">Admin status or status.</param>
        /// <param name="EntityType">The type of the entity: RoamingNetwork or ChargingStationOperator.</param>
        /// <param name="EntityId">The identification of the entity.</param>
        /// <param name="Value">The new status, e.g. "available".</param>
        /// <param name="Timestamp">Since when the entity has this status; a later one plans it.</param>
        /// <param name="StatusUpdate">The status update, when Updated.</param>
        /// <param name="ErrorResponse">Why it was refused or is out of date.</param>
        /// <param name="DataSource">The optional source of the status.</param>
        public POIStatusUpdateResult TryUpdate(POIRuntimeStatusKind  Kind,
                                               String                EntityType,
                                               String                EntityId,
                                               String                Value,
                                               DateTimeOffset        Timestamp,
                                               out POIStatusUpdate?  StatusUpdate,
                                               out String?           ErrorResponse,
                                               String?               DataSource   = null)

            => Update(Kind,
                      EntityType,
                      EntityId,
                      Value,
                      Timestamp,
                      Authoritative: false,
                      out StatusUpdate,
                      out ErrorResponse,
                      DataSource);

        #endregion

        #region TryUpdate(StatusUpdate, out ErrorResponse)

        /// <summary>
        /// Take the status of the given status update of the leading server as it is: also the same status
        /// with a later timestamp, as the leader's "since" counts, so that an older update arriving afterwards
        /// is out of date. Its old status is ignored.
        /// </summary>
        /// <param name="StatusUpdate">A status update.</param>
        /// <param name="ErrorResponse">Why it was refused or is out of date.</param>
        public POIStatusUpdateResult TryUpdate(POIStatusUpdate  StatusUpdate,
                                               out String?      ErrorResponse)

            => Update(StatusUpdate.Kind,
                      StatusUpdate.EntityType,
                      StatusUpdate.EntityId,
                      StatusUpdate.Value,
                      StatusUpdate.Timestamp,
                      Authoritative: true,
                      out _,
                      out ErrorResponse,
                      StatusUpdate.DataSource);

        #endregion

        #region (private) Update(Kind, EntityType, EntityId, Value, Timestamp, Authoritative, out StatusUpdate, out ErrorResponse, DataSource)

        private POIStatusUpdateResult Update(POIRuntimeStatusKind  Kind,
                                             String                EntityType,
                                             String                EntityId,
                                             String                Value,
                                             DateTimeOffset        Timestamp,
                                             Boolean               Authoritative,
                                             out POIStatusUpdate?  StatusUpdate,
                                             out String?           ErrorResponse,
                                             String?               DataSource)
        {

            StatusUpdate = null;

            var at       = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp.ToUnixTimeMilliseconds());
            var context  = DataSource is not null ? new POIStatusContext(DataSource) : null;
            var network  = history.Head.RoamingNetwork;

            POIStatusUpdateResult  result;
            String?                normalized;
            DateTimeOffset?        oldTimestamp;
            String?                oldValue;

            switch (EntityType)
            {

                case nameof(RoamingNetwork):

                    if (EntityId != network.Id.ToString())
                        return Unknown(EntityType, EntityId, out ErrorResponse);

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<RoamingNetworkAdminStatusType>(Value, RoamingNetworkAdminStatusType.TryParse,
                                                                      () => network.CurrentAdminStatus,
                                                                      (status, timestamp) => network.SetAdminStatus(status, timestamp, context),
                                                                      (status, timestamp) => network.SetAdminStatus([ new Timestamped<RoamingNetworkAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                                      Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<RoamingNetworkStatusType>     (Value, RoamingNetworkStatusType.TryParse,
                                                                      () => network.CurrentStatus,
                                                                      (status, timestamp) => network.SetStatus(status, timestamp, context),
                                                                      (status, timestamp) => network.SetStatus([ new Timestamped<RoamingNetworkStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                                      Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(ChargingStationOperator):

                    if (!ChargingStationOperator_Id.TryParse(EntityId, out var operatorId) ||
                        !network.TryGetChargingStationOperatorById(operatorId, out var chargingStationOperator))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ChargingStationOperatorAdminStatusType>(Value, ChargingStationOperatorAdminStatusType.TryParse,
                                                                               () => chargingStationOperator.CurrentAdminStatus,
                                                                               (status, timestamp) => chargingStationOperator.SetAdminStatus(status, timestamp, context),
                                                                               (status, timestamp) => chargingStationOperator.SetAdminStatus([ new Timestamped<ChargingStationOperatorAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                                               Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ChargingStationOperatorStatusType>     (Value, ChargingStationOperatorStatusType.TryParse,
                                                                               () => chargingStationOperator.CurrentStatus,
                                                                               (status, timestamp) => chargingStationOperator.SetStatus(status, timestamp, context),
                                                                               (status, timestamp) => chargingStationOperator.SetStatus([ new Timestamped<ChargingStationOperatorStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                                               Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                default:
                    ErrorResponse = $"The entity type '{EntityType}' has no runtime status!";
                    return POIStatusUpdateResult.Refused;

            }

            switch (result)
            {

                case POIStatusUpdateResult.Refused:
                    ErrorResponse = $"Invalid {POIStatusUpdate.KindText(Kind)} '{Value}' of a {EntityType}!";
                    break;

                case POIStatusUpdateResult.Outdated:
                    ErrorResponse = $"The {EntityType} '{EntityId}' has a {POIStatusUpdate.KindText(Kind)} since {oldTimestamp?.ToISO8601()}!";
                    break;

                case POIStatusUpdateResult.Scheduled:
                    ErrorResponse = null;
                    SchedulePlannedStatus();
                    break;

                case POIStatusUpdateResult.Updated:
                    ErrorResponse = null;
                    StatusUpdate  = new POIStatusUpdate(Kind, EntityType, EntityId, at, normalized!, oldTimestamp, oldValue, DataSource);
                    break;

                default:
                    ErrorResponse = null;
                    break;

            }

            return result;

        }

        #endregion

        #region TryGet(Kind, EntityType, EntityId, out Status)

        /// <summary>
        /// Try to return the current admin status or status of an entity of the head version, with since when it has it.
        /// </summary>
        /// <param name="Kind">Admin status or status.</param>
        /// <param name="EntityType">The type of the entity.</param>
        /// <param name="EntityId">The identification of the entity.</param>
        /// <param name="Status">The current status.</param>
        public Boolean TryGet(POIRuntimeStatusKind                       Kind,
                              String                                     EntityType,
                              String                                     EntityId,
                              [NotNullWhen(true)] out POIStatusUpdate?  Status)
        {

            Status = All.FirstOrDefault(status => status.Kind       == Kind       &&
                                                  status.EntityType == EntityType &&
                                                  status.EntityId   == EntityId);

            return Status is not null;

        }

        #endregion

        #region All

        /// <summary>
        /// The current admin status and status of all entities of the head version, without old values.
        /// </summary>
        public IReadOnlyList<POIStatusUpdate> All

            => Of(history.Head.RoamingNetwork);

        #endregion

        #region (internal static) Of(RoamingNetwork)

        /// <summary>
        /// The current admin status and status of all entities of the given version, without old values.
        /// </summary>
        internal static IReadOnlyList<POIStatusUpdate> Of(RoamingNetwork RoamingNetwork)
        {

                var network   = RoamingNetwork;
                var statuses  = new List<POIStatusUpdate>();

                Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(RoamingNetwork), network.Id.ToString(), network.CurrentAdminStatus);
                Add(statuses, POIRuntimeStatusKind.Status,      nameof(RoamingNetwork), network.Id.ToString(), network.CurrentStatus);

                foreach (var chargingStationOperator in network.ChargingStationOperators)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ChargingStationOperator), chargingStationOperator.Id.ToString(), chargingStationOperator.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(ChargingStationOperator), chargingStationOperator.Id.ToString(), chargingStationOperator.CurrentStatus);
                }

                return statuses.
                           OrderBy(status => status.EntityType, StringComparer.Ordinal).
                           ThenBy (status => status.EntityId,   StringComparer.Ordinal).
                           ThenBy (status => status.Kind).
                           ToArray();

        }

        private static void Add<T>(List<POIStatusUpdate>  Statuses,
                                   POIRuntimeStatusKind   Kind,
                                   String                 EntityType,
                                   String                 EntityId,
                                   Timestamped<T>?        Current)
        {
            if (Current.HasValue)
                Statuses.Add(new POIStatusUpdate(Kind, EntityType, EntityId, Truncate(Current.Value.Timestamp), Current.Value.Value!.ToString()!));
        }

        #endregion


        #region (internal) TryTake(StatusUpdates, out ErrorResponse)

        /// <summary>
        /// Take the given statuses of the leading server as they are, e.g. those of a snapshot.
        /// A status of an unknown entity or an invalid one refuses all after it.
        /// </summary>
        internal Boolean TryTake(IEnumerable<POIStatusUpdate>      StatusUpdates,
                                 [NotNullWhen(false)] out String?  ErrorResponse)
        {

            foreach (var statusUpdate in StatusUpdates)
            {
                if (TryUpdate(statusUpdate, out var error) == POIStatusUpdateResult.Refused)
                {
                    ErrorResponse = error ?? "Invalid runtime status!";
                    return false;
                }
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (internal) Attach(RoamingNetwork)

        /// <summary>
        /// Listen to the status events of the entities of the given new head version,
        /// and no longer to those of entities it does not have any more.
        /// Entities a version shares with the one before are not listened to twice.
        /// </summary>
        internal void Attach(RoamingNetwork RoamingNetwork)
        {

            lock (attachLock)
            {

                var entities = new HashSet<Object>(ReferenceEqualityComparer.Instance) { RoamingNetwork };

                foreach (var chargingStationOperator in RoamingNetwork.ChargingStationOperators)
                    entities.Add(chargingStationOperator);

                foreach (var gone in listeners.Keys.Where(entity => !entities.Contains(entity)).ToArray())
                {
                    listeners[gone]();
                    listeners.Remove(gone);
                }

                if (!listeners.ContainsKey(RoamingNetwork))
                    listeners[RoamingNetwork] = ListenTo(RoamingNetwork);

                foreach (var chargingStationOperator in RoamingNetwork.ChargingStationOperators)
                    if (!listeners.ContainsKey(chargingStationOperator))
                        listeners[chargingStationOperator] = ListenTo(chargingStationOperator);

            }

            SchedulePlannedStatus();

        }

        #endregion

        #region (private) ListenTo(RoamingNetwork)

        private Action ListenTo(RoamingNetwork RoamingNetwork)
        {

            OnRoamingNetworkAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, roamingNetwork, newStatus, oldStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(RoamingNetwork), roamingNetwork.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnRoamingNetworkStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, roamingNetwork, newStatus, oldStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(RoamingNetwork), roamingNetwork.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            RoamingNetwork.OnAdminStatusChanged  += onAdminStatusChanged;
            RoamingNetwork.OnStatusChanged       += onStatusChanged;

            return () => {
                RoamingNetwork.OnAdminStatusChanged  -= onAdminStatusChanged;
                RoamingNetwork.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ChargingStationOperator)

        private Action ListenTo(ChargingStationOperator ChargingStationOperator)
        {

            OnChargingStationOperatorAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, chargingStationOperator, oldStatus, newStatus) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ChargingStationOperator), chargingStationOperator.Id.ToString(), newStatus, oldStatus, null);
                return Task.CompletedTask;
            };

            OnChargingStationOperatorStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, chargingStationOperator, oldStatus, newStatus) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ChargingStationOperator), chargingStationOperator.Id.ToString(), newStatus, oldStatus, null);
                return Task.CompletedTask;
            };

            ChargingStationOperator.OnAdminStatusChanged  += onAdminStatusChanged;
            ChargingStationOperator.OnStatusChanged       += onStatusChanged;

            return () => {
                ChargingStationOperator.OnAdminStatusChanged  -= onAdminStatusChanged;
                ChargingStationOperator.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) Raise<T>(Kind, EntityType, EntityId, NewStatus, OldStatus, DataSource)

        private void Raise<T>(POIRuntimeStatusKind  Kind,
                              String                EntityType,
                              String                EntityId,
                              Timestamped<T>        NewStatus,
                              Timestamped<T>?       OldStatus,
                              Context?              DataSource)
        {

            // A schedule without any status before has a default one.
            var hasOldStatus = OldStatus.HasValue && OldStatus.Value.Timestamp != default;

            var statusUpdate = new POIStatusUpdate(Kind,
                                                   EntityType,
                                                   EntityId,
                                                   Truncate(NewStatus.Timestamp),
                                                   NewStatus.Value!.ToString()!,
                                                   hasOldStatus ? Truncate(OldStatus!.Value.Timestamp) : null,
                                                   hasOldStatus ? OldStatus!.Value.Value?.ToString()   : null,
                                                   (DataSource as POIStatusContext)?.Text);

            // One failing subscriber must neither fail the update nor keep it from the others.
            foreach (var subscriber in OnStatusUpdate?.GetInvocationList().Cast<OnPOIStatusUpdateDelegate>() ?? [])
            {
                try
                {
                    subscriber(statusUpdate);
                }
                catch
                { }
            }

        }

        #endregion


        #region (private) SchedulePlannedStatus()

        /// <summary>
        /// Wake up when the next planned status of the head version is due.
        /// </summary>
        private void SchedulePlannedStatus()
        {

            var network  = history.Head.RoamingNetwork;

            var next     = new[] {
                               network.NextAdminStatus?.Timestamp,
                               network.NextStatus?.     Timestamp
                           }.
                           Concat(network.ChargingStationOperators.SelectMany(chargingStationOperator => new[] {
                               chargingStationOperator.NextAdminStatus?.Timestamp,
                               chargingStationOperator.NextStatus?.     Timestamp
                           })).
                           Where(timestamp => timestamp.HasValue).
                           Min();

            lock (timerLock)
            {

                if (!next.HasValue)
                {
                    plannedStatusTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    return;
                }

                // A millisecond late rather than early, when the schedule would not switch yet.
                var due = next.Value - Timestamp.Now + TimeSpan.FromMilliseconds(1);

                if (due < TimeSpan.Zero)
                    due = TimeSpan.Zero;

                if (due > maxPlannedStatusWait)
                    due = maxPlannedStatusWait;

                plannedStatusTimer.Change(due, Timeout.InfiniteTimeSpan);

            }

        }

        #endregion

        #region (private) TakePlannedStatus()

        /// <summary>
        /// Let every due planned status become the current one: reading the current status of a schedule
        /// does that, and raises its events.
        /// </summary>
        private void TakePlannedStatus()
        {

            try
            {

                var network = history.Head.RoamingNetwork;

                _ = network.AdminStatus;
                _ = network.Status;

                foreach (var chargingStationOperator in network.ChargingStationOperators)
                {
                    _ = chargingStationOperator.AdminStatus;
                    _ = chargingStationOperator.Status;
                }

            }
            catch
            { }

            SchedulePlannedStatus();

        }

        #endregion


        #region (private static) Set<T>(Text, Parse, Current, Set, At, out Normalized, out OldTimestamp, out OldValue)

        private static POIStatusUpdateResult Set<T>(String                               Text,
                                                    TryParseStatus<T>                    Parse,
                                                    Func<Timestamped<T>?>                Current,
                                                    Action<T, DateTimeOffset>            Insert,
                                                    Action<T, DateTimeOffset>            Take,
                                                    Boolean                              Authoritative,
                                                    DateTimeOffset                       At,
                                                    out String?                          Normalized,
                                                    out DateTimeOffset?                  OldTimestamp,
                                                    out String?                          OldValue)
        {

            Normalized    = null;
            OldTimestamp  = null;
            OldValue      = null;

            if (String.IsNullOrWhiteSpace(Text) || !Parse(Text, out var status))
                return POIStatusUpdateResult.Refused;

            Normalized = status!.ToString();

            // As it is, or into the schedule, which ignores a status repeating the one in effect before it.
            var Set = Authoritative ? Take : Insert;

            var current = Current();

            if (current.HasValue)
            {
                OldTimestamp  = Truncate(current.Value.Timestamp);
                OldValue      = current.Value.Value?.ToString();
            }

            if (At > Timestamp.Now)
            {
                Set(status, At);
                return POIStatusUpdateResult.Scheduled;
            }

            if (current.HasValue)
            {

                if (At < current.Value.Timestamp)
                {
                    Set(status, At);
                    return POIStatusUpdateResult.Outdated;
                }

                if (EqualityComparer<T>.Default.Equals(current.Value.Value, status))
                {

                    // The leader's "since" counts.
                    if (Authoritative && At > current.Value.Timestamp)
                        Set(status, At);

                    return POIStatusUpdateResult.Unchanged;

                }

            }

            Set(status, At);
            return POIStatusUpdateResult.Updated;

        }

        #endregion

        #region (private static) Unknown(EntityType, EntityId, out ErrorResponse)

        private static POIStatusUpdateResult Unknown(String       EntityType,
                                                     String       EntityId,
                                                     out String?  ErrorResponse)
        {
            ErrorResponse = $"The {EntityType} '{EntityId}' is unknown in the head version!";
            return POIStatusUpdateResult.Refused;
        }

        #endregion

        #region (private static) Truncate(Timestamp)

        private static DateTimeOffset Truncate(DateTimeOffset Timestamp)

            => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp.ToUnixTimeMilliseconds());

        #endregion

    }

}
