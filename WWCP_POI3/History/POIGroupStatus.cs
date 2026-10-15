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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The default calculations of the status of groups from the status of their members,
    /// as delegates for RoamingNetworkRuntimeStatus.CalculateStatus and its CalculateStatusOfAll… methods.
    /// </summary>
    /// <remarks>
    /// The members of EVSE, charging station and charging pool groups are free (available, waitingForPlugin,
    /// and partialAvailable of stations and pools), busy (charging, pluggedIn, occupied), reserved, down
    /// (offline, error, unavailable, inoperative, outOfService, outOfOrder, blocked, doorNotClosed) or not yet there
    /// (planned, inDeployment); removed, unknown, unspecified and values not known yet are left out. The first rule
    /// that holds decides: no member left: unknown; all free: available; one free: partialAvailable; one busy:
    /// charging; one reserved: reserved; all offline: offline; all outOfService: outOfService; all down: error;
    /// all planned: planned; all not yet there: inDeployment; else unknown.
    /// </remarks>
    public static class POIGroupStatus
    {

        #region (private) Charging(MemberStatus)

        private enum MemberClass { Free, Busy, Reserved, Down, NotYet, Ignored }

        private static MemberClass ClassOf(String Status)

            => Status.ToLowerInvariant() switch {
                   "available" or "waitingforplugin" or "partialavailable"                                         => MemberClass.Free,
                   "charging"  or "pluggedin"        or "occupied"                                                 => MemberClass.Busy,
                   "reserved"                                                                                      => MemberClass.Reserved,
                   "offline"   or "error"            or "unavailable" or "inoperative" or "outofservice" or
                   "outoforder" or "blocked"         or "doornotclosed"                                            => MemberClass.Down,
                   "planned"   or "indeployment"                                                                   => MemberClass.NotYet,
                   _                                                                                               => MemberClass.Ignored
               };

        /// <summary>
        /// The status of a group of EVSEs, charging stations or charging pools, from the status of its members.
        /// </summary>
        private static String Charging(IEnumerable<String> MemberStatus)
        {

            var members  = MemberStatus.
                               Select(status => (Status: status.ToLowerInvariant(), Class: ClassOf(status))).
                               Where (member => member.Class != MemberClass.Ignored).
                               ToArray();

            Boolean All(Func<(String Status, MemberClass Class), Boolean> Predicate) => members.All(Predicate);
            Boolean Any(MemberClass Class)                                           => members.Any(member => member.Class == Class);

            if (members.Length == 0)                                       return "unknown";
            if (All(member => member.Class  == MemberClass.Free))          return "available";
            if (Any(MemberClass.Free))                                     return "partialAvailable";
            if (Any(MemberClass.Busy))                                     return "charging";
            if (Any(MemberClass.Reserved))                                 return "reserved";
            if (All(member => member.Status == "offline"))                 return "offline";
            if (All(member => member.Status == "outofservice"))            return "outOfService";
            if (All(member => member.Class  == MemberClass.Down))          return "error";
            if (All(member => member.Status == "planned"))                 return "planned";
            if (All(member => member.Class  == MemberClass.NotYet))        return "inDeployment";

            return "unknown";

        }

        #endregion


        #region OfEVSEs(Group, Members)

        /// <summary>
        /// The status of an EVSE group from the status of its EVSEs.
        /// </summary>
        public static EVSEGroupStatusType OfEVSEs(EVSEGroup                                               Group,
                                                  IReadOnlyDictionary<EVSE_Id, Timestamped<EVSEStatusType>>  Members)

            => EVSEGroupStatusType.Parse(Charging(Members.Values.Select(status => status.Value.ToString())));

        #endregion

        #region OfChargingStations(Group, Members)

        /// <summary>
        /// The status of a charging station group from the status of its charging stations.
        /// </summary>
        public static ChargingStationGroupStatusType OfChargingStations(ChargingStationGroup                                                          Group,
                                                                        IReadOnlyDictionary<ChargingStation_Id, Timestamped<ChargingStationStatusType>>  Members)

            => ChargingStationGroupStatusType.Parse(Charging(Members.Values.Select(status => status.Value.ToString())));

        #endregion

        #region OfChargingPools(Group, Members)

        /// <summary>
        /// The status of a charging pool group from the status of its charging pools.
        /// </summary>
        public static ChargingPoolGroupStatusType OfChargingPools(ChargingPoolGroup                                                       Group,
                                                                  IReadOnlyDictionary<ChargingPool_Id, Timestamped<ChargingPoolStatusType>>  Members)

            => ChargingPoolGroupStatusType.Parse(Charging(Members.Values.Select(status => status.Value.ToString())));

        #endregion

        #region OfChargingTariffs(Group, Members)

        /// <summary>
        /// The status of a charging tariff group from the status of its charging tariffs:
        /// active when one of them is, else planned when one of them is, else expired when all are, else unknown.
        /// </summary>
        public static ChargingTariffGroupStatusType OfChargingTariffs(ChargingTariffGroup                                                         Group,
                                                                      IReadOnlyDictionary<ChargingTariff_Id, Timestamped<ChargingTariffStatusType>>  Members)
        {

            var members = Members.Values.Select(status => status.Value.ToString()!.ToLowerInvariant()).ToArray();

            return ChargingTariffGroupStatusType.Parse(
                       members.Contains("active")                                ? "active"
                     : members.Contains("planned")                               ? "planned"
                     : members.Length > 0 && members.All(m => m == "expired")    ? "expired"
                     : "unknown"
                   );

        }

        #endregion

        #region OfParkingSpaces(Group, Members)

        /// <summary>
        /// The status of a parking space group from the status of its parking spaces: available when one of them is,
        /// else inUse when one of them is, else reserved when one of them is, else blocked, planned or inDeployment
        /// when all of them are (planned or inDeployment for the last), else unknown.
        /// </summary>
        public static ParkingSpaceGroupStatusType OfParkingSpaces(ParkingSpaceGroup                                                       Group,
                                                                  IReadOnlyDictionary<ParkingSpace_Id, Timestamped<ParkingSpaceStatusType>>  Members)
        {

            var members = Members.Values.
                              Select(status => status.Value.ToString()!.ToLowerInvariant()).
                              Where (status => status is not ("unknown" or "unspecified")).
                              ToArray();

            return ParkingSpaceGroupStatusType.Parse(
                       members.Length == 0                                                    ? "unknown"
                     : members.Contains("available")                                          ? "available"
                     : members.Contains("inuse")                                              ? "inUse"
                     : members.Contains("reserved")                                           ? "reserved"
                     : members.All(m => m == "blocked")                                       ? "blocked"
                     : members.All(m => m == "planned")                                       ? "planned"
                     : members.All(m => m is "planned" or "indeployment")                     ? "inDeployment"
                     : "unknown"
                   );

        }

        #endregion

    }

}
