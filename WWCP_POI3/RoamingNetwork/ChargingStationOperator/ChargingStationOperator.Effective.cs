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

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The effective charging pools, charging stations and EVSEs of a charging station operator: as they are at
    /// runtime, with the properties their groups give them.
    /// </summary>
    /// <remarks>
    /// The network keeps the properties where they are given, so a charging pool, charging station or EVSE may have
    /// only few of its own and take the others from its groups: charging pool groups, charging station groups and
    /// EVSE groups. Of every property they give:
    /// - a single value, e.g. the address, is its own, else the one of its group with the highest priority giving it,
    /// - an array, e.g. the features, is its own united with those of all its groups; of objects with the same
    ///   identification, e.g. brands or charging connectors, or the same URL, e.g. images, its own wins, else the one
    ///   of the group with the higher priority.
    /// The charging tariffs of an EVSE are its own; without any, the one of its EVSE groups, else of the charging station
    /// groups of its charging station, else of the charging pool groups of its charging pool, each the group's with the
    /// highest priority giving one: the most specific groups decide.
    /// The priority of a group is 0, unless given. Groups of a member with the same priority must not give it
    /// different values where these decide what it takes; neither must they make it invalid, e.g. a charging station
    /// by a maximum power above the one of all its EVSEs together: the operator is refused, and so a change set making
    /// it so. An effective charging station has the effective EVSEs, an effective charging pool the effective charging
    /// stations. The energy meters they embed take the properties of their energy meter groups the same way, as effective
    /// energy meters. Each is a version of its own, with the runtime status of its member, and its energy meters; its last
    /// change is the latest of the member, its groups giving it properties and what it embeds, and, where it is other
    /// than in the version before, also when a group stopped giving it something, not before the last change of the
    /// operator.
    /// </remarks>
    public sealed partial class ChargingStationOperator
    {

        #region Data

        /// <summary>
        /// The effective EVSEs differing from the EVSEs, by their identifications.
        /// </summary>
        private ImmutableDictionary<EVSE_Id,            EVSE>             effectiveEVSEs
            = ImmutableDictionary<EVSE_Id, EVSE>.Empty;

        /// <summary>
        /// The effective charging stations differing from the charging stations, by their identifications.
        /// </summary>
        private ImmutableDictionary<ChargingStation_Id, ChargingStation>  effectiveChargingStations
            = ImmutableDictionary<ChargingStation_Id, ChargingStation>.Empty;

        /// <summary>
        /// The effective charging pools differing from the charging pools, by their identifications.
        /// </summary>
        private ImmutableDictionary<ChargingPool_Id,    ChargingPool>     effectiveChargingPoolIndex
            = ImmutableDictionary<ChargingPool_Id, ChargingPool>.Empty;

        /// <summary>
        /// The effective charging pools, calculated when asked for.
        /// </summary>
        private Lazy<POICollection<ChargingPool_Id, ChargingPool>>        effectiveChargingPools;

        /// <summary>
        /// The last changes effective members have at least, as StampChangedEffective(…) found them: kept by the next version
        /// while they are the same.
        /// </summary>
        private ChangedMembers                                              effectiveFloors   = new ([], [], []);

        #endregion

        #region Properties

        /// <summary>
        /// The charging pools as they are at runtime, with the properties their groups give them, and with their
        /// effective charging stations, in the order of their identifications: a charging pool without any is itself.
        /// </summary>
        public POICollection<ChargingPool_Id, ChargingPool> EffectiveChargingPools
            => effectiveChargingPools.Value;

        /// <summary>
        /// The charging stations of all its charging pools as they are at runtime, with the properties their groups give
        /// them, and with their effective EVSEs.
        /// </summary>
        public IEnumerable<ChargingStation> EffectiveChargingStations
            => ChargingStations.Select(EffectiveChargingStation);

        /// <summary>
        /// The EVSEs of all its charging pools as they are at runtime, with the properties their groups give them.
        /// </summary>
        public IEnumerable<EVSE> EffectiveEVSEs
            => EVSEs.Select(EffectiveEVSE);

        #endregion


        #region EffectiveChargingPool          (ChargingPool)

        /// <summary>
        /// The given charging pool of this operator as it is at runtime, with the properties its groups give it, and with
        /// its effective charging stations: the charging pool itself, when there is nothing to give.
        /// </summary>
        /// <param name="ChargingPool">A charging pool of this operator.</param>
        public ChargingPool EffectiveChargingPool(ChargingPool ChargingPool)

            => effectiveChargingPoolIndex.TryGetValue(ChargingPool.Id, out var effectiveChargingPool)
                   ? effectiveChargingPool
                   : ChargingPool;

        #endregion

        #region EffectiveChargingStation       (ChargingStation)

        /// <summary>
        /// The given charging station of this operator as it is at runtime, with the properties its groups give it, and
        /// with its effective EVSEs: the charging station itself, when there is nothing to give.
        /// </summary>
        /// <param name="ChargingStation">A charging station of this operator.</param>
        public ChargingStation EffectiveChargingStation(ChargingStation ChargingStation)

            => effectiveChargingStations.TryGetValue(ChargingStation.Id, out var effectiveChargingStation)
                   ? effectiveChargingStation
                   : ChargingStation;

        #endregion

        #region EffectiveEVSE                  (EVSE)

        /// <summary>
        /// The given EVSE of this operator as it is at runtime, with the properties its groups give it: the EVSE itself,
        /// when they give it none.
        /// </summary>
        /// <param name="EVSE">An EVSE of this operator.</param>
        public EVSE EffectiveEVSE(EVSE EVSE)

            => effectiveEVSEs.TryGetValue(EVSE.Id, out var effectiveEVSE)
                   ? effectiveEVSE
                   : EVSE;

        #endregion

        #region TryGetEffectiveChargingPool    (ChargingPoolId,    out ChargingPool)

        /// <summary>
        /// Try to return the charging pool with the given identification as it is at runtime.
        /// </summary>
        public Boolean TryGetEffectiveChargingPool(ChargingPool_Id                        ChargingPoolId,
                                                   [NotNullWhen(true)] out ChargingPool?  ChargingPool)
        {

            if (!TryGetChargingPool(ChargingPoolId, out ChargingPool))
                return false;

            ChargingPool = EffectiveChargingPool(ChargingPool);
            return true;

        }

        #endregion

        #region TryGetEffectiveChargingStation (ChargingStationId, out ChargingStation)

        /// <summary>
        /// Try to return the charging station with the given identification as it is at runtime.
        /// </summary>
        public Boolean TryGetEffectiveChargingStation(ChargingStation_Id                        ChargingStationId,
                                                      [NotNullWhen(true)] out ChargingStation?  ChargingStation)
        {

            if (!TryGetChargingStation(ChargingStationId, out ChargingStation))
                return false;

            ChargingStation = EffectiveChargingStation(ChargingStation);
            return true;

        }

        #endregion

        #region TryGetEffectiveEVSE            (EVSEId,            out EVSE)

        /// <summary>
        /// Try to return the EVSE with the given identification as it is at runtime.
        /// </summary>
        public Boolean TryGetEffectiveEVSE(EVSE_Id                        EVSEId,
                                           [NotNullWhen(true)] out EVSE?  EVSE)
        {

            if (!TryGetEVSE(EVSEId, out EVSE))
                return false;

            EVSE = EffectiveEVSE(EVSE);
            return true;

        }

        #endregion

        #region ChargingPoolGroupsOf / ChargingStationGroupsOf / EVSEGroupsOf

        /// <summary>
        /// The groups the given charging pool is a member of, in the order of their identifications.
        /// </summary>
        public IEnumerable<ChargingPoolGroup> ChargingPoolGroupsOf(ChargingPool_Id ChargingPoolId)

            => ChargingPoolGroups.Where(chargingPoolGroup => chargingPoolGroup.ChargingPoolIds.Contains(ChargingPoolId));

        /// <summary>
        /// The groups the given charging station is a member of, in the order of their identifications.
        /// </summary>
        public IEnumerable<ChargingStationGroup> ChargingStationGroupsOf(ChargingStation_Id ChargingStationId)

            => ChargingStationGroups.Where(chargingStationGroup => chargingStationGroup.ChargingStationIds.Contains(ChargingStationId));

        /// <summary>
        /// The groups the given EVSE is a member of, in the order of their identifications.
        /// </summary>
        public IEnumerable<EVSEGroup> EVSEGroupsOf(EVSE_Id EVSEId)

            => EVSEGroups.Where(evseGroup => evseGroup.EVSEIds.Contains(EVSEId));

        /// <summary>
        /// The groups the given energy meter is a member of, in the order of their identifications.
        /// </summary>
        public IEnumerable<EnergyMeterGroup> EnergyMeterGroupsOf(EnergyMeter_Id EnergyMeterId)

            => EnergyMeterGroups.Where(energyMeterGroup => energyMeterGroup.EnergyMeterIds.Contains(EnergyMeterId));

        #endregion


        #region (private) CalculateEffective(Changed = null)

        /// <summary>
        /// The last change members have at least: the one of the operator, where they are other than in the version before;
        /// the one they had in it, where they are the same.
        /// </summary>
        private sealed record ChangedMembers(Dictionary<EVSE_Id,            DateTimeOffset>  EVSEs,
                                             Dictionary<ChargingStation_Id, DateTimeOffset>  ChargingStations,
                                             Dictionary<ChargingPool_Id,    DateTimeOffset>  ChargingPools);

        /// <summary>
        /// The effective EVSEs, charging stations and charging pools differing from the given ones, by their identifications;
        /// the given changed members as well, with at least their last change.
        /// </summary>
        private (ImmutableDictionary<EVSE_Id,            EVSE>             EVSEs,
                 ImmutableDictionary<ChargingStation_Id, ChargingStation>  ChargingStations,
                 ImmutableDictionary<ChargingPool_Id,    ChargingPool>     ChargingPools)  CalculateEffective(ChangedMembers? Changed = null)
        {

            // The JSON of a group, calculated once for all its members.
            var groupsJSON  = new Dictionary<IPOIPrioritizedGroup, JObject>(ReferenceEqualityComparer.Instance);

            JObject JSONOf(IPOIPrioritizedGroup Group)
            {
                if (!groupsJSON.TryGetValue(Group, out var json))
                {
                    json = Group.ToJSON();
                    groupsJSON.Add(Group, json);
                }
                return json;
            }

            #region Energy meters, with the properties of their groups

            var energyMeterGroupsOf = MembersOf(EnergyMeterGroups.Where(energyMeterGroup => energyMeterGroup.HasEnergyMeterProperties), energyMeterGroup => energyMeterGroup.EnergyMeterIds);

            Boolean HasGroups(EnergyMeter? EnergyMeter)

                => EnergyMeter is not null &&
                   energyMeterGroupsOf.ContainsKey(EnergyMeter.Id);

            EnergyMeter EffectiveMeter(EnergyMeter EnergyMeter)

                => energyMeterGroupsOf.TryGetValue(EnergyMeter.Id, out var groups)
                       ? EnergyMeterGroup.Effective(EnergyMeter, new POIGroupValues<EnergyMeterGroup>($"the energy meter '{EnergyMeter.Id}'", groups, JSONOf))
                       : EnergyMeter;

            #endregion

            #region EVSEs, with the charging tariff of their groups, unless they have their own

            var evseGroupsOf                   = MembersOf(EVSEGroups.           Where(evseGroup            => evseGroup.HasEVSEProperties),                    evseGroup            => evseGroup.EVSEIds);
            var evseTariffGroupsOf             = MembersOf(EVSEGroups.           Where(evseGroup            => evseGroup.ChargingTariffId.HasValue),            evseGroup            => evseGroup.EVSEIds);
            var chargingStationTariffGroupsOf  = MembersOf(ChargingStationGroups.Where(chargingStationGroup => chargingStationGroup.ChargingTariffId.HasValue), chargingStationGroup => chargingStationGroup.ChargingStationIds);
            var chargingPoolTariffGroupsOf     = MembersOf(ChargingPoolGroups.   Where(chargingPoolGroup    => chargingPoolGroup.ChargingTariffId.HasValue),    chargingPoolGroup    => chargingPoolGroup.ChargingPoolIds);
            var evses                          = ImmutableDictionary.CreateBuilder<EVSE_Id, EVSE>();

            foreach (var chargingPool in ChargingPools)
            {
                foreach (var chargingStation in chargingPool.ChargingStations)
                {
                    foreach (var evse in chargingStation.EVSEs)
                    {

                        var member     = $"the EVSE '{evse.Id}'";
                        var hasGroups  = evseGroupsOf.TryGetValue(evse.Id, out var groups);

                        // Its own tariffs, else the one of its most specific groups giving one.
                        var tariff     = evse.ChargingTariffIds.Length > 0
                                             ? null
                                             : TariffOf(member, evseTariffGroupsOf.           GetValueOrDefault(evse.Id),            evseGroup            => evseGroup.           ChargingTariffId, JSONOf) ??
                                               TariffOf(member, chargingStationTariffGroupsOf.GetValueOrDefault(chargingStation.Id), chargingStationGroup => chargingStationGroup.ChargingTariffId, JSONOf) ??
                                               TariffOf(member, chargingPoolTariffGroupsOf.   GetValueOrDefault(chargingPool.Id),    chargingPoolGroup    => chargingPoolGroup.   ChargingTariffId, JSONOf);

                        DateTimeOffset? atLeast = Changed?.EVSEs.TryGetValue(evse.Id, out var floor) == true ? floor : null;

                        if (!hasGroups && tariff is null && !atLeast.HasValue && !HasGroups(evse.EnergyMeter))
                            continue;

                        evses.Add(evse.Id, Effective(evse,
                                                     new POIGroupValues<EVSEGroup>(member, groups ?? [], JSONOf),
                                                     tariff,
                                                     evse.EnergyMeter is EnergyMeter energyMeter ? EffectiveMeter(energyMeter) : null,
                                                     atLeast));

                    }
                }
            }

            #endregion

            #region Charging stations, with their effective EVSEs

            var chargingStationGroupsOf  = MembersOf(ChargingStationGroups.Where(chargingStationGroup => chargingStationGroup.HasChargingStationProperties), chargingStationGroup => chargingStationGroup.ChargingStationIds);
            var chargingStations         = ImmutableDictionary.CreateBuilder<ChargingStation_Id, ChargingStation>();

            foreach (var chargingStation in ChargingStations)
            {

                var hasGroups = chargingStationGroupsOf.TryGetValue(chargingStation.Id, out var groups);

                DateTimeOffset? atLeast = Changed?.ChargingStations.TryGetValue(chargingStation.Id, out var floor) == true ? floor : null;

                if (!hasGroups && !atLeast.HasValue && !chargingStation.EVSEs.Any(evse => evses.ContainsKey(evse.Id)) && !chargingStation.EnergyMeters.Any(HasGroups))
                    continue;

                chargingStations.Add(chargingStation.Id,
                                     Effective(chargingStation,
                                               new POIGroupValues<ChargingStationGroup>($"the charging station '{chargingStation.Id}'", groups ?? [], JSONOf),
                                               [.. chargingStation.EVSEs.Select(evse => evses.TryGetValue(evse.Id, out var effectiveEVSE) ? effectiveEVSE : evse)],
                                               [.. chargingStation.EnergyMeters.Select(EffectiveMeter)],
                                               atLeast));

            }

            #endregion

            #region Charging pools, with their effective charging stations

            var chargingPoolGroupsOf  = MembersOf(ChargingPoolGroups.Where(chargingPoolGroup => chargingPoolGroup.HasChargingPoolProperties), chargingPoolGroup => chargingPoolGroup.ChargingPoolIds);
            var chargingPools         = ImmutableDictionary.CreateBuilder<ChargingPool_Id, ChargingPool>();

            foreach (var chargingPool in ChargingPools)
            {

                var hasGroups = chargingPoolGroupsOf.TryGetValue(chargingPool.Id, out var groups);

                DateTimeOffset? atLeast = Changed?.ChargingPools.TryGetValue(chargingPool.Id, out var floor) == true ? floor : null;

                if (!hasGroups && !atLeast.HasValue && !chargingPool.ChargingStations.Any(chargingStation => chargingStations.ContainsKey(chargingStation.Id)) && !chargingPool.EnergyMeters.Any(HasGroups))
                    continue;

                chargingPools.Add(chargingPool.Id,
                                  Effective(chargingPool,
                                            new POIGroupValues<ChargingPoolGroup>($"the charging pool '{chargingPool.Id}'", groups ?? [], JSONOf),
                                            [.. chargingPool.ChargingStations.Select(chargingStation => chargingStations.TryGetValue(chargingStation.Id, out var effectiveChargingStation) ? effectiveChargingStation : chargingStation)],
                                            [.. chargingPool.EnergyMeters.Select(EffectiveMeter)],
                                            atLeast));

            }

            #endregion

            return (evses.ToImmutable(), chargingStations.ToImmutable(), chargingPools.ToImmutable());

        }

        #endregion

        #region (private) CalculateEffectiveChargingPools()

        /// <summary>
        /// The charging pools, the effective ones instead of those differing.
        /// </summary>
        private POICollection<ChargingPool_Id, ChargingPool> CalculateEffectiveChargingPools()

            => effectiveChargingPoolIndex.IsEmpty
                   ? ChargingPools
                   : new (ChargingPools.Select(EffectiveChargingPool), chargingPool => chargingPool.Id, "charging pool", $"the operator '{Id}'");

        #endregion

        #region (private) StampChangedEffective(Previous)

        /// <summary>
        /// Calculate the effective members again where they are other than in the given previous version of this operator,
        /// with at least the last change of this operator: so a group that stops giving a member something, or a member
        /// that leaves a group, changes its last change as well, not only its content; and where they are the same, with at
        /// least the last change they had in it, e.g. after an earlier change of the same change set: it never goes back.
        /// Floors found before are kept, the later one of two: a merge compares with both of its parents, one after the
        /// other.
        /// </summary>
        /// <param name="Previous">The previous version of this operator.</param>
        /// <param name="AlsoEVSEs">EVSEs to compare as well, e.g. those a change set moved to another charging station.</param>
        /// <param name="AlsoChargingStations">Charging stations to compare as well, e.g. those a change set moved to another charging pool.</param>
        private void StampChangedEffective(ChargingStationOperator           Previous,
                                           IEnumerable<EVSE_Id>?             AlsoEVSEs              = null,
                                           IEnumerable<ChargingStation_Id>?  AlsoChargingStations   = null)
        {

            static Boolean Same(JObject Current, JObject Before, String? Embedded)
            {
                foreach (var json in new[] { Current, Before })
                {
                    json.Remove("lastChange");
                    if (Embedded is not null)
                        json.Remove(Embedded);
                }
                return POIJSON.SameValue(Current, Before);
            }

            // Other than before: at least the last change of the operator; the same: at least the one it had.
            DateTimeOffset? FloorOf(AEntity Current, AEntity Before, String? Embedded)

                => !Same(Current.ToJSONOf(), Before.ToJSONOf(), Embedded)  ? LastChangeDate
                   : Current.LastChangeDate < Before.LastChangeDate        ? Before.LastChangeDate
                   : null;

            #region Only where something may be other: the members of groups that changed, and those having a floor before

            // A member whose own representation changed has the last change of the change set anyway: only its groups
            // can change it without a change of its own, or the floor it had keep it.
            var evseCandidates             = new HashSet<EVSE_Id>           (Previous.effectiveFloors.EVSEs.Keys);
            var chargingStationCandidates  = new HashSet<ChargingStation_Id>(Previous.effectiveFloors.ChargingStations.Keys);
            var chargingPoolCandidates     = new HashSet<ChargingPool_Id>   (Previous.effectiveFloors.ChargingPools.Keys);

            evseCandidates.           UnionWith(AlsoEVSEs            ?? []);
            chargingStationCandidates.UnionWith(AlsoChargingStations ?? []);

            evseCandidates.           UnionWith(MembersOfChangedGroups(EVSEGroups,            Previous.EVSEGroups,            group => group.Id, group => group.EVSEIds));
            chargingStationCandidates.UnionWith(MembersOfChangedGroups(ChargingStationGroups, Previous.ChargingStationGroups, group => group.Id, group => group.ChargingStationIds));
            chargingPoolCandidates.   UnionWith(MembersOfChangedGroups(ChargingPoolGroups,    Previous.ChargingPoolGroups,    group => group.Id, group => group.ChargingPoolIds));

            // The tariff of a charging station or charging pool group is the one of their EVSEs.
            foreach (var chargingPoolId in MembersOfChangedGroups(ChargingPoolGroups, Previous.ChargingPoolGroups, group => group.Id, group => group.ChargingPoolIds, group => group.ChargingTariffId.HasValue))
                if (TryGetChargingPool(chargingPoolId, out var chargingPool))
                    evseCandidates.UnionWith(chargingPool.EVSEs.Select(evse => evse.Id));

            foreach (var chargingStationId in MembersOfChangedGroups(ChargingStationGroups, Previous.ChargingStationGroups, group => group.Id, group => group.ChargingStationIds, group => group.ChargingTariffId.HasValue))
                if (TryGetChargingStation(chargingStationId, out var chargingStation))
                    evseCandidates.UnionWith(chargingStation.EVSEs.Select(evse => evse.Id));

            // The members of energy meter groups are embedded in what they measure.
            var energyMeterCandidates = MembersOfChangedGroups(EnergyMeterGroups, Previous.EnergyMeterGroups, group => group.Id, group => group.EnergyMeterIds).ToHashSet();

            if (energyMeterCandidates.Count > 0)
            {
                foreach (var chargingPool in ChargingPools)
                {

                    if (chargingPool.EnergyMeters.Any(energyMeter => energyMeterCandidates.Contains(energyMeter.Id)))
                        chargingPoolCandidates.Add(chargingPool.Id);

                    foreach (var chargingStation in chargingPool.ChargingStations)
                    {

                        if (chargingStation.EnergyMeters.Any(energyMeter => energyMeterCandidates.Contains(energyMeter.Id)))
                            chargingStationCandidates.Add(chargingStation.Id);

                        foreach (var evse in chargingStation.EVSEs)
                            if (evse.EnergyMeter is not null && energyMeterCandidates.Contains(evse.EnergyMeter.Id))
                                evseCandidates.Add(evse.Id);

                    }

                }
            }

            #endregion

            // The floors found before, e.g. comparing a merge with its other parent, are kept.
            var evses             = new Dictionary<EVSE_Id,            DateTimeOffset>(effectiveFloors.EVSEs);
            var chargingStations  = new Dictionary<ChargingStation_Id, DateTimeOffset>(effectiveFloors.ChargingStations);
            var chargingPools     = new Dictionary<ChargingPool_Id,    DateTimeOffset>(effectiveFloors.ChargingPools);
            var found             = false;

            foreach (var evseId in evseCandidates)
                if (TryGetEVSE(evseId, out var evse) && Previous.TryGetEVSE(evseId, out var previousEVSE) &&
                    FloorOf(new (EffectiveEVSE(evse)), new (Previous.EffectiveEVSE(previousEVSE)), null) is DateTimeOffset floor)
                {
                    found = Raise(evses, evse.Id, floor) | found;
                }

            foreach (var chargingStationId in chargingStationCandidates)
                if (TryGetChargingStation(chargingStationId, out var chargingStation) && Previous.TryGetChargingStation(chargingStationId, out var previousChargingStation) &&
                    FloorOf(new (EffectiveChargingStation(chargingStation)), new (Previous.EffectiveChargingStation(previousChargingStation)), "EVSEs") is DateTimeOffset floor)
                {
                    found = Raise(chargingStations, chargingStation.Id, floor) | found;
                }

            foreach (var chargingPoolId in chargingPoolCandidates)
                if (TryGetChargingPool(chargingPoolId, out var chargingPool) && Previous.TryGetChargingPool(chargingPoolId, out var previousChargingPool) &&
                    FloorOf(new (EffectiveChargingPool(chargingPool)), new (Previous.EffectiveChargingPool(previousChargingPool)), "chargingStations") is DateTimeOffset floor)
                {
                    found = Raise(chargingPools, chargingPool.Id, floor) | found;
                }

            if (!found)
                return;

            effectiveFloors = new (evses, chargingStations, chargingPools);

            (effectiveEVSEs, effectiveChargingStations, effectiveChargingPoolIndex) = CalculateEffective(effectiveFloors);
            effectiveChargingPools = new (CalculateEffectiveChargingPools);

        }

        #endregion

        #region (private) ShareRuntimeOfEffective()

        /// <summary>
        /// Let the effective EVSEs, charging stations and charging pools, and the effective energy meters they embed, take
        /// the runtime status of the EVSEs, charging stations, charging pools and energy meters again, after these took the
        /// one of their previous versions.
        /// </summary>
        private void ShareRuntimeOfEffective()
        {

            foreach (var (evseId, effectiveEVSE) in effectiveEVSEs)
                if (TryGetEVSE(evseId, out var evse))
                    effectiveEVSE.ShareRuntimeOf(evse);

            foreach (var (chargingStationId, effectiveChargingStation) in effectiveChargingStations)
                if (TryGetChargingStation(chargingStationId, out var chargingStation))
                    effectiveChargingStation.ShareRuntimeOf(chargingStation);

            foreach (var (chargingPoolId, effectiveChargingPool) in effectiveChargingPoolIndex)
                if (TryGetChargingPool(chargingPoolId, out var chargingPool))
                    effectiveChargingPool.ShareRuntimeOf(chargingPool);

            // An effective charging station embeds the effective EVSEs, an effective charging pool the effective charging
            // stations: of each only its own energy meters.
            foreach (var effectiveEnergyMeter in effectiveEVSEs.             Values.Select    (effectiveEVSE            => effectiveEVSE.           EnergyMeter).
                                                 Concat(effectiveChargingStations. Values.SelectMany(effectiveChargingStation => effectiveChargingStation.EnergyMeters)).
                                                 Concat(effectiveChargingPoolIndex.Values.SelectMany(effectiveChargingPool    => effectiveChargingPool.   EnergyMeters)).
                                                 OfType<EnergyMeter>())
            {
                if (TryGetEnergyMeter(effectiveEnergyMeter.Id, out var energyMeter) && !ReferenceEquals(effectiveEnergyMeter, energyMeter))
                    effectiveEnergyMeter.ShareRuntimeOf(energyMeter);
            }

        }

        #endregion


        #region (internal static) Raise(Floors, Id, Floor)

        /// <summary>
        /// Raise the floor of the given member to the given one, unless it has a later one: whether it did.
        /// </summary>
        internal static Boolean Raise<TId>(Dictionary<TId, DateTimeOffset>  Floors,
                                           TId                              Id,
                                           DateTimeOffset                   Floor)

            where TId : notnull

        {

            if (Floors.TryGetValue(Id, out var had) && had >= Floor)
                return false;

            Floors[Id] = Floor;
            return true;

        }

        #endregion

        #region (private static) MembersOfChangedGroups(Current, Before, IdOf, MembersOf, Relevant = null)

        /// <summary>
        /// The members, in either version, of the groups that are other than before, added or removed: only these can give
        /// their members something else. Of the relevant ones only, if given, e.g. those naming a charging tariff.
        /// </summary>
        private static IEnumerable<TMemberId> MembersOfChangedGroups<TGroupId, TGroup, TMemberId>(POICollection<TGroupId, TGroup>  Current,
                                                                                                  POICollection<TGroupId, TGroup>  Before,
                                                                                                  Func<TGroup, TGroupId>           IdOf,
                                                                                                  Func<TGroup, IEnumerable<TMemberId>>  MembersOf,
                                                                                                  Func<TGroup, Boolean>?           Relevant   = null)

            where TGroupId : notnull
            where TGroup   : class, IPOIPrioritizedGroup

        {

            foreach (var group in Current)
            {

                var before = Before.TryGet(IdOf(group), out var previous) ? previous : null;

                if (before is not null && POIJSON.SameValue(group.ToJSON(), before.ToJSON()))
                    continue;

                if (Relevant is not null && !Relevant(group) && (before is null || !Relevant(before)))
                    continue;

                foreach (var memberId in MembersOf(group))
                    yield return memberId;

                if (before is not null)
                    foreach (var memberId in MembersOf(before))
                        yield return memberId;

            }

            foreach (var before in Before)
                if (!Current.TryGet(IdOf(before), out _) && (Relevant is null || Relevant(before)))
                    foreach (var memberId in MembersOf(before))
                        yield return memberId;

        }

        #endregion

        #region (private static) MembersOf(Groups, MemberIdsOf)

        /// <summary>
        /// The given groups of each member.
        /// </summary>
        private static Dictionary<TMemberId, TGroup[]> MembersOf<TGroup, TMemberId>(IEnumerable<TGroup>                       Groups,
                                                                                    Func<TGroup, IEnumerable<TMemberId>>      MemberIdsOf)

            where TMemberId : notnull

            => Groups.SelectMany(group => MemberIdsOf(group).Select(memberId => (MemberId: memberId, Group: group))).
                      GroupBy   (membership => membership.MemberId).
                      ToDictionary(members => members.Key,
                                   members => members.Select(membership => membership.Group).ToArray());

        #endregion

        #region (private) AEntity

        /// <summary>
        /// An effective member, with its JSON representation and its last change.
        /// </summary>
        private readonly record struct AEntity(Func<JObject> ToJSONOf, DateTimeOffset LastChangeDate)
        {
            public AEntity(EVSE            EVSE)            : this(EVSE.           ToJSON, EVSE.           LastChangeDate) { }
            public AEntity(ChargingStation ChargingStation) : this(ChargingStation.ToJSON, ChargingStation.LastChangeDate) { }
            public AEntity(ChargingPool    ChargingPool)    : this(ChargingPool.   ToJSON, ChargingPool.   LastChangeDate) { }
        }

        #endregion

        #region (private static) Latest(LastChange, Others)

        /// <summary>
        /// The latest of the given timestamps.
        /// </summary>
        private static DateTimeOffset Latest(DateTimeOffset            LastChange,
                                             params DateTimeOffset?[]  Others)

            => Others.Where(other => other.HasValue).Select(other => other!.Value).Append(LastChange).Max();

        #endregion

        #region (private static) LastChangeOfEffective(Effective, Own) / LastChangesOfEffective(Effective, Own)

        /// <summary>
        /// The last change of the given effective energy meter, where its groups give it properties: what a member embeds
        /// as it is at runtime is part of it. Null where it is the energy meter itself.
        /// </summary>
        private static DateTimeOffset? LastChangeOfEffective(EnergyMeter?  Effective,
                                                             EnergyMeter?  Own)

            => Effective is not null && !ReferenceEquals(Effective, Own)
                   ? Effective.LastChangeDate
                   : null;

        /// <summary>
        /// The last changes of the given effective energy meters, where their groups give them properties.
        /// </summary>
        private static IEnumerable<DateTimeOffset?> LastChangesOfEffective(IEnumerable<EnergyMeter>  Effective,
                                                                           IEnumerable<EnergyMeter>  Own)

            => Effective.Select(effective => LastChangeOfEffective(effective, Own.FirstOrDefault(own => own.Id == effective.Id)));

        #endregion

        #region (private static) TariffOf(Member, Groups, TariffOf, JSONOf)

        /// <summary>
        /// The charging tariff of the given groups of a member with the highest priority giving one, with its last change;
        /// none without groups.
        /// </summary>
        private static (ChargingTariff_Id Id, DateTimeOffset LastChange)? TariffOf<TGroup>(String                                Member,
                                                                                           TGroup[]?                             Groups,
                                                                                           Func<TGroup, ChargingTariff_Id?>      TariffOf,
                                                                                           Func<IPOIPrioritizedGroup, JObject>   JSONOf)

            where TGroup : class, IPOIPrioritizedGroup

        {

            if (Groups is null || Groups.Length == 0)
                return null;

            var values  = new POIGroupValues<TGroup>(Member, Groups, JSONOf);
            var tariff  = values.Single("chargingTariffId", null, TariffOf);

            return tariff.HasValue
                       ? (tariff.Value, values.Groups.First(group => TariffOf(group) == tariff).LastChangeDate)
                       : null;

        }

        #endregion

        #region (private static) Effective(EVSE,            Groups, Tariff, EnergyMeter)

        /// <summary>
        /// The given EVSE with the properties of the given groups, the given charging tariff of its groups, if any,
        /// and its given effective energy meter.
        /// </summary>
        private static EVSE Effective(EVSE                                                 EVSE,
                                      POIGroupValues<EVSEGroup>                            Groups,
                                      (ChargingTariff_Id Id, DateTimeOffset LastChange)?   Tariff,
                                      EnergyMeter?                                         EnergyMeter,
                                      DateTimeOffset?                                      AtLeast   = null)
        {

            try
            {

                var effective = new EVSE(

                                    EVSE.Id,
                                    EVSE.Name,
                                    EVSE.Description,

                                    PhysicalReference:         Groups.Single("physicalReference",         EVSE.PhysicalReference,         group => group.PhysicalReference),
                                    CurrentTypes:              Groups.Single("currentTypes",              EVSE.CurrentTypes  != POI.CurrentTypes. Unspecified ? EVSE.CurrentTypes  : null,
                                                                                                          group => group.CurrentTypes  != POI.CurrentTypes. Unspecified ? group.CurrentTypes  : (CurrentTypes?)  null),
                                    ChargingModes:             Groups.Single("chargingModes",             EVSE.ChargingModes != POI.ChargingModes.Unspecified ? EVSE.ChargingModes : null,
                                                                                                          group => group.ChargingModes != POI.ChargingModes.Unspecified ? group.ChargingModes : (ChargingModes?) null),
                                    MaxVoltage:                Groups.Single("maxVoltage",                EVSE.MaxVoltage,                group => group.MaxVoltage),
                                    MaxCurrent:                Groups.Single("maxCurrent",                EVSE.MaxCurrent,                group => group.MaxCurrent),
                                    MaxPower:                  Groups.Single("maxPower",                  EVSE.MaxPower,                  group => group.MaxPower),
                                    GeoLocation:               Groups.Single("geoLocation",               EVSE.GeoLocation,               group => group.GeoLocation),
                                    MaxReservationDuration:    Groups.Single("maxReservationDuration",    EVSE.MaxReservationDuration,    group => group.MaxReservationDuration),
                                    ReachDistance:             Groups.Single("reachDistance",             EVSE.ReachDistance,             group => group.ReachDistance),
                                    OperationTimeout:          Groups.Single("operationTimeout",          EVSE.OperationTimeout,          group => group.OperationTimeout),
                                    ExtendedOperationTimeout:  Groups.Single("extendedOperationTimeout",  EVSE.ExtendedOperationTimeout,  group => group.ExtendedOperationTimeout),

                                    ChargingConnectors:        Groups.Union ("charging connector", EVSE.ChargingConnectors, group => group.ChargingConnectors, chargingConnector => chargingConnector.Id,  chargingConnector => chargingConnector.ToJSON()),
                                    Brands:                    Groups.Union ("brand",              EVSE.Brands,             group => group.Brands,             brand             => brand.Id,              brand             => brand.ToJSON()),
                                    Images:                    Groups.Union ("image",              EVSE.Images,             group => group.Images,             image             => image.URL.ToString(),  image             => image.ToJSON()),
                                    Standards:                 Groups.Union (EVSE.Standards, group => group.Standards),
                                    FloorLevel:                Groups.Single("floorLevel",                EVSE.FloorLevel,                group => group.FloorLevel),
                                    AcceptedEMobilityProviderIds:  Groups.Union (EVSE.AcceptedEMobilityProviderIds, group => group.AcceptedEMobilityProviderIds),

                                    // Its own tariffs, else the one of its groups; its energy meter is a device of its own,
                                    // with the properties of its energy meter groups.
                                    ChargingTariffIds:         Tariff.HasValue ? [ Tariff.Value.Id ] : EVSE.ChargingTariffIds,
                                    EnergyMeter:               EnergyMeter,

                                    DataSource:                EVSE.DataSource,
                                    Created:                   EVSE.Created,
                                    LastChange:                Latest(Groups.LastChange(EVSE.LastChangeDate), Tariff?.LastChange, AtLeast, LastChangeOfEffective(EnergyMeter, EVSE.EnergyMeter)),
                                    CustomData:                EVSE.CustomData

                                );

                effective.ShareRuntimeOf(EVSE);

                return effective;

            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"The EVSE '{EVSE.Id}' {Groups.With} is invalid: {e.Message}", e);
            }

        }

        #endregion

        #region (private static) Effective(ChargingStation, Groups, EVSEs, EnergyMeters)

        /// <summary>
        /// The given charging station with the properties of the given groups, the given effective EVSEs, and its given
        /// effective energy meters.
        /// </summary>
        private static ChargingStation Effective(ChargingStation                             ChargingStation,
                                                 POIGroupValues<ChargingStationGroup>        Groups,
                                                 EVSE[]                                      EVSEs,
                                                 EnergyMeter[]                               EnergyMeters,
                                                 DateTimeOffset?                             AtLeast   = null)
        {

            try
            {

                var effective = new ChargingStation(

                                    ChargingStation.Id,
                                    ChargingStation.Name,
                                    ChargingStation.Description,

                                    PhysicalReference:    Groups.Single("physicalReference",  ChargingStation.PhysicalReference,   group => group.PhysicalReference),
                                    Address:              Groups.Single("address",            ChargingStation.Address,             group => group.Address),
                                    GeoLocation:          Groups.Single("geoLocation",        ChargingStation.GeoLocation,         group => group.GeoLocation),
                                    HotlinePhoneNumber:   Groups.Single("hotline",            ChargingStation.HotlinePhoneNumber,  group => group.HotlinePhoneNumber),
                                    ManufacturerId:       Groups.Single("manufacturerId",     ChargingStation.ManufacturerId,      group => group.ManufacturerId),
                                    ModelId:              Groups.Single("modelId",            ChargingStation.ModelId,             group => group.ModelId),
                                    OpeningTimes:         Groups.Single("openingTimes",       ChargingStation.OpeningTimes,        group => group.OpeningTimes),
                                    Accessibility:        Groups.Single("accessibility",      ChargingStation.Accessibility,       group => group.Accessibility),
                                    CalibrationInfo:      Groups.Single("calibrationInfo",    ChargingStation.CalibrationInfo,     group => group.CalibrationInfo),
                                    CertificationInfo:    Groups.Single("certificationInfo",  ChargingStation.CertificationInfo,   group => group.CertificationInfo),
                                    MaxPower:             Groups.Single("maxPower",           ChargingStation.MaxPower,            group => group.MaxPower),

                                    Brands:               Groups.Union ("brand",  ChargingStation.Brands, group => group.Brands, brand => brand.Id,             brand => brand.ToJSON()),
                                    Images:               Groups.Union ("image",  ChargingStation.Images, group => group.Images, image => image.URL.ToString(), image => image.ToJSON()),

                                    VehicleTypes:         Groups.Union (ChargingStation.VehicleTypes,        group => group.VehicleTypes),
                                    EUVehicleCategories:  Groups.Union (ChargingStation.EUVehicleCategories, group => group.EUVehicleCategories),
                                    Features:             Groups.Union (ChargingStation.Features,            group => group.Features),
                                    ParkingSpaceIds:      Groups.Union (ChargingStation.ParkingSpaceIds,     group => group.ParkingSpaceIds),

                                    // Its effective EVSEs, and its very energy meters, with the properties of their groups.
                                    EVSEs:                EVSEs,
                                    EnergyMeters:         EnergyMeters,

                                    DataSource:           ChargingStation.DataSource,
                                    Created:              ChargingStation.Created,
                                    LastChange:           Latest(EVSEs.Select(evse => evse.LastChangeDate).Append(Groups.LastChange(ChargingStation.LastChangeDate)).Max(),
                                                                 [ AtLeast, .. LastChangesOfEffective(EnergyMeters, ChargingStation.EnergyMeters) ]),
                                    CustomData:           ChargingStation.CustomData

                                );

                effective.ShareRuntimeOf(ChargingStation);

                return effective;

            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"The charging station '{ChargingStation.Id}' {Groups.With} is invalid: {e.Message}", e);
            }

        }

        #endregion

        #region (private static) Effective(ChargingPool,    Groups, ChargingStations, EnergyMeters)

        /// <summary>
        /// The given charging pool with the properties of the given groups, the given effective charging stations, and
        /// its given effective energy meters.
        /// </summary>
        private static ChargingPool Effective(ChargingPool                             ChargingPool,
                                              POIGroupValues<ChargingPoolGroup>        Groups,
                                              ChargingStation[]                        ChargingStations,
                                              EnergyMeter[]                            EnergyMeters,
                                              DateTimeOffset?                          AtLeast   = null)
        {

            static ImmutableI18NString? Given(ImmutableI18NString Text)
                => Text.IsNotNullOrEmpty() ? Text : null;

            try
            {

                var effective = new ChargingPool(

                                    ChargingPool.Id,
                                    ChargingPool.Name,
                                    ChargingPool.Description,

                                    Address:                   Groups.Single("address",                   ChargingPool.Address,                          group => group.Address),
                                    GeoLocation:               Groups.Single("geoLocation",               ChargingPool.GeoLocation,                      group => group.GeoLocation),
                                    ParkingType:               Groups.Single("parkingType",               ChargingPool.ParkingType,                      group => group.ParkingType),
                                    Accessibility:             Groups.Single("accessibility",             ChargingPool.Accessibility,                    group => group.Accessibility),
                                    HotlinePhoneNumber:        Groups.Single("hotline",                   ChargingPool.HotlinePhoneNumber,               group => group.HotlinePhoneNumber),
                                    GridConnectionPointId:     Groups.Single("gridConnectionPointId",     ChargingPool.GridConnectionPointId,            group => group.GridConnectionPointId),
                                    OpeningTimes:              Groups.Single("openingTimes",              ChargingPool.OpeningTimes,                     group => group.OpeningTimes),
                                    EnergyMix:                 Groups.Single("energyMix",                 ChargingPool.EnergyMix,                        group => group.EnergyMix),
                                    TimeZone:                  Groups.Single("timeZone",                  ChargingPool.TimeZone,                         group => group.TimeZone),
                                    EntranceAddress:           Groups.Single("entranceAddress",           ChargingPool.EntranceAddress,                  group => group.EntranceAddress),
                                    EntranceLocation:          Groups.Single("entranceLocation",          ChargingPool.EntranceLocation,                 group => group.EntranceLocation),
                                    ExitAddress:               Groups.Single("exitAddress",               ChargingPool.ExitAddress,                      group => group.ExitAddress),
                                    ExitLocation:              Groups.Single("exitLocation",              ChargingPool.ExitLocation,                     group => group.ExitLocation),
                                    ArrivalInstructions:       Groups.Single("arrivalInstructions",       Given(ChargingPool.ArrivalInstructions),       group => Given(group.ArrivalInstructions)),
                                    ChargingWhenClosed:        Groups.Single("chargingWhenClosed",        ChargingPool.ChargingWhenClosed,               group => group.ChargingWhenClosed),
                                    AssistanceServiceDetails:  Groups.Single("assistanceServiceDetails",  Given(ChargingPool.AssistanceServiceDetails),  group => Given(group.AssistanceServiceDetails)),

                                    DataLicenses:              Groups.Union ("data license",         ChargingPool.DataLicenses,         group => group.DataLicenses,         dataLicense         => dataLicense.Id,                 dataLicense         => dataLicense.ToPOIJSON()),
                                    Brands:                    Groups.Union ("brand",                ChargingPool.Brands,               group => group.Brands,               brand               => brand.Id,                       brand               => brand.ToJSON()),
                                    Images:                    Groups.Union ("image",                ChargingPool.Images,               group => group.Images,               image               => image.URL.ToString(),           image               => image.ToJSON()),
                                    AuthenticationModes:       Groups.Union ("authentication mode",  ChargingPool.AuthenticationModes,  group => group.AuthenticationModes,  authenticationMode  => authenticationMode.Type,        authenticationMode  => authenticationMode.ToJSON()),
                                    RelatedLocations:          Groups.Union ("related location",     ChargingPool.RelatedLocations,     group => group.RelatedLocations,     relatedLocation     => relatedLocation.ToJSON().ToString(Newtonsoft.Json.Formatting.None),
                                                                                                                                                                             relatedLocation     => relatedLocation.ToJSON()),

                                    Facilities:                Groups.Union (ChargingPool.Facilities,           group => group.Facilities),
                                    PaymentOptions:            Groups.Union (ChargingPool.PaymentOptions,       group => group.PaymentOptions),
                                    UIFeatures:                Groups.Union (ChargingPool.UIFeatures,           group => group.UIFeatures),
                                    Features:                  Groups.Union (ChargingPool.Features,             group => group.Features),
                                    LocationLanguages:         Groups.Union (ChargingPool.LocationLanguages,    group => group.LocationLanguages),
                                    ParkingRestrictions:       Groups.Union (ChargingPool.ParkingRestrictions,  group => group.ParkingRestrictions),
                                    Services:                  Groups.Union (ChargingPool.Services,             group => group.Services),
                                    Standards:                 Groups.Union (ChargingPool.Standards,            group => group.Standards),
                                    NUTSCodes:                 Groups.Union (ChargingPool.NUTSCodes,            group => group.NUTSCodes),

                                    // Its effective charging stations, and its very energy meters, with the properties of their groups.
                                    ChargingStations:          ChargingStations,
                                    EnergyMeters:              EnergyMeters,

                                    DataSource:                ChargingPool.DataSource,
                                    Created:                   ChargingPool.Created,
                                    LastChange:                Latest(ChargingStations.Select(chargingStation => chargingStation.LastChangeDate).Append(Groups.LastChange(ChargingPool.LastChangeDate)).Max(),
                                                                      [ AtLeast, .. LastChangesOfEffective(EnergyMeters, ChargingPool.EnergyMeters) ]),
                                    CustomData:                ChargingPool.CustomData

                                );

                effective.ShareRuntimeOf(ChargingPool);

                return effective;

            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"The charging pool '{ChargingPool.Id}' {Groups.With} is invalid: {e.Message}", e);
            }

        }

        #endregion

    }

}
