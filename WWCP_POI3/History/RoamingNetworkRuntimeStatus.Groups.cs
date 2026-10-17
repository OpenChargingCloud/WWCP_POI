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
    /// Calculates the status of a group from the current status of its members, e.g. POIGroupStatus.OfEVSEs.
    /// Members the head version does not have, and members without a status yet, are left out.
    /// </summary>
    /// <typeparam name="TGroup">The type of the group.</typeparam>
    /// <typeparam name="TMemberId">The identification of its members.</typeparam>
    /// <typeparam name="TMemberStatus">The status of its members.</typeparam>
    /// <typeparam name="TGroupStatus">The status of the group.</typeparam>
    /// <param name="Group">The group, of the head version.</param>
    /// <param name="Members">The current status of its members, with since when they have it.</param>
    public delegate TGroupStatus POIGroupStatusCalculator<TGroup, TMemberId, TMemberStatus, TGroupStatus>(TGroup                                                 Group,
                                                                                                       IReadOnlyDictionary<TMemberId, Timestamped<TMemberStatus>>  Members)

        where TMemberId : notnull;


    /// <summary>
    /// The status of groups: set manually, or calculated by a delegate from the status of their members.
    /// </summary>
    /// <remarks>
    /// A group whose status is calculated refuses every other status, also one of another server: the delegate
    /// has the say. Its status is calculated again whenever the status of one of its members changes, since then,
    /// and whenever a new head version may have changed its members, since that version's last change; a
    /// calculated status is a status like every other one: kept, told of by OnStatusUpdate, and sent to peers.
    /// Only the status is calculated, never the admin status. A calculation runs after the status that caused it
    /// was set, outside of every status schedule lock.
    /// </remarks>
    public sealed partial class RoamingNetworkRuntimeStatus
    {

        #region (private interface) IGroupKind

        /// <summary>
        /// One kind of group: how to find its groups and members, and which delegates calculate their status.
        /// </summary>
        private interface IGroupKind
        {

            String                                          GroupType     { get; }

            String                                          MemberType    { get; }

            Boolean                                         IsCalculated  (RoamingNetwork Network, String GroupId);

            IEnumerable<String>                             Calculated    (RoamingNetwork Network);

            IEnumerable<String>                             ContainingMember(RoamingNetwork Network, String MemberId);

            (String Value, DateTimeOffset Since)?           Calculate     (RoamingNetwork Network, String GroupId);

        }

        #endregion

        #region (private class) GroupKind<TGroup, TGroupId, TMemberId, TMemberStatus, TGroupStatus>

        private sealed class GroupKind<TGroup, TGroupId, TMemberId, TMemberStatus, TGroupStatus> : IGroupKind

            where TGroup        : class
            where TGroupId      : struct
            where TMemberId     : struct
            where TGroupStatus  : notnull

        {

            private readonly Lock                                                                                         calculatorsLock  = new ();
            private readonly Dictionary<TGroupId, POIGroupStatusCalculator<TGroup, TMemberId, TMemberStatus, TGroupStatus>?>  perGroup         = [];
            private          POIGroupStatusCalculator<TGroup, TMemberId, TMemberStatus, TGroupStatus>?                    forAll;

            public required String                                                    GroupType     { get; init; }
            public required String                                                    MemberType    { get; init; }
            public required Func<String, TGroupId?>                                   ParseGroupId  { get; init; }
            public required Func<String, TMemberId?>                                  ParseMemberId { get; init; }
            public required Func<RoamingNetwork, IEnumerable<TGroup>>                 Groups        { get; init; }
            public required Func<RoamingNetwork, TGroupId, TGroup?>                   Group         { get; init; }
            public required Func<TGroup, TGroupId>                                    IdOf          { get; init; }
            public required Func<TGroup, IEnumerable<TMemberId>>                      MembersOf     { get; init; }
            public required Func<TGroup, Timestamped<TGroupStatus>?>                  StatusOf      { get; init; }
            public required Func<RoamingNetwork, TMemberId, Timestamped<TMemberStatus>?>  MemberStatus  { get; init; }


            #region Calculate(GroupId, Calculator) / SetManually(GroupId) / CalculateAll(Calculator)

            public void Calculate(TGroupId GroupId, POIGroupStatusCalculator<TGroup, TMemberId, TMemberStatus, TGroupStatus> Calculator)
            {
                lock (calculatorsLock)
                    perGroup[GroupId] = Calculator;
            }

            /// <summary>
            /// The status of the given group is set manually, also while a delegate calculates that of all others.
            /// </summary>
            public void SetManually(TGroupId GroupId)
            {
                lock (calculatorsLock)
                    perGroup[GroupId] = null;
            }

            public void CalculateAll(POIGroupStatusCalculator<TGroup, TMemberId, TMemberStatus, TGroupStatus>? Calculator)
            {
                lock (calculatorsLock)
                    forAll = Calculator;
            }

            #endregion

            #region (private) CalculatorOf(GroupId)

            private POIGroupStatusCalculator<TGroup, TMemberId, TMemberStatus, TGroupStatus>? CalculatorOf(TGroupId GroupId)
            {
                lock (calculatorsLock)
                    return perGroup.TryGetValue(GroupId, out var calculator)
                               ? calculator
                               : forAll;
            }

            #endregion


            #region IGroupKind

            public Boolean IsCalculated(RoamingNetwork Network, String GroupId)

                => ParseGroupId(GroupId) is TGroupId groupId &&
                   CalculatorOf(groupId) is not null;

            public IEnumerable<String> Calculated(RoamingNetwork Network)

                => Groups(Network).
                       Where (group => CalculatorOf(IdOf(group)) is not null).
                       Select(group => IdOf(group).ToString()!).
                       ToArray();

            public IEnumerable<String> ContainingMember(RoamingNetwork Network, String MemberId)

                => ParseMemberId(MemberId) is TMemberId memberId
                       ? Groups(Network).
                             Where (group => MembersOf(group).Contains(memberId) &&
                                             CalculatorOf(IdOf(group)) is not null).
                             Select(group => IdOf(group).ToString()!).
                             ToArray()
                       : [];

            public (String Value, DateTimeOffset Since)? Calculate(RoamingNetwork Network, String GroupId)
            {

                if (ParseGroupId(GroupId) is not TGroupId groupId ||
                    Group(Network, groupId) is not TGroup group     ||
                    CalculatorOf(groupId) is not { } calculator)
                {
                    return null;
                }

                var members = new Dictionary<TMemberId, Timestamped<TMemberStatus>>();

                foreach (var memberId in MembersOf(group))
                    if (MemberStatus(Network, memberId) is Timestamped<TMemberStatus> memberStatus)
                        members[memberId] = memberStatus;

                var since = members.Count > 0
                                ? members.Values.Max(status => status.Timestamp)
                                : Network.LastChangeDate;

                // A status since before the current one would be out of date.
                if (StatusOf(group) is Timestamped<TGroupStatus> current && current.Timestamp > since)
                    since = current.Timestamp;

                return (calculator(group, members).ToString()!, since);

            }

            #endregion

        }

        #endregion


        #region Data

        private readonly GroupKind<EVSEGroup, EVSEGroup_Id, EVSE_Id, EVSEStatusType, EVSEGroupStatusType> evseGroups

            = Kind<EVSEGroup, EVSEGroup_Id, EVSE_Id, EVSEStatusType, EVSEGroupStatusType>(
                  nameof(EVSEGroup),
                  nameof(EVSE),
                  text            => EVSEGroup_Id.TryParse(text, out var id) ? id : null,
                  text            => EVSE_Id.     TryParse(text, out var id) ? id : null,
                  network         => network.EVSEGroups,
                  (network, id)   => network.TryGetEVSEGroupById(id, out var group) ? group : null,
                  group           => group.Id,
                  group           => group.EVSEIds,
                  group           => group.CurrentStatus,
                  (network, id)   => OperatorOf(network, id.OperatorId) is ChargingStationOperator chargingStationOperator &&
                                     chargingStationOperator.TryGetEVSE(id, out var evse)
                                         ? evse.CurrentStatus
                                         : null);

        private readonly GroupKind<ChargingStationGroup, ChargingStationGroup_Id, ChargingStation_Id, ChargingStationStatusType, ChargingStationGroupStatusType> chargingStationGroups

            = Kind<ChargingStationGroup, ChargingStationGroup_Id, ChargingStation_Id, ChargingStationStatusType, ChargingStationGroupStatusType>(
                  nameof(ChargingStationGroup),
                  nameof(ChargingStation),
                  text            => ChargingStationGroup_Id.TryParse(text, out var id) ? id : null,
                  text            => ChargingStation_Id.     TryParse(text, out var id) ? id : null,
                  network         => network.ChargingStationGroups,
                  (network, id)   => network.TryGetChargingStationGroupById(id, out var group) ? group : null,
                  group           => group.Id,
                  group           => group.ChargingStationIds,
                  group           => group.CurrentStatus,
                  (network, id)   => OperatorOf(network, id.OperatorId) is ChargingStationOperator chargingStationOperator &&
                                     chargingStationOperator.TryGetChargingStation(id, out var chargingStation)
                                         ? chargingStation.CurrentStatus
                                         : null);

        private readonly GroupKind<ChargingPoolGroup, ChargingPoolGroup_Id, ChargingPool_Id, ChargingPoolStatusType, ChargingPoolGroupStatusType> chargingPoolGroups

            = Kind<ChargingPoolGroup, ChargingPoolGroup_Id, ChargingPool_Id, ChargingPoolStatusType, ChargingPoolGroupStatusType>(
                  nameof(ChargingPoolGroup),
                  nameof(ChargingPool),
                  text            => ChargingPoolGroup_Id.TryParse(text, out var id) ? id : null,
                  text            => ChargingPool_Id.     TryParse(text, out var id) ? id : null,
                  network         => network.ChargingPoolGroups,
                  (network, id)   => network.TryGetChargingPoolGroupById(id, out var group) ? group : null,
                  group           => group.Id,
                  group           => group.ChargingPoolIds,
                  group           => group.CurrentStatus,
                  (network, id)   => OperatorOf(network, id.OperatorId) is ChargingStationOperator chargingStationOperator &&
                                     chargingStationOperator.TryGetChargingPool(id, out var chargingPool)
                                         ? chargingPool.CurrentStatus
                                         : null);

        private readonly GroupKind<ChargingTariffGroup, ChargingTariffGroup_Id, ChargingTariff_Id, ChargingTariffStatusType, ChargingTariffGroupStatusType> chargingTariffGroups

            = Kind<ChargingTariffGroup, ChargingTariffGroup_Id, ChargingTariff_Id, ChargingTariffStatusType, ChargingTariffGroupStatusType>(
                  nameof(ChargingTariffGroup),
                  nameof(ChargingTariff),
                  text            => ChargingTariffGroup_Id.TryParse(text, out var id) ? id : null,
                  text            => ChargingTariff_Id.     TryParse(text, out var id) ? id : null,
                  network         => network.ChargingTariffGroups,
                  (network, id)   => network.TryGetChargingTariffGroupById(id, out var group) ? group : null,
                  group           => group.Id,
                  group           => group.ChargingTariffIds,
                  group           => group.CurrentStatus,
                  (network, id)   => network.TryGetChargingTariffById(id, out var chargingTariff)
                                         ? chargingTariff.CurrentStatus
                                         : null);

        private readonly GroupKind<ParkingSpaceGroup, ParkingSpaceGroup_Id, ParkingSpace_Id, ParkingSpaceStatusType, ParkingSpaceGroupStatusType> parkingSpaceGroups

            = Kind<ParkingSpaceGroup, ParkingSpaceGroup_Id, ParkingSpace_Id, ParkingSpaceStatusType, ParkingSpaceGroupStatusType>(
                  nameof(ParkingSpaceGroup),
                  nameof(ParkingSpace),
                  text            => ParkingSpaceGroup_Id.TryParse(text, out var id) ? id : null,
                  text            => ParkingSpace_Id.     TryParse(text, out var id) ? id : null,
                  network         => network.ParkingSpaceGroups,
                  (network, id)   => network.TryGetParkingSpaceGroupById(id, out var group) ? group : null,
                  group           => group.Id,
                  group           => group.ParkingSpaceIds,
                  group           => group.CurrentStatus,
                  (network, id)   => network.TryGetParkingSpaceById(id, out var parkingSpace)
                                         ? parkingSpace.CurrentStatus
                                         : null);

        private readonly GroupKind<EnergyMeterGroup, EnergyMeterGroup_Id, EnergyMeter_Id, EnergyMeterStatusType, EnergyMeterGroupStatusType> energyMeterGroups

            = Kind<EnergyMeterGroup, EnergyMeterGroup_Id, EnergyMeter_Id, EnergyMeterStatusType, EnergyMeterGroupStatusType>(
                  nameof(EnergyMeterGroup),
                  nameof(EnergyMeter),
                  text            => EnergyMeterGroup_Id.TryParse(text, out var id) ? id : null,
                  text            => EnergyMeter_Id.     TryParse(text, out var id) ? id : null,
                  network         => network.EnergyMeterGroups,
                  (network, id)   => network.TryGetEnergyMeterGroupById(id, out var group) ? group : null,
                  group           => group.Id,
                  group           => group.EnergyMeterIds,
                  group           => group.CurrentStatus,
                  (network, id)   => network.TryGetEnergyMeterById(id, out var energyMeter)
                                         ? energyMeter.CurrentStatus
                                         : null);

        private IGroupKind[] groupKinds

            => [ evseGroups, chargingStationGroups, chargingPoolGroups, chargingTariffGroups, parkingSpaceGroups, energyMeterGroups ];

        /// <summary>
        /// The groups whose status is to be calculated again, with since when at least.
        /// </summary>
        private readonly Dictionary<(String GroupType, String GroupId), DateTimeOffset>  pendingCalculations  = [];
        private readonly Lock                                                             pendingLock          = new ();
        private readonly Lock                                                             calculationLock      = new ();

        /// <summary>
        /// How deep this thread is in setting a status here: calculations wait until it is out again.
        /// </summary>
        [ThreadStatic]
        private static Int32    updateDepth;

        /// <summary>
        /// Whether this thread is calculating: its status updates of groups are not refused.
        /// </summary>
        [ThreadStatic]
        private static Boolean  calculating;

        #endregion


        #region (private) CreateGroupKinds()

        private static GroupKind<TGroup, TGroupId, TMemberId, TMemberStatus, TGroupStatus> Kind<TGroup, TGroupId, TMemberId, TMemberStatus, TGroupStatus>(

            String                                                         GroupType,
            String                                                         MemberType,
            Func<String, TGroupId?>                                        ParseGroupId,
            Func<String, TMemberId?>                                       ParseMemberId,
            Func<RoamingNetwork, IEnumerable<TGroup>>                      Groups,
            Func<RoamingNetwork, TGroupId, TGroup?>                        Group,
            Func<TGroup, TGroupId>                                         IdOf,
            Func<TGroup, IEnumerable<TMemberId>>                           MembersOf,
            Func<TGroup, Timestamped<TGroupStatus>?>                       StatusOf,
            Func<RoamingNetwork, TMemberId, Timestamped<TMemberStatus>?>   MemberStatus)

            where TGroup        : class
            where TGroupId      : struct
            where TMemberId     : struct
            where TGroupStatus  : notnull

            => new () {
                   GroupType      = GroupType,
                   MemberType     = MemberType,
                   ParseGroupId   = ParseGroupId,
                   ParseMemberId  = ParseMemberId,
                   Groups         = Groups,
                   Group          = Group,
                   IdOf           = IdOf,
                   MembersOf      = MembersOf,
                   StatusOf       = StatusOf,
                   MemberStatus   = MemberStatus
               };

        private static ChargingStationOperator? OperatorOf(RoamingNetwork Network, ChargingStationOperator_Id OperatorId)

            => Network.TryGetChargingStationOperatorById(OperatorId, out var chargingStationOperator)
                   ? chargingStationOperator
                   : null;

        #endregion


        #region CalculateStatus(GroupId, Calculator)

        /// <summary>
        /// Calculate the status of the given EVSE group with the given delegate, e.g. POIGroupStatus.OfEVSEs.
        /// Its status set otherwise is refused from now on.
        /// </summary>
        public void CalculateStatus(EVSEGroup_Id GroupId, POIGroupStatusCalculator<EVSEGroup, EVSE_Id, EVSEStatusType, EVSEGroupStatusType> Calculator)
            => Calculate(evseGroups, GroupId, kind => kind.Calculate(GroupId, Calculator));

        /// <summary>
        /// Calculate the status of the given charging station group with the given delegate, e.g. POIGroupStatus.OfChargingStations.
        /// Its status set otherwise is refused from now on.
        /// </summary>
        public void CalculateStatus(ChargingStationGroup_Id GroupId, POIGroupStatusCalculator<ChargingStationGroup, ChargingStation_Id, ChargingStationStatusType, ChargingStationGroupStatusType> Calculator)
            => Calculate(chargingStationGroups, GroupId, kind => kind.Calculate(GroupId, Calculator));

        /// <summary>
        /// Calculate the status of the given charging pool group with the given delegate, e.g. POIGroupStatus.OfChargingPools.
        /// Its status set otherwise is refused from now on.
        /// </summary>
        public void CalculateStatus(ChargingPoolGroup_Id GroupId, POIGroupStatusCalculator<ChargingPoolGroup, ChargingPool_Id, ChargingPoolStatusType, ChargingPoolGroupStatusType> Calculator)
            => Calculate(chargingPoolGroups, GroupId, kind => kind.Calculate(GroupId, Calculator));

        /// <summary>
        /// Calculate the status of the given charging tariff group with the given delegate, e.g. POIGroupStatus.OfChargingTariffs.
        /// Its status set otherwise is refused from now on.
        /// </summary>
        public void CalculateStatus(ChargingTariffGroup_Id GroupId, POIGroupStatusCalculator<ChargingTariffGroup, ChargingTariff_Id, ChargingTariffStatusType, ChargingTariffGroupStatusType> Calculator)
            => Calculate(chargingTariffGroups, GroupId, kind => kind.Calculate(GroupId, Calculator));

        /// <summary>
        /// Calculate the status of the given parking space group with the given delegate, e.g. POIGroupStatus.OfParkingSpaces.
        /// Its status set otherwise is refused from now on.
        /// </summary>
        public void CalculateStatus(ParkingSpaceGroup_Id GroupId, POIGroupStatusCalculator<ParkingSpaceGroup, ParkingSpace_Id, ParkingSpaceStatusType, ParkingSpaceGroupStatusType> Calculator)
            => Calculate(parkingSpaceGroups, GroupId, kind => kind.Calculate(GroupId, Calculator));

        /// <summary>
        /// Calculate the status of the given energy meter group with the given delegate, e.g. POIGroupStatus.OfEnergyMeters.
        /// Its status set otherwise is refused from now on.
        /// </summary>
        public void CalculateStatus(EnergyMeterGroup_Id GroupId, POIGroupStatusCalculator<EnergyMeterGroup, EnergyMeter_Id, EnergyMeterStatusType, EnergyMeterGroupStatusType> Calculator)
            => Calculate(energyMeterGroups, GroupId, kind => kind.Calculate(GroupId, Calculator));

        #endregion

        #region SetStatusManually(GroupId)

        /// <summary>
        /// Set the status of the given EVSE group manually again, also while a delegate calculates that of all others.
        /// </summary>
        public void SetStatusManually(EVSEGroup_Id GroupId)
            => evseGroups.SetManually(GroupId);

        /// <summary>
        /// Set the status of the given charging station group manually again, also while a delegate calculates that of all others.
        /// </summary>
        public void SetStatusManually(ChargingStationGroup_Id GroupId)
            => chargingStationGroups.SetManually(GroupId);

        /// <summary>
        /// Set the status of the given charging pool group manually again, also while a delegate calculates that of all others.
        /// </summary>
        public void SetStatusManually(ChargingPoolGroup_Id GroupId)
            => chargingPoolGroups.SetManually(GroupId);

        /// <summary>
        /// Set the status of the given charging tariff group manually again, also while a delegate calculates that of all others.
        /// </summary>
        public void SetStatusManually(ChargingTariffGroup_Id GroupId)
            => chargingTariffGroups.SetManually(GroupId);

        /// <summary>
        /// Set the status of the given parking space group manually again, also while a delegate calculates that of all others.
        /// </summary>
        public void SetStatusManually(ParkingSpaceGroup_Id GroupId)
            => parkingSpaceGroups.SetManually(GroupId);

        /// <summary>
        /// Set the status of the given energy meter group manually again, also while a delegate calculates that of all others.
        /// </summary>
        public void SetStatusManually(EnergyMeterGroup_Id GroupId)
            => energyMeterGroups.SetManually(GroupId);

        #endregion

        #region CalculateStatusOfAll…Groups(Calculator)

        /// <summary>
        /// Calculate the status of every EVSE group with the given delegate, unless the group has a delegate of its own
        /// or is set manually; null sets them all manually again.
        /// </summary>
        public void CalculateStatusOfAllEVSEGroups(POIGroupStatusCalculator<EVSEGroup, EVSE_Id, EVSEStatusType, EVSEGroupStatusType>? Calculator)
            => Calculate(evseGroups, null, kind => kind.CalculateAll(Calculator));

        /// <summary>
        /// Calculate the status of every charging station group with the given delegate, unless the group has a delegate
        /// of its own or is set manually; null sets them all manually again.
        /// </summary>
        public void CalculateStatusOfAllChargingStationGroups(POIGroupStatusCalculator<ChargingStationGroup, ChargingStation_Id, ChargingStationStatusType, ChargingStationGroupStatusType>? Calculator)
            => Calculate(chargingStationGroups, null, kind => kind.CalculateAll(Calculator));

        /// <summary>
        /// Calculate the status of every charging pool group with the given delegate, unless the group has a delegate
        /// of its own or is set manually; null sets them all manually again.
        /// </summary>
        public void CalculateStatusOfAllChargingPoolGroups(POIGroupStatusCalculator<ChargingPoolGroup, ChargingPool_Id, ChargingPoolStatusType, ChargingPoolGroupStatusType>? Calculator)
            => Calculate(chargingPoolGroups, null, kind => kind.CalculateAll(Calculator));

        /// <summary>
        /// Calculate the status of every charging tariff group with the given delegate, unless the group has a delegate
        /// of its own or is set manually; null sets them all manually again.
        /// </summary>
        public void CalculateStatusOfAllChargingTariffGroups(POIGroupStatusCalculator<ChargingTariffGroup, ChargingTariff_Id, ChargingTariffStatusType, ChargingTariffGroupStatusType>? Calculator)
            => Calculate(chargingTariffGroups, null, kind => kind.CalculateAll(Calculator));

        /// <summary>
        /// Calculate the status of every parking space group with the given delegate, unless the group has a delegate
        /// of its own or is set manually; null sets them all manually again.
        /// </summary>
        public void CalculateStatusOfAllParkingSpaceGroups(POIGroupStatusCalculator<ParkingSpaceGroup, ParkingSpace_Id, ParkingSpaceStatusType, ParkingSpaceGroupStatusType>? Calculator)
            => Calculate(parkingSpaceGroups, null, kind => kind.CalculateAll(Calculator));

        /// <summary>
        /// Calculate the status of every energy meter group with the given delegate, unless the group has a delegate
        /// of its own or is set manually; null sets them all manually again.
        /// </summary>
        public void CalculateStatusOfAllEnergyMeterGroups(POIGroupStatusCalculator<EnergyMeterGroup, EnergyMeter_Id, EnergyMeterStatusType, EnergyMeterGroupStatusType>? Calculator)
            => Calculate(energyMeterGroups, null, kind => kind.CalculateAll(Calculator));

        #endregion

        #region IsCalculated(GroupType, GroupId)

        /// <summary>
        /// Whether a delegate calculates the status of the given group of the head version.
        /// </summary>
        /// <param name="GroupType">The type of the group, e.g. EVSEGroup.</param>
        /// <param name="GroupId">The identification of the group.</param>
        public Boolean IsCalculated(String GroupType,
                                    String GroupId)

            => groupKinds.FirstOrDefault(kind => kind.GroupType == GroupType) is IGroupKind kind &&
               kind.IsCalculated(history.Head.RoamingNetwork, GroupId);

        #endregion


        #region (private) Calculate<TKind>(Kind, GroupId, Register)

        /// <summary>
        /// Register or change a delegate, and calculate the status of the groups it concerns now.
        /// </summary>
        private void Calculate<TKind>(TKind Kind, Object? GroupId, Action<TKind> Register)

            where TKind : IGroupKind

        {

            Register(Kind);

            var network = history.Head.RoamingNetwork;

            foreach (var groupId in Kind.Calculated(network))
                if (GroupId is null || groupId == GroupId.ToString())
                    Pending(Kind.GroupType, groupId, DateTimeOffset.MinValue);

            CalculateNow();

        }

        #endregion

        #region (private) IsRefusedAsCalculated(Kind, EntityType, EntityId, out ErrorResponse)

        /// <summary>
        /// A status of a group whose status a delegate calculates is refused, unless it is the calculated one.
        /// </summary>
        private Boolean IsRefusedAsCalculated(POIRuntimeStatusKind              Kind,
                                              String                            EntityType,
                                              String                            EntityId,
                                              [NotNullWhen(true)] out String?   ErrorResponse)
        {

            if (Kind == POIRuntimeStatusKind.Status &&
               !calculating &&
                groupKinds.FirstOrDefault(kind => kind.GroupType == EntityType) is IGroupKind kind &&
                kind.IsCalculated(history.Head.RoamingNetwork, EntityId))
            {
                ErrorResponse = $"The status of the {EntityType} '{EntityId}' is calculated by a delegate, which has the say here!";
                return true;
            }

            ErrorResponse = null;
            return false;

        }

        #endregion

        #region (private) MemberStatusChanged(EntityType, EntityId, Since)

        /// <summary>
        /// The status of an entity changed: calculate the status of the groups it is a member of again,
        /// once the status schedule of the entity is no longer locked.
        /// </summary>
        private void MemberStatusChanged(String          EntityType,
                                         String          EntityId,
                                         DateTimeOffset  Since)
        {

            var network  = history.Head.RoamingNetwork;
            var any      = false;

            foreach (var kind in groupKinds)
                if (kind.MemberType == EntityType)
                    foreach (var groupId in kind.ContainingMember(network, EntityId))
                    {
                        Pending(kind.GroupType, groupId, Since);
                        any = true;
                    }

            // Set here, it is calculated once the status update is done; set at the entity, it is calculated
            // on another thread, as the status schedule of the entity is locked still.
            if (any && updateDepth == 0)
                ThreadPool.QueueUserWorkItem(_ => CalculateNow());

        }

        #endregion

        #region (private) HeadChanged(RoamingNetwork)

        /// <summary>
        /// A new head version may have changed the members of groups: calculate them all again, since its last change.
        /// </summary>
        private void HeadChanged(RoamingNetwork RoamingNetwork)
        {

            foreach (var kind in groupKinds)
                foreach (var groupId in kind.Calculated(RoamingNetwork))
                    Pending(kind.GroupType, groupId, RoamingNetwork.LastChangeDate);

            CalculateNow();

        }

        #endregion

        #region (private) Pending(GroupType, GroupId, Since)

        private void Pending(String GroupType, String GroupId, DateTimeOffset Since)
        {
            lock (pendingLock)
            {
                pendingCalculations[(GroupType, GroupId)] = pendingCalculations.TryGetValue((GroupType, GroupId), out var since) && since > Since
                                                                ? since
                                                                : Since;
            }
        }

        #endregion

        #region (private) CalculateNow()

        /// <summary>
        /// Calculate the status of every pending group, and set it, when it changed.
        /// </summary>
        private void CalculateNow()
        {

            if (calculating || updateDepth > 0)
                return;

            lock (calculationLock)
            {

                calculating = true;

                try
                {

                    while (true)
                    {

                        (String GroupType, String GroupId) key;
                        DateTimeOffset                    since;

                        lock (pendingLock)
                        {

                            if (pendingCalculations.Count == 0)
                                return;

                            (key, since) = pendingCalculations.First();
                            pendingCalculations.Remove(key);

                        }

                        try
                        {

                            if (groupKinds.FirstOrDefault(kind => kind.GroupType == key.GroupType) is not IGroupKind kind ||
                                kind.Calculate(history.Head.RoamingNetwork, key.GroupId) is not { } calculated)
                            {
                                continue;
                            }

                            Update(POIRuntimeStatusKind.Status,
                                   key.GroupType,
                                   key.GroupId,
                                   calculated.Value,
                                   calculated.Since > since ? calculated.Since : since,
                                   Authoritative: false,
                                   out _,
                                   out _,
                                   DataSource: "calculated");

                        }
                        catch
                        {
                            // A failing delegate must neither fail the update that caused it nor keep the others.
                        }

                    }

                }
                finally
                {
                    calculating = false;
                }

            }

        }

        #endregion

    }

}
