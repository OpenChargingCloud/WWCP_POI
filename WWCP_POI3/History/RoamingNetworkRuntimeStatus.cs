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
    /// either way OnStatusUpdate tells of it. A planned status is told of when it becomes the current one, and
    /// OnStatusPlanned tells that it is planned.
    /// </summary>
    /// <remarks>
    /// An update with an earlier timestamp than the current status goes into the history of the entity only,
    /// so that updates arriving out of order, e.g. at a peer, end with the latest one.
    /// The same status again keeps its timestamp: since when the entity has it. A status update of another
    /// server, TryUpdate(StatusUpdate, …), is taken as it is, though: its "since" counts, and the latest one wins.
    /// </remarks>
    public sealed partial class RoamingNetworkRuntimeStatus
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

        /// <summary>
        /// How to stop listening to the charging pools of the head version and to everything they have, by the charging pools.
        /// </summary>
        private readonly Dictionary<ChargingPool, List<Action>>  chargingPoolListeners  = new (ReferenceEqualityComparer.Instance);
        private readonly Lock                               timerLock             = new ();
        private readonly Timer                              plannedStatusTimer;

        /// <summary>
        /// When the next planned status the timer waits for is due, as SchedulePlannedStatus(…) found it last.
        /// </summary>
        private          DateTimeOffset?                    plannedStatusAt;

        /// <summary>
        /// The status of every charging tariff offer of the head version as its times tell it, as told last: an offer
        /// becoming active at its notBefore, or expiring after its notAfter, is told of by a status update.
        /// </summary>
        private readonly Dictionary<ChargingTariffOffer_Id, Timestamped<ChargingTariffOfferStatusType>>  offerStatuses      = [];
        private readonly Lock                                                                            offerStatusesLock  = new ();

        #endregion

        #region Events

        /// <summary>
        /// Called for every change of a current admin status or status of the head version, in their order,
        /// while the status schedule of the entity is locked: return quickly and do not set a status from there.
        /// </summary>
        public event OnPOIStatusUpdateDelegate? OnStatusUpdate;

        /// <summary>
        /// Called for every admin status, status or energy status planned by TryUpdate, with the timestamp it will be
        /// due at, e.g. a reservation or an announced maintenance; OnStatusUpdate tells of it again when it is due.
        /// Return quickly and do not set a status from there.
        /// </summary>
        public event OnPOIStatusUpdateDelegate? OnStatusPlanned;

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
        /// Take the status of the given status update of another server as it is: also the same status
        /// with a later timestamp, as its "since" counts, so that an older update arriving afterwards
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

        /// <summary>
        /// Set a status, unless a delegate calculates it, and calculate the status of the groups it changes afterwards,
        /// outside of the status schedule lock of the entity.
        /// </summary>
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

            if (IsRefusedAsCalculated(Kind, EntityType, EntityId, out var refused))
            {
                StatusUpdate   = null;
                ErrorResponse  = refused;
                return POIStatusUpdateResult.Refused;
            }

            updateDepth++;

            try
            {
                return UpdateEntity(Kind, EntityType, EntityId, Value, Timestamp, Authoritative, out StatusUpdate, out ErrorResponse, DataSource);
            }
            finally
            {
                if (--updateDepth == 0)
                    CalculateNow();
            }

        }

        #endregion

        #region (private) UpdateEntity(Kind, EntityType, EntityId, Value, Timestamp, Authoritative, out StatusUpdate, out ErrorResponse, DataSource)

        private POIStatusUpdateResult UpdateEntity(POIRuntimeStatusKind  Kind,
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

            if (Kind == POIRuntimeStatusKind.EnergyStatus &&
                EntityType is not (nameof(EVSE) or nameof(ChargingStation)))
            {
                ErrorResponse = $"The entity type '{EntityType}' has no energy status!";
                return POIStatusUpdateResult.Refused;
            }

            if (Kind == POIRuntimeStatusKind.PriceStatus &&
                EntityType is not (nameof(EVSE) or nameof(ChargingStation) or nameof(ChargingPool)))
            {
                ErrorResponse = $"The entity type '{EntityType}' has no price status!";
                return POIStatusUpdateResult.Refused;
            }

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

                case nameof(ChargingStationManufacturer):

                    if (!ChargingStationManufacturer_Id.TryParse(EntityId, out var manufacturerId) ||
                        !network.TryGetChargingStationManufacturerById(manufacturerId, out var manufacturer))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ChargingStationManufacturerAdminStatusType>(Value, ChargingStationManufacturerAdminStatusType.TryParse,
                                                    () => manufacturer.CurrentAdminStatus,
                                                    (status, timestamp) => manufacturer.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => manufacturer.SetAdminStatus([ new Timestamped<ChargingStationManufacturerAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ChargingStationManufacturerStatusType>(Value, ChargingStationManufacturerStatusType.TryParse,
                                                    () => manufacturer.CurrentStatus,
                                                    (status, timestamp) => manufacturer.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => manufacturer.SetStatus([ new Timestamped<ChargingStationManufacturerStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(EnergyMeterManufacturer):

                    if (!EnergyMeterManufacturer_Id.TryParse(EntityId, out var meterManufacturerId) ||
                        !network.TryGetEnergyMeterManufacturerById(meterManufacturerId, out var meterManufacturer))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<EnergyMeterManufacturerAdminStatusType>(Value, EnergyMeterManufacturerAdminStatusType.TryParse,
                                                    () => meterManufacturer.CurrentAdminStatus,
                                                    (status, timestamp) => meterManufacturer.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => meterManufacturer.SetAdminStatus([ new Timestamped<EnergyMeterManufacturerAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<EnergyMeterManufacturerStatusType>(Value, EnergyMeterManufacturerStatusType.TryParse,
                                                    () => meterManufacturer.CurrentStatus,
                                                    (status, timestamp) => meterManufacturer.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => meterManufacturer.SetStatus([ new Timestamped<EnergyMeterManufacturerStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(ConformityAssessmentBody):

                    if (!ConformityAssessmentBody_Id.TryParse(EntityId, out var bodyId) ||
                        !network.TryGetConformityAssessmentBodyById(bodyId, out var body))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ConformityAssessmentBodyAdminStatusType>(Value, ConformityAssessmentBodyAdminStatusType.TryParse,
                                                    () => body.CurrentAdminStatus,
                                                    (status, timestamp) => body.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => body.SetAdminStatus([ new Timestamped<ConformityAssessmentBodyAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ConformityAssessmentBodyStatusType>(Value, ConformityAssessmentBodyStatusType.TryParse,
                                                    () => body.CurrentStatus,
                                                    (status, timestamp) => body.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => body.SetStatus([ new Timestamped<ConformityAssessmentBodyStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(TransparencySoftwareManufacturer):

                    if (!TransparencySoftwareManufacturer_Id.TryParse(EntityId, out var softwareManufacturerId) ||
                        !network.TryGetTransparencySoftwareManufacturerById(softwareManufacturerId, out var softwareManufacturer))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<TransparencySoftwareManufacturerAdminStatusType>(Value, TransparencySoftwareManufacturerAdminStatusType.TryParse,
                                                    () => softwareManufacturer.CurrentAdminStatus,
                                                    (status, timestamp) => softwareManufacturer.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => softwareManufacturer.SetAdminStatus([ new Timestamped<TransparencySoftwareManufacturerAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<TransparencySoftwareManufacturerStatusType>(Value, TransparencySoftwareManufacturerStatusType.TryParse,
                                                    () => softwareManufacturer.CurrentStatus,
                                                    (status, timestamp) => softwareManufacturer.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => softwareManufacturer.SetStatus([ new Timestamped<TransparencySoftwareManufacturerStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(EMobilityProvider):

                    if (!EMobilityProvider_Id.TryParse(EntityId, out var eMobilityProviderId) ||
                        !network.TryGetEMobilityProviderById(eMobilityProviderId, out var eMobilityProvider))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<EMobilityProviderAdminStatusType>(Value, EMobilityProviderAdminStatusType.TryParse,
                                                    () => eMobilityProvider.CurrentAdminStatus,
                                                    (status, timestamp) => eMobilityProvider.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => eMobilityProvider.SetAdminStatus([ new Timestamped<EMobilityProviderAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<EMobilityProviderStatusType>(Value, EMobilityProviderStatusType.TryParse,
                                                    () => eMobilityProvider.CurrentStatus,
                                                    (status, timestamp) => eMobilityProvider.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => eMobilityProvider.SetStatus([ new Timestamped<EMobilityProviderStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(RoamingNetworkOperator):

                    if (!RoamingNetworkOperator_Id.TryParse(EntityId, out var roamingNetworkOperatorId) ||
                        network.Operator is not RoamingNetworkOperator roamingNetworkOperator ||
                        roamingNetworkOperator.Id != roamingNetworkOperatorId)
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<RoamingNetworkOperatorAdminStatusType>(Value, RoamingNetworkOperatorAdminStatusType.TryParse,
                                                    () => roamingNetworkOperator.CurrentAdminStatus,
                                                    (status, timestamp) => roamingNetworkOperator.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => roamingNetworkOperator.SetAdminStatus([ new Timestamped<RoamingNetworkOperatorAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<RoamingNetworkOperatorStatusType>(Value, RoamingNetworkOperatorStatusType.TryParse,
                                                    () => roamingNetworkOperator.CurrentStatus,
                                                    (status, timestamp) => roamingNetworkOperator.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => roamingNetworkOperator.SetStatus([ new Timestamped<RoamingNetworkOperatorStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(GridOperator):

                    if (!GridOperator_Id.TryParse(EntityId, out var gridOperatorId) ||
                        !network.TryGetGridOperatorById(gridOperatorId, out var gridOperator))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<GridOperatorAdminStatusType>(Value, GridOperatorAdminStatusType.TryParse,
                                                    () => gridOperator.CurrentAdminStatus,
                                                    (status, timestamp) => gridOperator.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => gridOperator.SetAdminStatus([ new Timestamped<GridOperatorAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<GridOperatorStatusType>(Value, GridOperatorStatusType.TryParse,
                                                    () => gridOperator.CurrentStatus,
                                                    (status, timestamp) => gridOperator.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => gridOperator.SetStatus([ new Timestamped<GridOperatorStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(EnergyMeter):

                    if (!EnergyMeter_Id.TryParse(EntityId, out var energyMeterId) ||
                        !network.TryGetEnergyMeterById(energyMeterId, out var energyMeter))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<EnergyMeterAdminStatusType>(Value, EnergyMeterAdminStatusType.TryParse,
                                                    () => energyMeter.CurrentAdminStatus,
                                                    (status, timestamp) => energyMeter.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => energyMeter.SetAdminStatus([ new Timestamped<EnergyMeterAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<EnergyMeterStatusType>(Value, EnergyMeterStatusType.TryParse,
                                                    () => energyMeter.CurrentStatus,
                                                    (status, timestamp) => energyMeter.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => energyMeter.SetStatus([ new Timestamped<EnergyMeterStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(ParkingOperator):

                    if (!ParkingOperator_Id.TryParse(EntityId, out var parkingOperatorId) ||
                        !network.TryGetParkingOperatorById(parkingOperatorId, out var parkingOperator))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ParkingOperatorAdminStatusType>(Value, ParkingOperatorAdminStatusType.TryParse,
                                                    () => parkingOperator.CurrentAdminStatus,
                                                    (status, timestamp) => parkingOperator.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => parkingOperator.SetAdminStatus([ new Timestamped<ParkingOperatorAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ParkingOperatorStatusType>(Value, ParkingOperatorStatusType.TryParse,
                                                    () => parkingOperator.CurrentStatus,
                                                    (status, timestamp) => parkingOperator.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => parkingOperator.SetStatus([ new Timestamped<ParkingOperatorStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(ParkingGarage):

                    if (!ParkingGarage_Id.TryParse(EntityId, out var parkingGarageId) ||
                        !network.TryGetParkingGarageById(parkingGarageId, out var parkingGarage))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ParkingGarageAdminStatusType>(Value, ParkingGarageAdminStatusType.TryParse,
                                                    () => parkingGarage.CurrentAdminStatus,
                                                    (status, timestamp) => parkingGarage.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => parkingGarage.SetAdminStatus([ new Timestamped<ParkingGarageAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ParkingGarageStatusType>(Value, ParkingGarageStatusType.TryParse,
                                                    () => parkingGarage.CurrentStatus,
                                                    (status, timestamp) => parkingGarage.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => parkingGarage.SetStatus([ new Timestamped<ParkingGarageStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(ParkingSpace):

                    if (!ParkingSpace_Id.TryParse(EntityId, out var parkingSpaceId) ||
                        !network.TryGetParkingSpaceById(parkingSpaceId, out var parkingSpace))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ParkingSpaceAdminStatusType>(Value, ParkingSpaceAdminStatusType.TryParse,
                                                    () => parkingSpace.CurrentAdminStatus,
                                                    (status, timestamp) => parkingSpace.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => parkingSpace.SetAdminStatus([ new Timestamped<ParkingSpaceAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ParkingSpaceStatusType>(Value, ParkingSpaceStatusType.TryParse,
                                                    () => parkingSpace.CurrentStatus,
                                                    (status, timestamp) => parkingSpace.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => parkingSpace.SetStatus([ new Timestamped<ParkingSpaceStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(ParkingSensor):

                    if (!ParkingSensor_Id.TryParse(EntityId, out var parkingSensorId) ||
                        !network.TryGetParkingSensorById(parkingSensorId, out var parkingSensor))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ParkingSensorAdminStatusType>(Value, ParkingSensorAdminStatusType.TryParse,
                                                    () => parkingSensor.CurrentAdminStatus,
                                                    (status, timestamp) => parkingSensor.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => parkingSensor.SetAdminStatus([ new Timestamped<ParkingSensorAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ParkingSensorStatusType>(Value, ParkingSensorStatusType.TryParse,
                                                    () => parkingSensor.CurrentStatus,
                                                    (status, timestamp) => parkingSensor.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => parkingSensor.SetStatus([ new Timestamped<ParkingSensorStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(ChargingTariff):

                    if (!ChargingTariff_Id.TryParse(EntityId, out var chargingTariffId) ||
                        !network.TryGetChargingTariffById(chargingTariffId, out var chargingTariff))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ChargingTariffAdminStatusType>(Value, ChargingTariffAdminStatusType.TryParse,
                                                    () => chargingTariff.CurrentAdminStatus,
                                                    (status, timestamp) => chargingTariff.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => chargingTariff.SetAdminStatus([ new Timestamped<ChargingTariffAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ChargingTariffStatusType>(Value, ChargingTariffStatusType.TryParse,
                                                    () => chargingTariff.CurrentStatus,
                                                    (status, timestamp) => chargingTariff.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => chargingTariff.SetStatus([ new Timestamped<ChargingTariffStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(EVSEGroup):

                    if (!EVSEGroup_Id.TryParse(EntityId, out var evseGroupId) ||
                        !network.TryGetEVSEGroupById(evseGroupId, out var evseGroup))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<EVSEGroupAdminStatusType>(Value, EVSEGroupAdminStatusType.TryParse,
                                                    () => evseGroup.CurrentAdminStatus,
                                                    (status, timestamp) => evseGroup.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => evseGroup.SetAdminStatus([ new Timestamped<EVSEGroupAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<EVSEGroupStatusType>(Value, EVSEGroupStatusType.TryParse,
                                                    () => evseGroup.CurrentStatus,
                                                    (status, timestamp) => evseGroup.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => evseGroup.SetStatus([ new Timestamped<EVSEGroupStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(ChargingStationGroup):

                    if (!ChargingStationGroup_Id.TryParse(EntityId, out var chargingStationGroupId) ||
                        !network.TryGetChargingStationGroupById(chargingStationGroupId, out var chargingStationGroup))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ChargingStationGroupAdminStatusType>(Value, ChargingStationGroupAdminStatusType.TryParse,
                                                    () => chargingStationGroup.CurrentAdminStatus,
                                                    (status, timestamp) => chargingStationGroup.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => chargingStationGroup.SetAdminStatus([ new Timestamped<ChargingStationGroupAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ChargingStationGroupStatusType>(Value, ChargingStationGroupStatusType.TryParse,
                                                    () => chargingStationGroup.CurrentStatus,
                                                    (status, timestamp) => chargingStationGroup.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => chargingStationGroup.SetStatus([ new Timestamped<ChargingStationGroupStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(ChargingPoolGroup):

                    if (!ChargingPoolGroup_Id.TryParse(EntityId, out var chargingPoolGroupId) ||
                        !network.TryGetChargingPoolGroupById(chargingPoolGroupId, out var chargingPoolGroup))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ChargingPoolGroupAdminStatusType>(Value, ChargingPoolGroupAdminStatusType.TryParse,
                                                    () => chargingPoolGroup.CurrentAdminStatus,
                                                    (status, timestamp) => chargingPoolGroup.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => chargingPoolGroup.SetAdminStatus([ new Timestamped<ChargingPoolGroupAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ChargingPoolGroupStatusType>(Value, ChargingPoolGroupStatusType.TryParse,
                                                    () => chargingPoolGroup.CurrentStatus,
                                                    (status, timestamp) => chargingPoolGroup.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => chargingPoolGroup.SetStatus([ new Timestamped<ChargingPoolGroupStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(ChargingTariffGroup):

                    if (!ChargingTariffGroup_Id.TryParse(EntityId, out var chargingTariffGroupId) ||
                        !network.TryGetChargingTariffGroupById(chargingTariffGroupId, out var chargingTariffGroup))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ChargingTariffGroupAdminStatusType>(Value, ChargingTariffGroupAdminStatusType.TryParse,
                                                    () => chargingTariffGroup.CurrentAdminStatus,
                                                    (status, timestamp) => chargingTariffGroup.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => chargingTariffGroup.SetAdminStatus([ new Timestamped<ChargingTariffGroupAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ChargingTariffGroupStatusType>(Value, ChargingTariffGroupStatusType.TryParse,
                                                    () => chargingTariffGroup.CurrentStatus,
                                                    (status, timestamp) => chargingTariffGroup.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => chargingTariffGroup.SetStatus([ new Timestamped<ChargingTariffGroupStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(ChargingTariffOffer):

                    if (!ChargingTariffOffer_Id.TryParse(EntityId, out var chargingTariffOfferId) ||
                        !network.TryGetChargingTariffOfferById(chargingTariffOfferId, out var chargingTariffOffer))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    // Its owner sets it exhausted; planned, active and expired follow from its times (StatusAt).
                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ChargingTariffOfferAdminStatusType>(Value, ChargingTariffOfferAdminStatusType.TryParse,
                                                    () => chargingTariffOffer.CurrentAdminStatus,
                                                    (status, timestamp) => chargingTariffOffer.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => chargingTariffOffer.SetAdminStatus([ new Timestamped<ChargingTariffOfferAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ChargingTariffOfferStatusType>(Value, ChargingTariffOfferStatusType.TryParse,
                                                    () => chargingTariffOffer.CurrentStatus,
                                                    (status, timestamp) => chargingTariffOffer.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => chargingTariffOffer.SetStatus([ new Timestamped<ChargingTariffOfferStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(ParkingSpaceGroup):

                    if (!ParkingSpaceGroup_Id.TryParse(EntityId, out var parkingSpaceGroupId) ||
                        !network.TryGetParkingSpaceGroupById(parkingSpaceGroupId, out var parkingSpaceGroup))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ParkingSpaceGroupAdminStatusType>(Value, ParkingSpaceGroupAdminStatusType.TryParse,
                                                    () => parkingSpaceGroup.CurrentAdminStatus,
                                                    (status, timestamp) => parkingSpaceGroup.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => parkingSpaceGroup.SetAdminStatus([ new Timestamped<ParkingSpaceGroupAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ParkingSpaceGroupStatusType>(Value, ParkingSpaceGroupStatusType.TryParse,
                                                    () => parkingSpaceGroup.CurrentStatus,
                                                    (status, timestamp) => parkingSpaceGroup.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => parkingSpaceGroup.SetStatus([ new Timestamped<ParkingSpaceGroupStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(EnergyMeterGroup):

                    if (!EnergyMeterGroup_Id.TryParse(EntityId, out var energyMeterGroupId) ||
                        !network.TryGetEnergyMeterGroupById(energyMeterGroupId, out var energyMeterGroup))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<EnergyMeterGroupAdminStatusType>(Value, EnergyMeterGroupAdminStatusType.TryParse,
                                                    () => energyMeterGroup.CurrentAdminStatus,
                                                    (status, timestamp) => energyMeterGroup.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => energyMeterGroup.SetAdminStatus([ new Timestamped<EnergyMeterGroupAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<EnergyMeterGroupStatusType>(Value, EnergyMeterGroupStatusType.TryParse,
                                                    () => energyMeterGroup.CurrentStatus,
                                                    (status, timestamp) => energyMeterGroup.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => energyMeterGroup.SetStatus([ new Timestamped<EnergyMeterGroupStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(ChargingPool):

                    if (!ChargingPool_Id.TryParse(EntityId, out var chargingPoolId) ||
                        !network.TryGetChargingStationOperatorById(chargingPoolId.OperatorId, out var chargingPoolOperator) ||
                        !chargingPoolOperator.TryGetChargingPool(chargingPoolId, out var chargingPool))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    if (Kind == POIRuntimeStatusKind.PriceStatus)
                    {

                        if (PriceError(Value, chargingPoolOperator, out var chargingPoolPriceError))
                        {
                            ErrorResponse = chargingPoolPriceError;
                            return POIStatusUpdateResult.Refused;
                        }

                        result = Set<ChargingPrices>(Value, TryParsePrices,
                                                     () => chargingPool.PriceStatus,
                                                     (priceStatus, timestamp) => chargingPool.SetPriceStatus (priceStatus, timestamp, context),
                                                     (priceStatus, timestamp) => chargingPool.TakePriceStatus(priceStatus, timestamp, context),
                                                     Authoritative, at, out normalized, out oldTimestamp, out oldValue,
                                                     ChargingPrices.ToText);
                        break;

                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ChargingPoolAdminStatusType>(Value, ChargingPoolAdminStatusType.TryParse,
                                                    () => chargingPool.CurrentAdminStatus,
                                                    (status, timestamp) => chargingPool.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => chargingPool.SetAdminStatus([ new Timestamped<ChargingPoolAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ChargingPoolStatusType>(Value, ChargingPoolStatusType.TryParse,
                                                    () => chargingPool.CurrentStatus,
                                                    (status, timestamp) => chargingPool.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => chargingPool.SetStatus([ new Timestamped<ChargingPoolStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(ChargingStation):

                    if (!ChargingStation_Id.TryParse(EntityId, out var chargingStationId) ||
                        !network.TryGetChargingStationOperatorById(chargingStationId.OperatorId, out var chargingStationOwner) ||
                        !chargingStationOwner.TryGetChargingStation(chargingStationId, out var chargingStation))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    if (Kind == POIRuntimeStatusKind.PriceStatus)
                    {

                        if (PriceError(Value, chargingStationOwner, out var chargingStationPriceError))
                        {
                            ErrorResponse = chargingStationPriceError;
                            return POIStatusUpdateResult.Refused;
                        }

                        result = Set<ChargingPrices>(Value, TryParsePrices,
                                                     () => chargingStation.PriceStatus,
                                                     (priceStatus, timestamp) => chargingStation.SetPriceStatus (priceStatus, timestamp, context),
                                                     (priceStatus, timestamp) => chargingStation.TakePriceStatus(priceStatus, timestamp, context),
                                                     Authoritative, at, out normalized, out oldTimestamp, out oldValue,
                                                     ChargingPrices.ToText);
                        break;

                    }

                    if (Kind == POIRuntimeStatusKind.EnergyStatus)
                    {

                        // Only an EVSE has a current voltage and a remaining charging time.
                        if (POIEnergyStatus.TryParseText(Value, out var stationEnergyStatus) && stationEnergyStatus.HasEVSEValues)
                        {
                            ErrorResponse = $"The energy status of the charging station '{EntityId}' must not have a current voltage or a remaining charging time: only an EVSE has them!";
                            return POIStatusUpdateResult.Refused;
                        }

                        result = Set<EnergyInfo>(Value, POIEnergyStatus.TryParseText,
                                                 () => chargingStation.EnergyStatus,
                                                 (energyStatus, timestamp) => chargingStation.SetEnergyStatus (energyStatus, timestamp, context),
                                                 (energyStatus, timestamp) => chargingStation.TakeEnergyStatus(energyStatus, timestamp, context),
                                                 Authoritative, at, out normalized, out oldTimestamp, out oldValue,
                                                 POIEnergyStatus.ToText);
                        break;
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<ChargingStationAdminStatusType>(Value, ChargingStationAdminStatusType.TryParse,
                                                    () => chargingStation.CurrentAdminStatus,
                                                    (status, timestamp) => chargingStation.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => chargingStation.SetAdminStatus([ new Timestamped<ChargingStationAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<ChargingStationStatusType>(Value, ChargingStationStatusType.TryParse,
                                                    () => chargingStation.CurrentStatus,
                                                    (status, timestamp) => chargingStation.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => chargingStation.SetStatus([ new Timestamped<ChargingStationStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue);
                    break;

                case nameof(EVSE):

                    if (!EVSE_Id.TryParse(EntityId, out var evseId) ||
                        !network.TryGetChargingStationOperatorById(evseId.OperatorId, out var evseOperator) ||
                        !evseOperator.TryGetEVSE(evseId, out var evse))
                    {
                        return Unknown(EntityType, EntityId, out ErrorResponse);
                    }

                    if (Kind == POIRuntimeStatusKind.PriceStatus)
                    {

                        if (PriceError(Value, evseOperator, out var evsePriceError))
                        {
                            ErrorResponse = evsePriceError;
                            return POIStatusUpdateResult.Refused;
                        }

                        result = Set<ChargingPrices>(Value, TryParsePrices,
                                                     () => evse.PriceStatus,
                                                     (priceStatus, timestamp) => evse.SetPriceStatus (priceStatus, timestamp, context),
                                                     (priceStatus, timestamp) => evse.TakePriceStatus(priceStatus, timestamp, context),
                                                     Authoritative, at, out normalized, out oldTimestamp, out oldValue,
                                                     ChargingPrices.ToText);
                        break;

                    }

                    if (Kind == POIRuntimeStatusKind.EnergyStatus)
                    {
                        result = Set<EnergyInfo>(Value, POIEnergyStatus.TryParseText,
                                                 () => evse.EnergyStatus,
                                                 (energyStatus, timestamp) => evse.SetEnergyStatus (energyStatus, timestamp, context),
                                                 (energyStatus, timestamp) => evse.TakeEnergyStatus(energyStatus, timestamp, context),
                                                 Authoritative, at, out normalized, out oldTimestamp, out oldValue,
                                                 POIEnergyStatus.ToText);
                        break;
                    }

                    result = Kind == POIRuntimeStatusKind.AdminStatus

                                 ? Set<EVSEAdminStatusType>(Value, EVSEAdminStatusType.TryParse,
                                                    () => evse.CurrentAdminStatus,
                                                    (status, timestamp) => evse.SetAdminStatus(status, timestamp, context),
                                                    (status, timestamp) => evse.SetAdminStatus([ new Timestamped<EVSEAdminStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
                                                    Authoritative, at, out normalized, out oldTimestamp, out oldValue)

                                 : Set<EVSEStatusType>(Value, EVSEStatusType.TryParse,
                                                    () => evse.CurrentStatus,
                                                    (status, timestamp) => evse.SetStatus(status, timestamp, context),
                                                    (status, timestamp) => evse.SetStatus([ new Timestamped<EVSEStatusType>(timestamp, status) ], ChangeMethods.Insert, context),
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
                    RaisePlanned(new POIStatusUpdate(Kind, EntityType, EntityId, at, normalized!, oldTimestamp, oldValue, DataSource));
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

                if (network.Operator is RoamingNetworkOperator roamingNetworkOperator)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(RoamingNetworkOperator), roamingNetworkOperator.Id.ToString(), roamingNetworkOperator.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(RoamingNetworkOperator), roamingNetworkOperator.Id.ToString(), roamingNetworkOperator.CurrentStatus);
                }

                foreach (var eMobilityProvider in network.EMobilityProviders)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(EMobilityProvider), eMobilityProvider.Id.ToString(), eMobilityProvider.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(EMobilityProvider), eMobilityProvider.Id.ToString(), eMobilityProvider.CurrentStatus);
                }

                foreach (var gridOperator in network.GridOperators)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(GridOperator), gridOperator.Id.ToString(), gridOperator.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(GridOperator), gridOperator.Id.ToString(), gridOperator.CurrentStatus);
                }

                foreach (var energyMeter in network.EnergyMeters)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(EnergyMeter), energyMeter.Id.ToString(), energyMeter.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(EnergyMeter), energyMeter.Id.ToString(), energyMeter.CurrentStatus);
                }

                foreach (var parkingOperator in network.ParkingOperators)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ParkingOperator), parkingOperator.Id.ToString(), parkingOperator.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(ParkingOperator), parkingOperator.Id.ToString(), parkingOperator.CurrentStatus);
                }

                foreach (var parkingGarage in network.ParkingGarages)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ParkingGarage), parkingGarage.Id.ToString(), parkingGarage.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(ParkingGarage), parkingGarage.Id.ToString(), parkingGarage.CurrentStatus);
                }

                foreach (var parkingSpace in network.ParkingSpaces)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ParkingSpace), parkingSpace.Id.ToString(), parkingSpace.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(ParkingSpace), parkingSpace.Id.ToString(), parkingSpace.CurrentStatus);
                }

                foreach (var parkingSensor in network.ParkingSensors)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ParkingSensor), parkingSensor.Id.ToString(), parkingSensor.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(ParkingSensor), parkingSensor.Id.ToString(), parkingSensor.CurrentStatus);
                }

                foreach (var chargingTariff in network.ChargingTariffs)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ChargingTariff), chargingTariff.Id.ToString(), chargingTariff.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(ChargingTariff), chargingTariff.Id.ToString(), chargingTariff.CurrentStatus);
                }

                foreach (var evseGroup in network.EVSEGroups)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(EVSEGroup), evseGroup.Id.ToString(), evseGroup.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(EVSEGroup), evseGroup.Id.ToString(), evseGroup.CurrentStatus);
                }

                foreach (var chargingStationGroup in network.ChargingStationGroups)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ChargingStationGroup), chargingStationGroup.Id.ToString(), chargingStationGroup.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(ChargingStationGroup), chargingStationGroup.Id.ToString(), chargingStationGroup.CurrentStatus);
                }

                foreach (var chargingPoolGroup in network.ChargingPoolGroups)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ChargingPoolGroup), chargingPoolGroup.Id.ToString(), chargingPoolGroup.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(ChargingPoolGroup), chargingPoolGroup.Id.ToString(), chargingPoolGroup.CurrentStatus);
                }

                foreach (var chargingTariffGroup in network.ChargingTariffGroups)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ChargingTariffGroup), chargingTariffGroup.Id.ToString(), chargingTariffGroup.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(ChargingTariffGroup), chargingTariffGroup.Id.ToString(), chargingTariffGroup.CurrentStatus);
                }

                // The status of an offer is calculated from its times, unless its owner set it exhausted.
                foreach (var chargingTariffOffer in network.ChargingTariffOffers)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ChargingTariffOffer), chargingTariffOffer.Id.ToString(), chargingTariffOffer.CurrentAdminStatus);
                    Add<ChargingTariffOfferStatusType>(statuses, POIRuntimeStatusKind.Status, nameof(ChargingTariffOffer), chargingTariffOffer.Id.ToString(), chargingTariffOffer.StatusSince(Timestamp.Now));
                }

                foreach (var parkingSpaceGroup in network.ParkingSpaceGroups)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ParkingSpaceGroup), parkingSpaceGroup.Id.ToString(), parkingSpaceGroup.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(ParkingSpaceGroup), parkingSpaceGroup.Id.ToString(), parkingSpaceGroup.CurrentStatus);
                }

                foreach (var energyMeterGroup in network.EnergyMeterGroups)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(EnergyMeterGroup), energyMeterGroup.Id.ToString(), energyMeterGroup.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(EnergyMeterGroup), energyMeterGroup.Id.ToString(), energyMeterGroup.CurrentStatus);
                }

                foreach (var manufacturer in network.ChargingStationManufacturers)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ChargingStationManufacturer), manufacturer.Id.ToString(), manufacturer.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(ChargingStationManufacturer), manufacturer.Id.ToString(), manufacturer.CurrentStatus);
                }

                foreach (var manufacturer in network.EnergyMeterManufacturers)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(EnergyMeterManufacturer),     manufacturer.Id.ToString(), manufacturer.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(EnergyMeterManufacturer),     manufacturer.Id.ToString(), manufacturer.CurrentStatus);
                }

                foreach (var manufacturer in network.TransparencySoftwareManufacturers)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(TransparencySoftwareManufacturer), manufacturer.Id.ToString(), manufacturer.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(TransparencySoftwareManufacturer), manufacturer.Id.ToString(), manufacturer.CurrentStatus);
                }

                foreach (var body in network.ConformityAssessmentBodies)
                {
                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ConformityAssessmentBody), body.Id.ToString(), body.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(ConformityAssessmentBody), body.Id.ToString(), body.CurrentStatus);
                }

                foreach (var chargingStationOperator in network.ChargingStationOperators)
                {

                    Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ChargingStationOperator), chargingStationOperator.Id.ToString(), chargingStationOperator.CurrentAdminStatus);
                    Add(statuses, POIRuntimeStatusKind.Status,      nameof(ChargingStationOperator), chargingStationOperator.Id.ToString(), chargingStationOperator.CurrentStatus);

                    foreach (var chargingPool in chargingStationOperator.ChargingPools)
                    {
                        Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ChargingPool),    chargingPool.Id.ToString(),    chargingPool.CurrentAdminStatus);
                        Add(statuses, POIRuntimeStatusKind.Status,      nameof(ChargingPool),    chargingPool.Id.ToString(),    chargingPool.CurrentStatus);
                        Add(statuses, POIRuntimeStatusKind.PriceStatus, nameof(ChargingPool),    chargingPool.Id.ToString(),    chargingPool.PriceStatus, ChargingPrices.ToText);
                    }

                    foreach (var chargingStation in chargingStationOperator.ChargingStations)
                    {
                        Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(ChargingStation), chargingStation.Id.ToString(), chargingStation.CurrentAdminStatus);
                        Add(statuses, POIRuntimeStatusKind.Status,      nameof(ChargingStation), chargingStation.Id.ToString(), chargingStation.CurrentStatus);
                        Add(statuses, POIRuntimeStatusKind.EnergyStatus, nameof(ChargingStation), chargingStation.Id.ToString(), chargingStation.EnergyStatus, POIEnergyStatus.ToText);
                        Add(statuses, POIRuntimeStatusKind.PriceStatus,  nameof(ChargingStation), chargingStation.Id.ToString(), chargingStation.PriceStatus,  ChargingPrices.ToText);
                    }

                    foreach (var evse in chargingStationOperator.EVSEs)
                    {
                        Add(statuses, POIRuntimeStatusKind.AdminStatus, nameof(EVSE),            evse.Id.ToString(),            evse.CurrentAdminStatus);
                        Add(statuses, POIRuntimeStatusKind.Status,      nameof(EVSE),            evse.Id.ToString(),            evse.CurrentStatus);
                        Add(statuses, POIRuntimeStatusKind.EnergyStatus, nameof(EVSE),           evse.Id.ToString(),            evse.EnergyStatus, POIEnergyStatus.ToText);
                        Add(statuses, POIRuntimeStatusKind.PriceStatus,  nameof(EVSE),           evse.Id.ToString(),            evse.PriceStatus,  ChargingPrices.ToText);
                    }

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
                                   Timestamped<T>?        Current,
                                   Func<T, String>?       Format   = null)
        {
            if (Current.HasValue)
                Statuses.Add(new POIStatusUpdate(Kind, EntityType, EntityId, Truncate(Current.Value.Timestamp), Format?.Invoke(Current.Value.Value) ?? Current.Value.Value!.ToString()!));
        }

        #endregion


        #region (internal) TryTake(StatusUpdates, out ErrorResponse)

        /// <summary>
        /// Take the given statuses of another server as they are, e.g. those of a snapshot.
        /// A status of an unknown entity or an invalid one refuses all after it.
        /// The status of a group a delegate calculates here is left out: the delegate has the say.
        /// </summary>
        internal Boolean TryTake(IEnumerable<POIStatusUpdate>      StatusUpdates,
                                 [NotNullWhen(false)] out String?  ErrorResponse)
        {

            foreach (var statusUpdate in StatusUpdates)
            {

                if (statusUpdate.Kind == POIRuntimeStatusKind.Status &&
                    IsCalculated(statusUpdate.EntityType, statusUpdate.EntityId))
                {
                    continue;
                }

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
        /// Entities a version shares with the one before are not listened to twice,
        /// and only what it has new may plan a status not looked at yet.
        /// A charging pool it shares is the same with everything it has: its charging stations, EVSEs and energy meters
        /// are not looked at again, so that a new head costs what it changed (SPECIFICATION.md, section 8.7).
        /// </summary>
        internal void Attach(RoamingNetwork RoamingNetwork)
        {

            var newEntities = new List<Object>();

            lock (attachLock)
            {

                #region The charging pools, with their charging stations, EVSEs and energy meters

                var chargingPools = new HashSet<ChargingPool>(RoamingNetwork.ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingPools),
                                                              ReferenceEqualityComparer.Instance);

                foreach (var gone in chargingPoolListeners.Keys.Where(chargingPool => !chargingPools.Contains(chargingPool)).ToArray())
                {

                    foreach (var stopListening in chargingPoolListeners[gone])
                        stopListening();

                    chargingPoolListeners.Remove(gone);

                }

                foreach (var chargingPool in chargingPools)
                {

                    if (chargingPoolListeners.ContainsKey(chargingPool))
                        continue;

                    var stopListening = new List<Action> { ListenTo(chargingPool) };
                    newEntities.Add(chargingPool);

                    foreach (var chargingStation in chargingPool.ChargingStations)
                    {
                        stopListening.Add(ListenTo(chargingStation));
                        newEntities.Add(chargingStation);
                    }

                    foreach (var evse in chargingPool.EVSEs)
                    {
                        stopListening.Add(ListenTo(evse));
                        newEntities.Add(evse);
                    }

                    foreach (var energyMeter in chargingPool.AllEnergyMeters)
                    {
                        stopListening.Add(ListenTo(energyMeter));
                        newEntities.Add(energyMeter);
                    }

                    chargingPoolListeners.Add(chargingPool, stopListening);

                }

                #endregion

                #region Everything else

                var entities = new HashSet<Object>(ReferenceEqualityComparer.Instance) { RoamingNetwork };

                if (RoamingNetwork.Operator is not null)
                    entities.Add(RoamingNetwork.Operator);

                entities.UnionWith(RoamingNetwork.ChargingStationManufacturers);
                entities.UnionWith(RoamingNetwork.EnergyMeterManufacturers);
                entities.UnionWith(RoamingNetwork.TransparencySoftwareManufacturers);
                entities.UnionWith(RoamingNetwork.ConformityAssessmentBodies);
                entities.UnionWith(RoamingNetwork.EVSEGroups);
                entities.UnionWith(RoamingNetwork.ChargingStationGroups);
                entities.UnionWith(RoamingNetwork.ChargingPoolGroups);
                entities.UnionWith(RoamingNetwork.ChargingTariffGroups);
                entities.UnionWith(RoamingNetwork.ChargingTariffOffers);
                entities.UnionWith(RoamingNetwork.ParkingSpaceGroups);
                entities.UnionWith(RoamingNetwork.EnergyMeterGroups);
                entities.UnionWith(RoamingNetwork.ChargingTariffs);
                entities.UnionWith(RoamingNetwork.ParkingOperators);
                entities.UnionWith(RoamingNetwork.ParkingGarages);
                entities.UnionWith(RoamingNetwork.ParkingSpaces);
                entities.UnionWith(RoamingNetwork.ParkingSensors);
                entities.UnionWith(RoamingNetwork.EMobilityProviders);
                entities.UnionWith(RoamingNetwork.GridOperators);
                entities.UnionWith(RoamingNetwork.ChargingStationOperators);

                // Those of the grid connection points: the others are those of the charging pools.
                entities.UnionWith(RoamingNetwork.GridOperators.SelectMany(gridOperator => gridOperator.EnergyMeters));

                foreach (var gone in listeners.Keys.Where(entity => !entities.Contains(entity)).ToArray())
                {
                    listeners[gone]();
                    listeners.Remove(gone);
                }

                foreach (var entity in entities)
                {

                    if (listeners.ContainsKey(entity))
                        continue;

                    listeners[entity] = entity switch {
                                            RoamingNetwork               roamingNetwork           => ListenTo(roamingNetwork),
                                            RoamingNetworkOperator       roamingNetworkOperator   => ListenTo(roamingNetworkOperator),
                                            EMobilityProvider            eMobilityProvider        => ListenTo(eMobilityProvider),
                                            GridOperator                 gridOperator             => ListenTo(gridOperator),
                                            EnergyMeter                  energyMeter              => ListenTo(energyMeter),
                                            ParkingOperator              parkingOperator          => ListenTo(parkingOperator),
                                            ParkingGarage                parkingGarage            => ListenTo(parkingGarage),
                                            ParkingSpace                 parkingSpace             => ListenTo(parkingSpace),
                                            ParkingSensor                parkingSensor            => ListenTo(parkingSensor),
                                            ChargingTariff               chargingTariff           => ListenTo(chargingTariff),
                                            EVSEGroup                    evseGroup                => ListenTo(evseGroup),
                                            ChargingStationGroup         chargingStationGroup     => ListenTo(chargingStationGroup),
                                            ChargingPoolGroup            chargingPoolGroup        => ListenTo(chargingPoolGroup),
                                            ChargingTariffGroup          chargingTariffGroup      => ListenTo(chargingTariffGroup),
                                            ChargingTariffOffer          chargingTariffOffer      => ListenTo(chargingTariffOffer),
                                            ParkingSpaceGroup            parkingSpaceGroup        => ListenTo(parkingSpaceGroup),
                                            EnergyMeterGroup             energyMeterGroup         => ListenTo(energyMeterGroup),
                                            ChargingStationManufacturer  manufacturer             => ListenTo(manufacturer),
                                            EnergyMeterManufacturer      meterManufacturer        => ListenTo(meterManufacturer),
                                            TransparencySoftwareManufacturer  softwareManufacturer  => ListenTo(softwareManufacturer),
                                            ConformityAssessmentBody     conformityAssessmentBody  => ListenTo(conformityAssessmentBody),
                                            ChargingStationOperator      chargingStationOperator  => ListenTo(chargingStationOperator),
                                            _                                                     => throw new InvalidOperationException($"No status events of a {entity.GetType().Name}!")
                                        };

                    newEntities.Add(entity);

                }

                #endregion

            }

            SchedulePlannedStatus(newEntities);

            // Its offers may have other times now.
            TakeOfferStatus(RoamingNetwork);

            // Its groups may have other members now.
            HeadChanged(RoamingNetwork);

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

        #region (private) ListenTo(ChargingStationManufacturer)

        private Action ListenTo(ChargingStationManufacturer Entity)
        {

            OnChargingStationManufacturerAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ChargingStationManufacturer), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnChargingStationManufacturerStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ChargingStationManufacturer), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ConformityAssessmentBody)

        private Action ListenTo(ConformityAssessmentBody Entity)
        {

            OnConformityAssessmentBodyAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ConformityAssessmentBody), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnConformityAssessmentBodyStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ConformityAssessmentBody), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(TransparencySoftwareManufacturer)

        private Action ListenTo(TransparencySoftwareManufacturer Entity)
        {

            OnTransparencySoftwareManufacturerAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(TransparencySoftwareManufacturer), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnTransparencySoftwareManufacturerStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(TransparencySoftwareManufacturer), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(EnergyMeterManufacturer)

        private Action ListenTo(EnergyMeterManufacturer Entity)
        {

            OnEnergyMeterManufacturerAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(EnergyMeterManufacturer), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnEnergyMeterManufacturerStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(EnergyMeterManufacturer), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(EMobilityProvider)

        private Action ListenTo(EMobilityProvider Entity)
        {

            OnEMobilityProviderAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(EMobilityProvider), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnEMobilityProviderStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(EMobilityProvider), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(RoamingNetworkOperator)

        private Action ListenTo(RoamingNetworkOperator Entity)
        {

            OnRoamingNetworkOperatorAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(RoamingNetworkOperator), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnRoamingNetworkOperatorStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(RoamingNetworkOperator), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(GridOperator)

        private Action ListenTo(GridOperator Entity)
        {

            OnGridOperatorAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(GridOperator), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnGridOperatorStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(GridOperator), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(EnergyMeter)

        private Action ListenTo(EnergyMeter Entity)
        {

            OnEnergyMeterAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(EnergyMeter), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnEnergyMeterStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(EnergyMeter), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ParkingOperator)

        private Action ListenTo(ParkingOperator Entity)
        {

            OnParkingOperatorAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ParkingOperator), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnParkingOperatorStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ParkingOperator), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ParkingGarage)

        private Action ListenTo(ParkingGarage Entity)
        {

            OnParkingGarageAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ParkingGarage), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnParkingGarageStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ParkingGarage), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ParkingSpace)

        private Action ListenTo(ParkingSpace Entity)
        {

            OnParkingSpaceAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ParkingSpace), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnParkingSpaceStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ParkingSpace), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ParkingSensor)

        private Action ListenTo(ParkingSensor Entity)
        {

            OnParkingSensorAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ParkingSensor), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnParkingSensorStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ParkingSensor), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ChargingTariff)

        private Action ListenTo(ChargingTariff Entity)
        {

            OnChargingTariffAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ChargingTariff), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnChargingTariffStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ChargingTariff), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(EVSEGroup)

        private Action ListenTo(EVSEGroup Entity)
        {

            OnEVSEGroupAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(EVSEGroup), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnEVSEGroupStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(EVSEGroup), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ChargingStationGroup)

        private Action ListenTo(ChargingStationGroup Entity)
        {

            OnChargingStationGroupAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ChargingStationGroup), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnChargingStationGroupStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ChargingStationGroup), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ChargingPoolGroup)

        private Action ListenTo(ChargingPoolGroup Entity)
        {

            OnChargingPoolGroupAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ChargingPoolGroup), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnChargingPoolGroupStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ChargingPoolGroup), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ChargingTariffGroup)

        private Action ListenTo(ChargingTariffGroup Entity)
        {

            OnChargingTariffGroupAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ChargingTariffGroup), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnChargingTariffGroupStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ChargingTariffGroup), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ChargingTariffOffer)

        private Action ListenTo(ChargingTariffOffer Entity)
        {

            OnChargingTariffOfferAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ChargingTariffOffer), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnChargingTariffOfferStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ChargingTariffOffer), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(EnergyMeterGroup)

        private Action ListenTo(EnergyMeterGroup Entity)
        {

            OnEnergyMeterGroupAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(EnergyMeterGroup), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnEnergyMeterGroupStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(EnergyMeterGroup), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ParkingSpaceGroup)

        private Action ListenTo(ParkingSpaceGroup Entity)
        {

            OnParkingSpaceGroupAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ParkingSpaceGroup), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnParkingSpaceGroupStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ParkingSpaceGroup), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ChargingPool)

        private Action ListenTo(ChargingPool Entity)
        {

            OnChargingPoolAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ChargingPool), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnChargingPoolStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ChargingPool), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnChargingPoolPriceStatusChangedDelegate onPriceStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.PriceStatus, nameof(ChargingPool), entity.Id.ToString(), newStatus, oldStatus, dataSource, ChargingPrices.ToText);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged  += onAdminStatusChanged;
            Entity.OnStatusChanged       += onStatusChanged;
            Entity.OnPriceStatusChanged  += onPriceStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged  -= onAdminStatusChanged;
                Entity.OnStatusChanged       -= onStatusChanged;
                Entity.OnPriceStatusChanged  -= onPriceStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ChargingStation)

        private Action ListenTo(ChargingStation Entity)
        {

            OnChargingStationAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ChargingStation), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnChargingStationStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ChargingStation), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnChargingStationEnergyStatusChangedDelegate onEnergyStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.EnergyStatus, nameof(ChargingStation), entity.Id.ToString(), newStatus, oldStatus, dataSource, POIEnergyStatus.ToText);
                return Task.CompletedTask;
            };

            OnChargingStationPriceStatusChangedDelegate onPriceStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.PriceStatus, nameof(ChargingStation), entity.Id.ToString(), newStatus, oldStatus, dataSource, ChargingPrices.ToText);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged   += onAdminStatusChanged;
            Entity.OnStatusChanged        += onStatusChanged;
            Entity.OnEnergyStatusChanged  += onEnergyStatusChanged;
            Entity.OnPriceStatusChanged   += onPriceStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged   -= onAdminStatusChanged;
                Entity.OnStatusChanged        -= onStatusChanged;
                Entity.OnEnergyStatusChanged  -= onEnergyStatusChanged;
                Entity.OnPriceStatusChanged   -= onPriceStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(EVSE)

        private Action ListenTo(EVSE Entity)
        {

            OnEVSEAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(EVSE), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnEVSEStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(EVSE), entity.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnEVSEEnergyStatusChangedDelegate onEnergyStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.EnergyStatus, nameof(EVSE), entity.Id.ToString(), newStatus, oldStatus, dataSource, POIEnergyStatus.ToText);
                return Task.CompletedTask;
            };

            OnEVSEPriceStatusChangedDelegate onPriceStatusChanged = (timestamp, eventTrackingId, entity, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.PriceStatus, nameof(EVSE), entity.Id.ToString(), newStatus, oldStatus, dataSource, ChargingPrices.ToText);
                return Task.CompletedTask;
            };

            Entity.OnAdminStatusChanged   += onAdminStatusChanged;
            Entity.OnStatusChanged        += onStatusChanged;
            Entity.OnEnergyStatusChanged  += onEnergyStatusChanged;
            Entity.OnPriceStatusChanged   += onPriceStatusChanged;

            return () => {
                Entity.OnAdminStatusChanged   -= onAdminStatusChanged;
                Entity.OnStatusChanged        -= onStatusChanged;
                Entity.OnEnergyStatusChanged  -= onEnergyStatusChanged;
                Entity.OnPriceStatusChanged   -= onPriceStatusChanged;
            };

        }

        #endregion

        #region (private) ListenTo(ChargingStationOperator)

        private Action ListenTo(ChargingStationOperator ChargingStationOperator)
        {

            OnChargingStationOperatorAdminStatusChangedDelegate onAdminStatusChanged = (timestamp, eventTrackingId, chargingStationOperator, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.AdminStatus, nameof(ChargingStationOperator), chargingStationOperator.Id.ToString(), newStatus, oldStatus, dataSource);
                return Task.CompletedTask;
            };

            OnChargingStationOperatorStatusChangedDelegate onStatusChanged = (timestamp, eventTrackingId, chargingStationOperator, oldStatus, newStatus, dataSource) => {
                Raise(POIRuntimeStatusKind.Status, nameof(ChargingStationOperator), chargingStationOperator.Id.ToString(), newStatus, oldStatus, dataSource);
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
                              Context?              DataSource,
                              Func<T, String>?      Format   = null)
        {

            // A schedule without any status before has a default one.
            var hasOldStatus = OldStatus.HasValue && OldStatus.Value.Timestamp != default;

            var statusUpdate = new POIStatusUpdate(Kind,
                                                   EntityType,
                                                   EntityId,
                                                   Truncate(NewStatus.Timestamp),
                                                   Format?.Invoke(NewStatus.Value) ?? NewStatus.Value!.ToString()!,
                                                   hasOldStatus ? Truncate(OldStatus!.Value.Timestamp) : null,
                                                   hasOldStatus ? (Format is not null ? Format(OldStatus!.Value.Value) : OldStatus!.Value.Value?.ToString()) : null,
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

            // The groups it is a member of may have another status now.
            if (Kind == POIRuntimeStatusKind.Status)
                MemberStatusChanged(EntityType, EntityId, statusUpdate.Timestamp);

        }

        #endregion


        #region (private static) TryParsePrices(Text, out Prices) / PriceError(Value, Operator, out ErrorResponse)

        private static Boolean TryParsePrices(String Text, out ChargingPrices Prices)
        {
            var parsed = ChargingPrices.TryParseText(Text, out var prices);
            Prices = prices!;
            return parsed;
        }

        /// <summary>
        /// Whether the given price status names a charging tariff the given operator does not have: the prices of a
        /// charging pool, station or EVSE are those of tariffs of its operator.
        /// </summary>
        private static Boolean PriceError(String                                     Value,
                                          ChargingStationOperator                    Operator,
                                          [NotNullWhen(true)] out String?            ErrorResponse)
        {

            ErrorResponse = null;

            if (!ChargingPrices.TryParseText(Value, out var prices))
                return false;

            foreach (var price in prices.Prices)
                if (!Operator.ChargingTariffs.Any(tariff => tariff.Id == price.ChargingTariffId))
                {
                    ErrorResponse = $"The charging tariff '{price.ChargingTariffId}' of the price status is no tariff of the charging station operator '{Operator.Id}'!";
                    return true;
                }

            return false;

        }

        #endregion

        #region (private) RaisePlanned(StatusUpdate)

        /// <summary>
        /// Tell of a planned status; one failing subscriber must neither fail the update nor keep it from the others.
        /// </summary>
        private void RaisePlanned(POIStatusUpdate StatusUpdate)
        {
            foreach (var subscriber in OnStatusPlanned?.GetInvocationList().Cast<OnPOIStatusUpdateDelegate>() ?? [])
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

        #region (private) SchedulePlannedStatus(NewEntities = null)

        /// <summary>
        /// Wake up when the next planned status of the head version is due: of all its entities, or, for a new head version,
        /// of the given entities it has new and of its charging tariff offers, unless the timer is due earlier already. The
        /// other entities share their schedules with the versions before, so what they plan was looked at; one the head no
        /// longer has lets the timer look again, in vain.
        /// </summary>
        /// <param name="NewEntities">Optionally, the entities a new head version has new.</param>
        private void SchedulePlannedStatus(IEnumerable<Object>? NewEntities = null)
        {

            var network  = history.Head.RoamingNetwork;
            var now      = Timestamp.Now;

            var next     = (NewEntities is null
                                ? EntitiesOf(network)
                                : NewEntities.Concat(network.ChargingTariffOffers)).
                           SelectMany(entity => NextStatusOf(entity, now)).
                           Where     (timestamp => timestamp.HasValue).
                           Min();

            lock (timerLock)
            {

                if (NewEntities is not null && plannedStatusAt.HasValue && (!next.HasValue || plannedStatusAt.Value <= next.Value))
                    return;

                plannedStatusAt = next;

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

        #region (private static) EntitiesOf(RoamingNetwork)

        /// <summary>
        /// The entities of the given version having a runtime status that may be planned.
        /// </summary>
        private static IEnumerable<Object> EntitiesOf(RoamingNetwork RoamingNetwork)
        {

            yield return RoamingNetwork;

            foreach (var chargingStationOperator in RoamingNetwork.ChargingStationOperators)
            {

                yield return chargingStationOperator;

                foreach (var chargingPool in chargingStationOperator.ChargingPools)
                    yield return chargingPool;

                foreach (var chargingStation in chargingStationOperator.ChargingStations)
                    yield return chargingStation;

                foreach (var evse in chargingStationOperator.EVSEs)
                    yield return evse;

            }

            foreach (var entity in RoamingNetwork.EMobilityProviders.          Cast<Object>().
                                   Concat(RoamingNetwork.GridOperators).
                                   Concat(RoamingNetwork.EnergyMeters).
                                   Concat(RoamingNetwork.ParkingOperators).
                                   Concat(RoamingNetwork.ParkingGarages).
                                   Concat(RoamingNetwork.ParkingSpaces).
                                   Concat(RoamingNetwork.ParkingSensors).
                                   Concat(RoamingNetwork.ChargingTariffs).
                                   Concat(RoamingNetwork.EVSEGroups).
                                   Concat(RoamingNetwork.ChargingStationGroups).
                                   Concat(RoamingNetwork.ChargingPoolGroups).
                                   Concat(RoamingNetwork.ChargingTariffGroups).
                                   Concat(RoamingNetwork.ChargingTariffOffers).
                                   Concat(RoamingNetwork.ParkingSpaceGroups).
                                   Concat(RoamingNetwork.EnergyMeterGroups).
                                   Concat(RoamingNetwork.ChargingStationManufacturers).
                                   Concat(RoamingNetwork.EnergyMeterManufacturers).
                                   Concat(RoamingNetwork.TransparencySoftwareManufacturers).
                                   Concat(RoamingNetwork.ConformityAssessmentBodies))
            {
                yield return entity;
            }

        }

        #endregion

        #region (private static) NextStatusOf(Entity, Now)

        /// <summary>
        /// When the next planned admin status, status, energy status and price status of the given entity are due, as far as
        /// it has them; for a charging tariff offer also when its times change its status next.
        /// </summary>
        private static IEnumerable<DateTimeOffset?> NextStatusOf(Object          Entity,
                                                                 DateTimeOffset  Now)

            => Entity switch {
                   RoamingNetwork               network               => [ network.              NextAdminStatus?.Timestamp, network.              NextStatus?.Timestamp ],
                   ChargingStationOperator      chargingStationOperator => [ chargingStationOperator.NextAdminStatus?.Timestamp, chargingStationOperator.NextStatus?.Timestamp ],
                   ChargingPool                 chargingPool          => [ chargingPool.         NextAdminStatus?.Timestamp, chargingPool.         NextStatus?.Timestamp, chargingPool.   NextPriceStatus?.Timestamp ],
                   ChargingStation              chargingStation       => [ chargingStation.      NextAdminStatus?.Timestamp, chargingStation.      NextStatus?.Timestamp, chargingStation.NextEnergyStatus?.Timestamp, chargingStation.NextPriceStatus?.Timestamp ],
                   EVSE                         evse                  => [ evse.                 NextAdminStatus?.Timestamp, evse.                 NextStatus?.Timestamp, evse.           NextEnergyStatus?.Timestamp, evse.           NextPriceStatus?.Timestamp ],
                   EMobilityProvider            eMobilityProvider     => [ eMobilityProvider.    NextAdminStatus?.Timestamp, eMobilityProvider.    NextStatus?.Timestamp ],
                   GridOperator                 gridOperator          => [ gridOperator.         NextAdminStatus?.Timestamp, gridOperator.         NextStatus?.Timestamp ],
                   EnergyMeter                  energyMeter           => [ energyMeter.          NextAdminStatus?.Timestamp, energyMeter.          NextStatus?.Timestamp ],
                   ParkingOperator              parkingOperator       => [ parkingOperator.      NextAdminStatus?.Timestamp, parkingOperator.      NextStatus?.Timestamp ],
                   ParkingGarage                parkingGarage         => [ parkingGarage.        NextAdminStatus?.Timestamp, parkingGarage.        NextStatus?.Timestamp ],
                   ParkingSpace                 parkingSpace          => [ parkingSpace.         NextAdminStatus?.Timestamp, parkingSpace.         NextStatus?.Timestamp ],
                   ParkingSensor                parkingSensor         => [ parkingSensor.        NextAdminStatus?.Timestamp, parkingSensor.        NextStatus?.Timestamp ],
                   ChargingTariff               chargingTariff        => [ chargingTariff.       NextAdminStatus?.Timestamp, chargingTariff.       NextStatus?.Timestamp ],
                   EVSEGroup                    evseGroup             => [ evseGroup.            NextAdminStatus?.Timestamp, evseGroup.            NextStatus?.Timestamp ],
                   ChargingStationGroup         chargingStationGroup  => [ chargingStationGroup. NextAdminStatus?.Timestamp, chargingStationGroup. NextStatus?.Timestamp ],
                   ChargingPoolGroup            chargingPoolGroup     => [ chargingPoolGroup.    NextAdminStatus?.Timestamp, chargingPoolGroup.    NextStatus?.Timestamp ],
                   ChargingTariffGroup          chargingTariffGroup   => [ chargingTariffGroup.  NextAdminStatus?.Timestamp, chargingTariffGroup.  NextStatus?.Timestamp ],
                   ChargingTariffOffer          chargingTariffOffer   => [ chargingTariffOffer.  NextAdminStatus?.Timestamp, chargingTariffOffer.  NextStatus?.Timestamp, chargingTariffOffer.NextStatusChange(Now) ],
                   ParkingSpaceGroup            parkingSpaceGroup     => [ parkingSpaceGroup.    NextAdminStatus?.Timestamp, parkingSpaceGroup.    NextStatus?.Timestamp ],
                   EnergyMeterGroup             energyMeterGroup      => [ energyMeterGroup.     NextAdminStatus?.Timestamp, energyMeterGroup.     NextStatus?.Timestamp ],
                   ChargingStationManufacturer  manufacturer          => [ manufacturer.         NextAdminStatus?.Timestamp, manufacturer.         NextStatus?.Timestamp ],
                   EnergyMeterManufacturer      meterManufacturer     => [ meterManufacturer.    NextAdminStatus?.Timestamp, meterManufacturer.    NextStatus?.Timestamp ],
                   TransparencySoftwareManufacturer  softwareManufacturer  => [ softwareManufacturer.NextAdminStatus?.Timestamp, softwareManufacturer.NextStatus?.Timestamp ],
                   ConformityAssessmentBody     conformityAssessmentBody  => [ conformityAssessmentBody.NextAdminStatus?.Timestamp, conformityAssessmentBody.NextStatus?.Timestamp ],
                   _                                                  => []
               };

        #endregion

        #region (private) TakeOfferStatus(RoamingNetwork)

        /// <summary>
        /// Tell of every charging tariff offer of the given head version whose status, as its times tell it, is another
        /// one than told last: it becomes active at its notBefore and expires after its notAfter, and no planned status of
        /// its schedule tells so. An offer new in the version is not told of, the version tells it; its owner setting it
        /// exhausted, or no longer, its schedule tells.
        /// </summary>
        private void TakeOfferStatus(RoamingNetwork RoamingNetwork)
        {

            var now      = Timestamp.Now;
            var changed  = new List<(ChargingTariffOffer_Id Id, Timestamped<ChargingTariffOfferStatusType> New, Timestamped<ChargingTariffOfferStatusType> Old)>();

            lock (offerStatusesLock)
            {

                var offers = RoamingNetwork.ChargingTariffOffers.ToDictionary(chargingTariffOffer => chargingTariffOffer.Id);

                foreach (var gone in offerStatuses.Keys.Where(offerId => !offers.ContainsKey(offerId)).ToArray())
                    offerStatuses.Remove(gone);

                foreach (var offer in offers.Values)
                {

                    var status = offer.StatusSince(now);

                    if (offerStatuses.TryGetValue(offer.Id, out var told) &&
                        told.Value   != status.Value &&
                        told.Value   != ChargingTariffOfferStatusType.Exhausted &&
                        status.Value != ChargingTariffOfferStatusType.Exhausted)
                    {
                        changed.Add((offer.Id, status, told));
                    }

                    offerStatuses[offer.Id] = status;

                }

            }

            foreach (var (offerId, newStatus, oldStatus) in changed)
                Raise(POIRuntimeStatusKind.Status, nameof(ChargingTariffOffer), offerId.ToString(), newStatus, oldStatus, null);

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

                foreach (var eMobilityProvider in network.EMobilityProviders)
                {
                    _ = eMobilityProvider.AdminStatus;
                    _ = eMobilityProvider.Status;
                }

                foreach (var gridOperator in network.GridOperators)
                {
                    _ = gridOperator.AdminStatus;
                    _ = gridOperator.Status;
                }

                foreach (var energyMeter in network.EnergyMeters)
                {
                    _ = energyMeter.AdminStatus;
                    _ = energyMeter.Status;
                }

                foreach (var parkingOperator in network.ParkingOperators)
                {
                    _ = parkingOperator.AdminStatus;
                    _ = parkingOperator.Status;
                }

                foreach (var parkingGarage in network.ParkingGarages)
                {
                    _ = parkingGarage.AdminStatus;
                    _ = parkingGarage.Status;
                }

                foreach (var parkingSpace in network.ParkingSpaces)
                {
                    _ = parkingSpace.AdminStatus;
                    _ = parkingSpace.Status;
                }

                foreach (var parkingSensor in network.ParkingSensors)
                {
                    _ = parkingSensor.AdminStatus;
                    _ = parkingSensor.Status;
                }

                foreach (var chargingTariff in network.ChargingTariffs)
                {
                    _ = chargingTariff.AdminStatus;
                    _ = chargingTariff.Status;
                }

                foreach (var evseGroup in network.EVSEGroups)
                {
                    _ = evseGroup.AdminStatus;
                    _ = evseGroup.Status;
                }

                foreach (var chargingStationGroup in network.ChargingStationGroups)
                {
                    _ = chargingStationGroup.AdminStatus;
                    _ = chargingStationGroup.Status;
                }

                foreach (var chargingPoolGroup in network.ChargingPoolGroups)
                {
                    _ = chargingPoolGroup.AdminStatus;
                    _ = chargingPoolGroup.Status;
                }

                foreach (var chargingTariffGroup in network.ChargingTariffGroups)
                {
                    _ = chargingTariffGroup.AdminStatus;
                    _ = chargingTariffGroup.Status;
                }

                foreach (var chargingTariffOffer in network.ChargingTariffOffers)
                {
                    _ = chargingTariffOffer.AdminStatus;
                    _ = chargingTariffOffer.Status;
                }

                TakeOfferStatus(network);

                foreach (var parkingSpaceGroup in network.ParkingSpaceGroups)
                {
                    _ = parkingSpaceGroup.AdminStatus;
                    _ = parkingSpaceGroup.Status;
                }

                foreach (var energyMeterGroup in network.EnergyMeterGroups)
                {
                    _ = energyMeterGroup.AdminStatus;
                    _ = energyMeterGroup.Status;
                }

                foreach (var manufacturer in network.ChargingStationManufacturers)
                {
                    _ = manufacturer.AdminStatus;
                    _ = manufacturer.Status;
                }

                foreach (var manufacturer in network.EnergyMeterManufacturers)
                {
                    _ = manufacturer.AdminStatus;
                    _ = manufacturer.Status;
                }

                foreach (var manufacturer in network.TransparencySoftwareManufacturers)
                {
                    _ = manufacturer.AdminStatus;
                    _ = manufacturer.Status;
                }

                foreach (var body in network.ConformityAssessmentBodies)
                {
                    _ = body.AdminStatus;
                    _ = body.Status;
                }

                foreach (var chargingStationOperator in network.ChargingStationOperators)
                {

                    _ = chargingStationOperator.AdminStatus;
                    _ = chargingStationOperator.Status;

                    foreach (var chargingPool in chargingStationOperator.ChargingPools)
                    {
                        _ = chargingPool.AdminStatus;
                        _ = chargingPool.Status;
                        _ = chargingPool.PriceStatus;
                    }

                    foreach (var chargingStation in chargingStationOperator.ChargingStations)
                    {
                        _ = chargingStation.AdminStatus;
                        _ = chargingStation.Status;
                        _ = chargingStation.EnergyStatus;
                        _ = chargingStation.PriceStatus;
                    }

                    foreach (var evse in chargingStationOperator.EVSEs)
                    {
                        _ = evse.AdminStatus;
                        _ = evse.Status;
                        _ = evse.EnergyStatus;
                        _ = evse.PriceStatus;
                    }

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
                                                    out String?                          OldValue,
                                                    Func<T, String>?                     Format   = null)
        {

            Normalized    = null;
            OldTimestamp  = null;
            OldValue      = null;

            if (String.IsNullOrWhiteSpace(Text) || !Parse(Text, out var status))
                return POIStatusUpdateResult.Refused;

            Normalized = Format?.Invoke(status) ?? status!.ToString();

            // As it is, or into the schedule, which ignores a status repeating the one in effect before it.
            var Set = Authoritative ? Take : Insert;

            var current = Current();

            if (current.HasValue)
            {
                OldTimestamp  = Truncate(current.Value.Timestamp);
                OldValue      = Format is not null ? Format(current.Value.Value) : current.Value.Value?.ToString();
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

                    // The "since" of the other server counts.
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
