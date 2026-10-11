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
    /// Called for every status update, in their order, while the runtime status is locked:
    /// return quickly and do not update the runtime status from here.
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
        /// The entity has a status since a later time: the update is out of date.
        /// </summary>
        Outdated,

        /// <summary>
        /// The entity is unknown in the head version, or the status is invalid.
        /// </summary>
        Refused

    }


    /// <summary>
    /// The current admin status and status of the entities of the head version of a history.
    /// It is runtime data, not content: it has no ETags and no commits, and it is kept
    /// across versions. The status of an entity that a commit removes is dropped.
    /// </summary>
    /// <remarks>
    /// An update with an earlier timestamp than the current status is out of date,
    /// so that updates arriving out of order, e.g. at a follower, end with the latest one.
    /// The same status again keeps its timestamp: since when the entity has it.
    /// </remarks>
    public sealed class RoamingNetworkRuntimeStatus
    {

        #region Data

        private readonly Lock                                                                     statusLock  = new ();
        private readonly Dictionary<(POIRuntimeStatusKind, String, String), POIStatusUpdate>      current     = [];
        private readonly RoamingNetworkHistory                                                    history;

        #endregion

        #region Events

        /// <summary>
        /// Called for every status update, in their order, while the runtime status is locked:
        /// return quickly and do not update the runtime status from there.
        /// </summary>
        public event OnPOIStatusUpdateDelegate? OnStatusUpdate;

        #endregion

        #region Constructor(s)

        internal RoamingNetworkRuntimeStatus(RoamingNetworkHistory History)
        {
            this.history = History;
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
        /// <param name="Timestamp">Since when the entity has this status.</param>
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
        {

            StatusUpdate = null;

            lock (statusLock)
            {

                if (!IsKnown(history.Head.RoamingNetwork, EntityType, EntityId, out ErrorResponse))
                    return POIStatusUpdateResult.Refused;

                if (!TryNormalize(Kind, EntityType, Value, out var value))
                {
                    ErrorResponse = $"Invalid {POIStatusUpdate.KindText(Kind)} '{Value}' of a {EntityType}!";
                    return POIStatusUpdateResult.Refused;
                }

                var timestamp  = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp.ToUnixTimeMilliseconds());
                var key        = (Kind, EntityType, EntityId);

                if (current.TryGetValue(key, out var old))
                {

                    if (timestamp < old.Timestamp)
                    {
                        ErrorResponse = $"The {EntityType} '{EntityId}' has a {POIStatusUpdate.KindText(Kind)} since {old.Timestamp.ToISO8601()}!";
                        return POIStatusUpdateResult.Outdated;
                    }

                    if (old.Value == value)
                    {
                        ErrorResponse = null;
                        return POIStatusUpdateResult.Unchanged;
                    }

                }

                StatusUpdate   = new POIStatusUpdate(Kind,
                                                     EntityType,
                                                     EntityId,
                                                     timestamp,
                                                     value,
                                                     old?.Timestamp,
                                                     old?.Value,
                                                     DataSource);

                current[key]   = StatusUpdate with { OldTimestamp = null, OldValue = null };

                ErrorResponse  = null;

                RaiseStatusUpdate(StatusUpdate);

                return POIStatusUpdateResult.Updated;

            }

        }

        #endregion

        #region TryUpdate(StatusUpdate, out ErrorResponse)

        /// <summary>
        /// Take the status of the given status update, e.g. one pushed by the leading server.
        /// Its old status is ignored.
        /// </summary>
        /// <param name="StatusUpdate">A status update.</param>
        /// <param name="ErrorResponse">Why it was refused or is out of date.</param>
        public POIStatusUpdateResult TryUpdate(POIStatusUpdate  StatusUpdate,
                                               out String?      ErrorResponse)

            => TryUpdate(StatusUpdate.Kind,
                         StatusUpdate.EntityType,
                         StatusUpdate.EntityId,
                         StatusUpdate.Value,
                         StatusUpdate.Timestamp,
                         out _,
                         out ErrorResponse,
                         StatusUpdate.DataSource);

        #endregion

        #region TryGet(Kind, EntityType, EntityId, out Status)

        /// <summary>
        /// Try to return the current admin status or status of an entity, with since when it has it.
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
            lock (statusLock)
            {
                return current.TryGetValue((Kind, EntityType, EntityId), out Status);
            }
        }

        #endregion

        #region All

        /// <summary>
        /// The current admin status and status of all entities, without old values.
        /// </summary>
        public IReadOnlyList<POIStatusUpdate> All
        {
            get
            {
                lock (statusLock)
                {
                    return current.Values.
                               OrderBy(status => status.EntityType, StringComparer.Ordinal).
                               ThenBy (status => status.EntityId,   StringComparer.Ordinal).
                               ThenBy (status => status.Kind).
                               ToArray();
                }
            }
        }

        #endregion


        #region (internal) Prune(RoamingNetwork)

        /// <summary>
        /// Drop the status of every entity the given new head version no longer has.
        /// </summary>
        internal void Prune(RoamingNetwork RoamingNetwork)
        {
            lock (statusLock)
            {
                foreach (var key in current.Keys.Where(key => !IsKnown(RoamingNetwork, key.Item2, key.Item3, out _)).ToArray())
                    current.Remove(key);
            }
        }

        #endregion

        #region (private static) IsKnown(RoamingNetwork, EntityType, EntityId, out ErrorResponse)

        private static Boolean IsKnown(RoamingNetwork                    RoamingNetwork,
                                       String                            EntityType,
                                       String                            EntityId,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            switch (EntityType)
            {

                case nameof(POI.RoamingNetwork):
                    if (EntityId == RoamingNetwork.Id.ToString())
                    {
                        ErrorResponse = null;
                        return true;
                    }
                    break;

                case nameof(ChargingStationOperator):
                    if (ChargingStationOperator_Id.TryParse(EntityId, out var operatorId) &&
                        RoamingNetwork.ChargingStationOperatorExists(operatorId))
                    {
                        ErrorResponse = null;
                        return true;
                    }
                    break;

                default:
                    ErrorResponse = $"The entity type '{EntityType}' has no runtime status!";
                    return false;

            }

            ErrorResponse = $"The {EntityType} '{EntityId}' is unknown in the head version!";
            return false;

        }

        #endregion

        #region (private static) TryNormalize(Kind, EntityType, Value, out Normalized)

        /// <summary>
        /// Whether the given status is valid for the entity type, in its normalized text.
        /// </summary>
        private static Boolean TryNormalize(POIRuntimeStatusKind              Kind,
                                            String                            EntityType,
                                            String                            Value,
                                            [NotNullWhen(true)] out String?   Normalized)
        {

            Normalized = null;

            if (String.IsNullOrWhiteSpace(Value))
                return false;

            switch (EntityType, Kind)
            {

                case (nameof(POI.RoamingNetwork), POIRuntimeStatusKind.AdminStatus):
                    if (RoamingNetworkAdminStatusType.TryParse(Value, out var roamingNetworkAdminStatus))
                        Normalized = roamingNetworkAdminStatus.ToString();
                    break;

                case (nameof(POI.RoamingNetwork), POIRuntimeStatusKind.Status):
                    if (RoamingNetworkStatusType.TryParse(Value, out var roamingNetworkStatus))
                        Normalized = roamingNetworkStatus.ToString();
                    break;

                case (nameof(ChargingStationOperator), POIRuntimeStatusKind.AdminStatus):
                    if (ChargingStationOperatorAdminStatusType.TryParse(Value, out var operatorAdminStatus))
                        Normalized = operatorAdminStatus.ToString();
                    break;

                case (nameof(ChargingStationOperator), POIRuntimeStatusKind.Status):
                    if (ChargingStationOperatorStatusType.TryParse(Value, out var operatorStatus))
                        Normalized = operatorStatus.ToString();
                    break;

            }

            return Normalized is not null;

        }

        #endregion

        #region (private) RaiseStatusUpdate(StatusUpdate)

        private void RaiseStatusUpdate(POIStatusUpdate StatusUpdate)
        {

            // One failing subscriber must neither fail the update nor keep it from the others.
            foreach (var subscriber in OnStatusUpdate?.GetInvocationList().Cast<OnPOIStatusUpdateDelegate>() ?? [])
            {
                try
                {
                    subscriber(StatusUpdate);
                }
                catch
                { }
            }

        }

        #endregion

    }

}
