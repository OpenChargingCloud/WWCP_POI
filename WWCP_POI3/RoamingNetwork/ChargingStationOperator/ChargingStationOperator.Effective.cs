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
    /// The effective charging tariffs, charging pools, charging stations and EVSEs of a charging station operator: as they
    /// are at runtime, with the properties their templates give them.
    /// </summary>
    /// <remarks>
    /// The network keeps the properties where they are given, so a charging pool, charging station, EVSE or charging tariff
    /// may have only few of its own and take the others from the templates it names in its templateIds, in its order. Of
    /// every property they give:
    /// - a single value, e.g. the address, is its own, else the one of the first of its templates giving one,
    /// - an array, e.g. the features, is its own united with those of all its templates; of objects with the same
    ///   identification, e.g. brands or charging connectors, or the same URL, e.g. images, its own wins, else the one of
    ///   the first template giving it.
    /// The charging tariffs of an EVSE are its own; without any, the one of the first of its EVSE templates naming one,
    /// else of the first template of its charging station naming one, else of the first template of its charging pool
    /// naming one: the most specific templates decide. Templates never disagree, but they must not make a member invalid,
    /// e.g. a charging station by a maximum power above the one of all its EVSEs together: the operator is refused, and so
    /// a change set making it so. An effective charging station has the effective EVSEs, an effective charging pool the
    /// effective charging stations; the energy meters they embed take the properties of their energy meter templates the
    /// same way, as effective energy meters. Each is a version of its own, with the runtime status of its member, and its
    /// energy meters; its last change is the latest of the member, its templates and what it embeds, and, where it is other
    /// than in the version before, not before the last change of the operator.
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
        /// The effective charging tariffs differing from the charging tariffs, by their identifications.
        /// </summary>
        private ImmutableDictionary<ChargingTariff_Id,  ChargingTariff>   effectiveChargingTariffs
            = ImmutableDictionary<ChargingTariff_Id, ChargingTariff>.Empty;

        /// <summary>
        /// The effective charging pools, calculated when asked for.
        /// </summary>
        private Lazy<POICollection<ChargingPool_Id, ChargingPool>>        effectiveChargingPools;

        /// <summary>
        /// The time zones of its EVSEs, of their charging pools as they are at runtime, calculated when asked for.
        /// </summary>
        private readonly Lazy<ImmutableDictionary<EVSE_Id, TimeZoneInfo>>  evseTimeZones;

        /// <summary>
        /// The last changes effective members have at least, as StampChangedEffective(…) found them: kept by the next version
        /// while they are the same.
        /// </summary>
        private ChangedMembers                                              effectiveFloors   = new ([], [], []);

        #endregion

        #region Properties

        /// <summary>
        /// The charging pools as they are at runtime, with the properties their templates give them, and with their
        /// effective charging stations, in the order of their identifications: a charging pool without any is itself.
        /// </summary>
        public POICollection<ChargingPool_Id, ChargingPool> EffectiveChargingPools
            => effectiveChargingPools.Value;

        /// <summary>
        /// The charging stations of all its charging pools as they are at runtime, with the properties their templates give
        /// them, and with their effective EVSEs.
        /// </summary>
        public IEnumerable<ChargingStation> EffectiveChargingStations
            => ChargingStations.Select(EffectiveChargingStation);

        /// <summary>
        /// The EVSEs of all its charging pools as they are at runtime, with the properties their templates give them.
        /// </summary>
        public IEnumerable<EVSE> EffectiveEVSEs
            => EVSEs.Select(EffectiveEVSE);

        /// <summary>
        /// The charging tariffs as they are at runtime, with the properties their templates give them, in the order of
        /// their identifications.
        /// </summary>
        public POICollection<ChargingTariff_Id, ChargingTariff> EffectiveChargingTariffs
            => effectiveChargingTariffs.IsEmpty
                   ? ChargingTariffs
                   : new (ChargingTariffs.Select(EffectiveChargingTariff), chargingTariff => chargingTariff.Id, "charging tariff", $"the operator '{Id}'");

        #endregion


        #region EffectiveChargingPool          (ChargingPool)

        /// <summary>
        /// The given charging pool of this operator as it is at runtime, with the properties its templates give it, and
        /// with its effective charging stations: the charging pool itself, when there is nothing to give.
        /// </summary>
        /// <param name="ChargingPool">A charging pool of this operator.</param>
        public ChargingPool EffectiveChargingPool(ChargingPool ChargingPool)

            => effectiveChargingPoolIndex.TryGetValue(ChargingPool.Id, out var effectiveChargingPool)
                   ? effectiveChargingPool
                   : ChargingPool;

        #endregion

        #region EffectiveChargingStation       (ChargingStation)

        /// <summary>
        /// The given charging station of this operator as it is at runtime, with the properties its templates give it, and
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
        /// The given EVSE of this operator as it is at runtime, with the properties its templates give it:
        /// the EVSE itself, when they give it none.
        /// </summary>
        /// <param name="EVSE">An EVSE of this operator.</param>
        public EVSE EffectiveEVSE(EVSE EVSE)

            => effectiveEVSEs.TryGetValue(EVSE.Id, out var effectiveEVSE)
                   ? effectiveEVSE
                   : EVSE;

        #endregion

        #region EffectiveChargingTariff        (ChargingTariff)

        /// <summary>
        /// The given charging tariff of this operator as it is at runtime, with the properties its templates give it: the
        /// charging tariff itself, when it names none.
        /// </summary>
        /// <param name="ChargingTariff">A charging tariff of this operator.</param>
        public ChargingTariff EffectiveChargingTariff(ChargingTariff ChargingTariff)

            => effectiveChargingTariffs.TryGetValue(ChargingTariff.Id, out var effectiveChargingTariff)
                   ? effectiveChargingTariff
                   : ChargingTariff;

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

        #region TryGetEffectiveChargingTariff  (ChargingTariffId,  out ChargingTariff)

        /// <summary>
        /// Try to return the charging tariff with the given identification as it is at runtime.
        /// </summary>
        public Boolean TryGetEffectiveChargingTariff(ChargingTariff_Id                        ChargingTariffId,
                                                     [NotNullWhen(true)] out ChargingTariff?  ChargingTariff)
        {

            if (!TryGetChargingTariff(ChargingTariffId, out ChargingTariff))
                return false;

            ChargingTariff = EffectiveChargingTariff(ChargingTariff);
            return true;

        }

        #endregion

        #region (internal) EffectiveChargingPoolsOfTheContent() / EffectiveChargingTariffsOfTheContent()

        /// <summary>
        /// The effective charging pools differing from the charging pools, by their identifications, as the effective content
        /// of a version has them (SPECIFICATION.md, section 8.1): their last changes without the floors kept at runtime, so
        /// that they follow from the version alone.
        /// </summary>
        internal IReadOnlyDictionary<ChargingPool_Id, ChargingPool> EffectiveChargingPoolsOfTheContent()

            => effectiveFloors.EVSEs.Count == 0 && effectiveFloors.ChargingStations.Count == 0 && effectiveFloors.ChargingPools.Count == 0
                   ? effectiveChargingPoolIndex
                   : CalculateEffective().ChargingPools;

        /// <summary>
        /// The effective charging tariffs differing from the charging tariffs, by their identifications: they have no floors.
        /// </summary>
        internal IReadOnlyDictionary<ChargingTariff_Id, ChargingTariff> EffectiveChargingTariffsOfTheContent()

            => effectiveChargingTariffs;

        #endregion

        #region ChargingPoolGroupsOf / ChargingStationGroupsOf / EVSEGroupsOf / EnergyMeterGroupsOf

        /// <summary>
        /// The groups of this operator the given charging pool is a member of, in the order of their identifications.
        /// </summary>
        public IEnumerable<ChargingPoolGroup> ChargingPoolGroupsOf(ChargingPool_Id ChargingPoolId)

            => ChargingPoolGroups.Where(chargingPoolGroup => chargingPoolGroup.ChargingPoolIds.Contains(ChargingPoolId));

        /// <summary>
        /// The groups of this operator the given charging station is a member of, in the order of their identifications.
        /// </summary>
        public IEnumerable<ChargingStationGroup> ChargingStationGroupsOf(ChargingStation_Id ChargingStationId)

            => ChargingStationGroups.Where(chargingStationGroup => chargingStationGroup.ChargingStationIds.Contains(ChargingStationId));

        /// <summary>
        /// The groups of this operator the given EVSE is a member of, in the order of their identifications.
        /// </summary>
        public IEnumerable<EVSEGroup> EVSEGroupsOf(EVSE_Id EVSEId)

            => EVSEGroups.Where(evseGroup => evseGroup.EVSEIds.Contains(EVSEId));

        /// <summary>
        /// The groups of this operator the given energy meter is a member of, in the order of their identifications.
        /// </summary>
        public IEnumerable<EnergyMeterGroup> EnergyMeterGroupsOf(EnergyMeter_Id EnergyMeterId)

            => EnergyMeterGroups.Where(energyMeterGroup => energyMeterGroup.EnergyMeterIds.Contains(EnergyMeterId));

        #endregion

        #region TemplatesOf(ChargingPool / ChargingStation / EVSE / ChargingTariff / EnergyMeter)

        /// <summary>
        /// The templates the given charging pool names, in its order.
        /// </summary>
        public IEnumerable<ChargingPoolTemplate> TemplatesOf(ChargingPool ChargingPool)

            => TemplatesOf(ChargingPool.TemplateIds, ChargingPoolTemplates);

        /// <summary>
        /// The templates the given charging station names, in its order.
        /// </summary>
        public IEnumerable<ChargingStationTemplate> TemplatesOf(ChargingStation ChargingStation)

            => TemplatesOf(ChargingStation.TemplateIds, ChargingStationTemplates);

        /// <summary>
        /// The templates the given EVSE names, in its order.
        /// </summary>
        public IEnumerable<EVSETemplate> TemplatesOf(EVSE EVSE)

            => TemplatesOf(EVSE.TemplateIds, EVSETemplates);

        /// <summary>
        /// The templates the given charging tariff names, in its order.
        /// </summary>
        public IEnumerable<ChargingTariffTemplate> TemplatesOf(ChargingTariff ChargingTariff)

            => TemplatesOf(ChargingTariff.TemplateIds, ChargingTariffTemplates);

        /// <summary>
        /// The templates the given energy meter of one of its charging pools, charging stations or EVSEs names, in its order.
        /// </summary>
        public IEnumerable<EnergyMeterTemplate> TemplatesOf(EnergyMeter EnergyMeter)

            => TemplatesOf(EnergyMeter.TemplateIds, EnergyMeterTemplates);

        #endregion


        #region (private) CalculateEffectiveChargingTariffs()

        /// <summary>
        /// The effective charging tariffs differing from the given ones, by their identifications: those naming templates.
        /// A tariff naming none must have a currency and elements of its own.
        /// </summary>
        private ImmutableDictionary<ChargingTariff_Id, ChargingTariff> CalculateEffectiveChargingTariffs()
        {

            var chargingTariffs = ImmutableDictionary.CreateBuilder<ChargingTariff_Id, ChargingTariff>();

            foreach (var chargingTariff in ChargingTariffs)
            {

                if (chargingTariff.TemplateIds.Length == 0)
                {

                    if (!chargingTariff.IsComplete)
                        throw new ArgumentException(chargingTariff.Currency is null
                                                        ? $"The charging tariff '{chargingTariff.Id}' has no currency, neither of its own nor of its templates!"
                                                        : $"The charging tariff '{chargingTariff.Id}' has no elements, neither of its own nor of its templates!");

                    continue;

                }

                chargingTariffs.Add(chargingTariff.Id,
                                    ChargingTariffTemplate.Effective(chargingTariff, Values(chargingTariff.TemplateIds, ChargingTariffTemplates)));

            }

            return chargingTariffs.ToImmutable();

        }

        #endregion

        #region (private) CalculateEffective(Changed = null, Previous = null)

        /// <summary>
        /// The last change members have at least: the one of the operator, where they are other than in the version before;
        /// the one they had in it, where they are the same.
        /// </summary>
        private sealed record ChangedMembers(Dictionary<EVSE_Id,            DateTimeOffset>  EVSEs,
                                             Dictionary<ChargingStation_Id, DateTimeOffset>  ChargingStations,
                                             Dictionary<ChargingPool_Id,    DateTimeOffset>  ChargingPools);

        /// <summary>
        /// The effective EVSEs, charging stations and charging pools differing from the given ones, by their identifications;
        /// the given changed members as well, with at least their last change. Those of the charging pools kept from the given
        /// previous version are kept as well where nothing they are made of changed, neither their templates nor their floors,
        /// so that they keep their digests (SPECIFICATION.md, section 8.7).
        /// </summary>
        /// <param name="Changed">Optionally, the changed members, with at least their last change.</param>
        /// <param name="Previous">Optionally, a previous version: the effective members of the charging pools kept from it are kept.</param>
        private (ImmutableDictionary<EVSE_Id,            EVSE>             EVSEs,
                 ImmutableDictionary<ChargingStation_Id, ChargingStation>  ChargingStations,
                 ImmutableDictionary<ChargingPool_Id,    ChargingPool>     ChargingPools)  CalculateEffective(ChangedMembers?           Changed    = null,
                                                                                                         ChargingStationOperator?  Previous   = null)
        {

            #region Energy meters, with the properties of their templates

            static Boolean HasTemplates(EnergyMeter? EnergyMeter)

                => EnergyMeter is not null &&
                   EnergyMeter.TemplateIds.Length > 0;

            EnergyMeter EffectiveMeter(EnergyMeter EnergyMeter)

                => HasTemplates(EnergyMeter)
                       ? EnergyMeterTemplate.Effective(EnergyMeter, Values(EnergyMeter.TemplateIds, EnergyMeterTemplates))
                       : EnergyMeter;

            #endregion

            #region What may be kept: the members of the charging pools kept from the previous version, taking no changed template

            var changedEVSETemplates             = Previous is not null ? ChangedTemplates(EVSETemplates,            Previous.EVSETemplates,            template => template.Id) : [];
            var changedChargingStationTemplates  = Previous is not null ? ChangedTemplates(ChargingStationTemplates, Previous.ChargingStationTemplates, template => template.Id) : [];
            var changedChargingPoolTemplates     = Previous is not null ? ChangedTemplates(ChargingPoolTemplates,    Previous.ChargingPoolTemplates,    template => template.Id) : [];
            var changedEnergyMeterTemplates      = Previous is not null ? ChangedTemplates(EnergyMeterTemplates,     Previous.EnergyMeterTemplates,     template => template.Id) : [];

            // The same charging pool, with the same charging stations and EVSEs.
            Boolean IsKept(ChargingPool ChargingPool)

                => Previous is not null &&
                   Previous.TryGetChargingPool(ChargingPool.Id, out var previousChargingPool) &&
                   ReferenceEquals(previousChargingPool, ChargingPool);

            static Boolean NoneChanged<TTemplateId>(ImmutableArray<TTemplateId>  TemplateIds,
                                                    HashSet<TTemplateId>         ChangedTemplates)

                => !TemplateIds.Any(ChangedTemplates.Contains);

            Boolean MeterKept(EnergyMeter? EnergyMeter)

                => EnergyMeter is null ||
                   NoneChanged(EnergyMeter.TemplateIds, changedEnergyMeterTemplates);

            static DateTimeOffset? FloorOf<TId>(ChangedMembers?                                        Floors,
                                                Func<ChangedMembers, Dictionary<TId, DateTimeOffset>>  Of,
                                                TId                                                    Id)

                where TId : notnull

                => Floors is not null && Of(Floors).TryGetValue(Id, out var floor)
                       ? floor
                       : null;

            // The effective member is the one of the previous version, or neither has one.
            static Boolean SameAsBefore<TId, TEntity>(ImmutableDictionary<TId, TEntity>.Builder  Current,
                                                      ImmutableDictionary<TId, TEntity>          Before,
                                                      TId                                        Id)

                where TId     : notnull
                where TEntity : class

                => Current.TryGetValue(Id, out var current)
                       ? Before.TryGetValue(Id, out var before) && ReferenceEquals(current, before)
                       : !Before.ContainsKey(Id);

            #endregion

            #region EVSEs, with the charging tariff of their templates, unless they have their own

            var evses = ImmutableDictionary.CreateBuilder<EVSE_Id, EVSE>();

            foreach (var chargingPool in ChargingPools)
            {

                var kept = IsKept(chargingPool);

                foreach (var chargingStation in chargingPool.ChargingStations)
                {
                    foreach (var evse in chargingStation.EVSEs)
                    {

                        DateTimeOffset? atLeast = FloorOf(Changed, floors => floors.EVSEs, evse.Id);

                        // As in the previous version: so is its tariff, of its own templates or of those of its charging
                        // station and charging pool.
                        if (kept &&
                            NoneChanged(evse.           TemplateIds, changedEVSETemplates)            &&
                            NoneChanged(chargingStation.TemplateIds, changedChargingStationTemplates) &&
                            NoneChanged(chargingPool.   TemplateIds, changedChargingPoolTemplates)    &&
                            MeterKept(evse.EnergyMeter) &&
                            atLeast == FloorOf(Previous!.effectiveFloors, floors => floors.EVSEs, evse.Id))
                        {

                            if (Previous.effectiveEVSEs.TryGetValue(evse.Id, out var keptEVSE))
                                evses.Add(evse.Id, keptEVSE);

                            continue;

                        }

                        // Its own tariffs, else the one of its most specific templates naming one.
                        var tariff  = evse.ChargingTariffIds.Length > 0
                                          ? null
                                          : TariffOf(Values(evse.           TemplateIds, EVSETemplates),            template => template.ChargingTariffId) ??
                                            TariffOf(Values(chargingStation.TemplateIds, ChargingStationTemplates), template => template.ChargingTariffId) ??
                                            TariffOf(Values(chargingPool.   TemplateIds, ChargingPoolTemplates),    template => template.ChargingTariffId);

                        if (evse.TemplateIds.Length == 0 && tariff is null && !atLeast.HasValue && !HasTemplates(evse.EnergyMeter))
                            continue;

                        evses.Add(evse.Id, Effective(evse,
                                                     Values(evse.TemplateIds, EVSETemplates),
                                                     tariff,
                                                     evse.EnergyMeter is EnergyMeter energyMeter ? EffectiveMeter(energyMeter) : null,
                                                     atLeast));

                    }
                }

            }

            #endregion

            #region Charging stations, with their effective EVSEs

            var chargingStations = ImmutableDictionary.CreateBuilder<ChargingStation_Id, ChargingStation>();

            foreach (var chargingPool in ChargingPools)
            {

                var kept = IsKept(chargingPool);

                foreach (var chargingStation in chargingPool.ChargingStations)
                {

                    DateTimeOffset? atLeast = FloorOf(Changed, floors => floors.ChargingStations, chargingStation.Id);

                    if (kept &&
                        NoneChanged(chargingStation.TemplateIds, changedChargingStationTemplates) &&
                        chargingStation.EnergyMeters.All(MeterKept) &&
                        chargingStation.EVSEs.All(evse => SameAsBefore(evses, Previous!.effectiveEVSEs, evse.Id)) &&
                        atLeast == FloorOf(Previous!.effectiveFloors, floors => floors.ChargingStations, chargingStation.Id))
                    {

                        if (Previous.effectiveChargingStations.TryGetValue(chargingStation.Id, out var keptChargingStation))
                            chargingStations.Add(chargingStation.Id, keptChargingStation);

                        continue;

                    }

                    if (chargingStation.TemplateIds.Length == 0 && !atLeast.HasValue && !chargingStation.EVSEs.Any(evse => evses.ContainsKey(evse.Id)) && !chargingStation.EnergyMeters.Any(HasTemplates))
                        continue;

                    chargingStations.Add(chargingStation.Id,
                                         Effective(chargingStation,
                                                   Values(chargingStation.TemplateIds, ChargingStationTemplates),
                                                   [.. chargingStation.EVSEs.Select(evse => evses.TryGetValue(evse.Id, out var effectiveEVSE) ? effectiveEVSE : evse)],
                                                   [.. chargingStation.EnergyMeters.Select(EffectiveMeter)],
                                                   atLeast));

                }

            }

            #endregion

            #region Charging pools, with their effective charging stations

            var chargingPools = ImmutableDictionary.CreateBuilder<ChargingPool_Id, ChargingPool>();

            foreach (var chargingPool in ChargingPools)
            {

                DateTimeOffset? atLeast = FloorOf(Changed, floors => floors.ChargingPools, chargingPool.Id);

                if (IsKept(chargingPool) &&
                    NoneChanged(chargingPool.TemplateIds, changedChargingPoolTemplates) &&
                    chargingPool.EnergyMeters.All(MeterKept) &&
                    chargingPool.ChargingStations.All(chargingStation => SameAsBefore(chargingStations, Previous!.effectiveChargingStations, chargingStation.Id)) &&
                    atLeast == FloorOf(Previous!.effectiveFloors, floors => floors.ChargingPools, chargingPool.Id))
                {

                    if (Previous.effectiveChargingPoolIndex.TryGetValue(chargingPool.Id, out var keptChargingPool))
                        chargingPools.Add(chargingPool.Id, keptChargingPool);

                    continue;

                }

                if (chargingPool.TemplateIds.Length == 0 && !atLeast.HasValue && !chargingPool.ChargingStations.Any(chargingStation => chargingStations.ContainsKey(chargingStation.Id)) && !chargingPool.EnergyMeters.Any(HasTemplates))
                    continue;

                chargingPools.Add(chargingPool.Id,
                                  Effective(chargingPool,
                                            Values(chargingPool.TemplateIds, ChargingPoolTemplates),
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
        /// with at least the last change of this operator: so a template that stops giving a member something, or a member
        /// that stops naming a template, changes its last change as well, not only its content; and where they are the same,
        /// with at least the last change they had in it, e.g. after an earlier change of the same change set: it never goes
        /// back. Floors found before are kept, the later one of two: a merge compares with both of its parents, one after the
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
                    json.Remove(BasePropertyKeys.LastChange);
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

            #region Only where something may be other: the members naming templates that changed, and those having a floor before

            // A member whose own representation changed has the last change of the change set anyway: only its templates
            // can change it without a change of its own, or the floor it had keep it. The EVSEs of a charging station or a
            // charging pool naming other templates than before may take another tariff of them.
            var evseCandidates             = new HashSet<EVSE_Id>           (Previous.effectiveFloors.EVSEs.Keys);
            var chargingStationCandidates  = new HashSet<ChargingStation_Id>(Previous.effectiveFloors.ChargingStations.Keys);
            var chargingPoolCandidates     = new HashSet<ChargingPool_Id>   (Previous.effectiveFloors.ChargingPools.Keys);

            evseCandidates.           UnionWith(AlsoEVSEs            ?? []);
            chargingStationCandidates.UnionWith(AlsoChargingStations ?? []);

            var changedEVSETemplates             = ChangedTemplates(EVSETemplates,            Previous.EVSETemplates,            template => template.Id);
            var changedChargingStationTemplates  = ChangedTemplates(ChargingStationTemplates, Previous.ChargingStationTemplates, template => template.Id);
            var changedChargingPoolTemplates     = ChangedTemplates(ChargingPoolTemplates,    Previous.ChargingPoolTemplates,    template => template.Id);
            var changedEnergyMeterTemplates      = ChangedTemplates(EnergyMeterTemplates,     Previous.EnergyMeterTemplates,     template => template.Id);

            foreach (var chargingPool in ChargingPools)
            {

                var poolChanged         = chargingPool.TemplateIds.Any(changedChargingPoolTemplates.Contains);
                var poolTemplatesOther  = Previous.TryGetChargingPool(chargingPool.Id, out var previousPool) &&
                                          !previousPool.TemplateIds.SequenceEqual(chargingPool.TemplateIds);

                if (poolChanged || chargingPool.EnergyMeters.Any(energyMeter => energyMeter.TemplateIds.Any(changedEnergyMeterTemplates.Contains)))
                    chargingPoolCandidates.Add(chargingPool.Id);

                foreach (var chargingStation in chargingPool.ChargingStations)
                {

                    var stationChanged         = chargingStation.TemplateIds.Any(changedChargingStationTemplates.Contains);
                    var stationTemplatesOther  = Previous.TryGetChargingStation(chargingStation.Id, out var previousStation) &&
                                                 !previousStation.TemplateIds.SequenceEqual(chargingStation.TemplateIds);

                    if (stationChanged || chargingStation.EnergyMeters.Any(energyMeter => energyMeter.TemplateIds.Any(changedEnergyMeterTemplates.Contains)))
                        chargingStationCandidates.Add(chargingStation.Id);

                    // The tariff of a charging station template or charging pool template is the one of their EVSEs.
                    foreach (var evse in chargingStation.EVSEs)
                        if (poolChanged || stationChanged || poolTemplatesOther || stationTemplatesOther ||
                            evse.TemplateIds.Any(changedEVSETemplates.Contains) ||
                            (evse.EnergyMeter is not null && evse.EnergyMeter.TemplateIds.Any(changedEnergyMeterTemplates.Contains)))
                        {
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
                    FloorOf(new (EffectiveChargingStation(chargingStation)), new (Previous.EffectiveChargingStation(previousChargingStation)), ChargingStation.PropertyKeys.EVSEs) is DateTimeOffset floor)
                {
                    found = Raise(chargingStations, chargingStation.Id, floor) | found;
                }

            foreach (var chargingPoolId in chargingPoolCandidates)
                if (TryGetChargingPool(chargingPoolId, out var chargingPool) && Previous.TryGetChargingPool(chargingPoolId, out var previousChargingPool) &&
                    FloorOf(new (EffectiveChargingPool(chargingPool)), new (Previous.EffectiveChargingPool(previousChargingPool)), ChargingPool.PropertyKeys.ChargingStations) is DateTimeOffset floor)
                {
                    found = Raise(chargingPools, chargingPool.Id, floor) | found;
                }

            if (!found)
                return;

            effectiveFloors = new (evses, chargingStations, chargingPools);

            (effectiveEVSEs, effectiveChargingStations, effectiveChargingPoolIndex) = CalculateEffective(effectiveFloors, Previous);
            effectiveChargingPools = new (CalculateEffectiveChargingPools);

        }

        #endregion

        #region (private) ShareRuntimeOfEffective()

        /// <summary>
        /// Let the effective EVSEs, charging stations, charging pools and charging tariffs, and the effective energy meters
        /// they embed, take the runtime status of the EVSEs, charging stations, charging pools, charging tariffs and energy
        /// meters again, after these took the one of their previous versions.
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

            foreach (var (chargingTariffId, effectiveChargingTariff) in effectiveChargingTariffs)
                if (TryGetChargingTariff(chargingTariffId, out var chargingTariff))
                    effectiveChargingTariff.ShareRuntimeOf(chargingTariff);

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

        #region (internal static) ChangedTemplates(Current, Before, IdOf)

        /// <summary>
        /// The identifications of the templates that are other than before, added or removed: only these can give the members
        /// naming them something else.
        /// </summary>
        internal static HashSet<TTemplateId> ChangedTemplates<TTemplateId, TTemplate>(POICollection<TTemplateId, TTemplate>  Current,
                                                                                    POICollection<TTemplateId, TTemplate>  Before,
                                                                                    Func<TTemplate, TTemplateId>           IdOf)

            where TTemplateId : notnull
            where TTemplate   : class, IPOITemplate

        {

            var changed = new HashSet<TTemplateId>();

            foreach (var template in Current)
                if (!Before.TryGet(IdOf(template), out var before) || !POIJSON.SameValue(template.ToJSON(Embedded: true), before.ToJSON(Embedded: true)))
                    changed.Add(IdOf(template));

            foreach (var before in Before)
                if (!Current.TryGet(IdOf(before), out _))
                    changed.Add(IdOf(before));

            return changed;

        }

        #endregion

        #region (internal static) Values(TemplateIds, Templates)

        /// <summary>
        /// The values the templates a member names give it, in its order; the templates exist, as the owner checked.
        /// </summary>
        internal static POITemplateValues<TTemplate> Values<TTemplateId, TTemplate>(ImmutableArray<TTemplateId>            TemplateIds,
                                                                                    POICollection<TTemplateId, TTemplate>  Templates)

            where TTemplateId : notnull
            where TTemplate   : class, IPOITemplate

            => new (TemplatesOf(TemplateIds, Templates));

        #endregion

        #region (internal static) TemplatesOf(TemplateIds, Templates)

        /// <summary>
        /// The templates a member names, in its order; an unknown one is left out.
        /// </summary>
        internal static IEnumerable<TTemplate> TemplatesOf<TTemplateId, TTemplate>(ImmutableArray<TTemplateId>            TemplateIds,
                                                                                   POICollection<TTemplateId, TTemplate>  Templates)

            where TTemplateId : notnull
            where TTemplate   : class

        {

            foreach (var templateId in TemplateIds)
                if (Templates.TryGet(templateId, out var template))
                    yield return template;

        }

        #endregion

        #region (private) AEntity

        /// <summary>
        /// An effective member, with its JSON representation and its last change.
        /// </summary>
        private readonly record struct AEntity(Func<JObject> ToJSONOf, DateTimeOffset LastChangeDate)
        {
            public AEntity(EVSE            EVSE)            : this(() => EVSE.           ToJSON(Embedded: true), EVSE.           LastChangeDate) { }
            public AEntity(ChargingStation ChargingStation) : this(() => ChargingStation.ToJSON(Embedded: true), ChargingStation.LastChangeDate) { }
            public AEntity(ChargingPool    ChargingPool)    : this(() => ChargingPool.   ToJSON(Embedded: true), ChargingPool.   LastChangeDate) { }
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
        /// The last change of the given effective energy meter, where its templates give it properties: what a member embeds
        /// as it is at runtime is part of it. Null where it is the energy meter itself.
        /// </summary>
        private static DateTimeOffset? LastChangeOfEffective(EnergyMeter?  Effective,
                                                             EnergyMeter?  Own)

            => Effective is not null && !ReferenceEquals(Effective, Own)
                   ? Effective.LastChangeDate
                   : null;

        /// <summary>
        /// The last changes of the given effective energy meters, where their templates give them properties.
        /// </summary>
        private static IEnumerable<DateTimeOffset?> LastChangesOfEffective(IEnumerable<EnergyMeter>  Effective,
                                                                           IEnumerable<EnergyMeter>  Own)

            => Effective.Select(effective => LastChangeOfEffective(effective, Own.FirstOrDefault(own => own.Id == effective.Id)));

        #endregion

        #region (private static) TariffOf(Templates, TariffOf)

        /// <summary>
        /// The charging tariff of the first of the given templates of a member naming one, with the last change of that
        /// template; none without one.
        /// </summary>
        private static (ChargingTariff_Id Id, DateTimeOffset LastChange)? TariffOf<TTemplate>(POITemplateValues<TTemplate>         Templates,
                                                                                              Func<TTemplate, ChargingTariff_Id?>  TariffOf)

            where TTemplate : class, IPOITemplate

        {

            foreach (var template in Templates.Templates)
                if (TariffOf(template) is ChargingTariff_Id tariffId)
                    return (tariffId, template.LastChangeDate);

            return null;

        }

        #endregion

        #region (private static) Effective(EVSE,            Templates, Tariff, EnergyMeter)

        /// <summary>
        /// The given EVSE with the properties of the given templates, the given charging tariff of its templates, if any,
        /// and its given effective energy meter.
        /// </summary>
        private static EVSE Effective(EVSE                                                 EVSE,
                                      POITemplateValues<EVSETemplate>                      Templates,
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

                                    PhysicalReference:         Templates.Single(EVSE.PhysicalReference,         template => template.PhysicalReference),
                                    CurrentTypes:              Templates.Single(EVSE.CurrentTypes  != POI.CurrentTypes. Unspecified ? EVSE.CurrentTypes  : null,
                                                                                template => template.CurrentTypes  != POI.CurrentTypes. Unspecified ? template.CurrentTypes  : (CurrentTypes?)  null),
                                    ChargingModes:             Templates.Single(EVSE.ChargingModes != POI.ChargingModes.Unspecified ? EVSE.ChargingModes : null,
                                                                                template => template.ChargingModes != POI.ChargingModes.Unspecified ? template.ChargingModes : (ChargingModes?) null),
                                    MaxVoltage:                Templates.Single(EVSE.MaxVoltage,                template => template.MaxVoltage),
                                    MaxCurrent:                Templates.Single(EVSE.MaxCurrent,                template => template.MaxCurrent),
                                    MaxPower:                  Templates.Single(EVSE.MaxPower,                  template => template.MaxPower),
                                    GeoLocation:               Templates.Single(EVSE.GeoLocation,               template => template.GeoLocation),
                                    MaxReservationDuration:    Templates.Single(EVSE.MaxReservationDuration,    template => template.MaxReservationDuration),
                                    ReachDistance:             Templates.Single(EVSE.ReachDistance,             template => template.ReachDistance),
                                    OperationTimeout:          Templates.Single(EVSE.OperationTimeout,          template => template.OperationTimeout),
                                    ExtendedOperationTimeout:  Templates.Single(EVSE.ExtendedOperationTimeout,  template => template.ExtendedOperationTimeout),

                                    ChargingConnectors:        Templates.Union (EVSE.ChargingConnectors, template => template.ChargingConnectors, chargingConnector => chargingConnector.Id),
                                    Brands:                    Templates.Union (EVSE.Brands,             template => template.Brands,             brand             => brand.Id),
                                    Images:                    Templates.Union (EVSE.Images,             template => template.Images,             image             => image.URL.ToString()),
                                    Standards:                 Templates.Union (EVSE.Standards,          template => template.Standards),
                                    FloorLevel:                Templates.Single(EVSE.FloorLevel,                template => template.FloorLevel),
                                    AcceptedEMobilityProviderIds:  Templates.Union (EVSE.AcceptedEMobilityProviderIds, template => template.AcceptedEMobilityProviderIds),

                                    // Its own tariffs, else the one of its templates; its energy meter is a device of its own,
                                    // with the properties of its energy meter templates.
                                    ChargingTariffIds:         Tariff.HasValue ? [ Tariff.Value.Id ] : EVSE.ChargingTariffIds,
                                    EnergyMeter:               EnergyMeter,
                                    TemplateIds:               EVSE.TemplateIds,

                                    DataSource:                EVSE.DataSource,
                                    Created:                   EVSE.Created,
                                    LastChange:                Latest(Templates.LastChange(EVSE.LastChangeDate), Tariff?.LastChange, AtLeast, LastChangeOfEffective(EnergyMeter, EVSE.EnergyMeter)),
                                    CustomData:                EVSE.CustomData

                                );

                effective.ShareRuntimeOf(EVSE);

                return effective;

            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"The EVSE '{EVSE.Id}' {Templates.With} is invalid: {e.Message}", e);
            }

        }

        #endregion

        #region (private static) Effective(ChargingStation, Templates, EVSEs, EnergyMeters)

        /// <summary>
        /// The given charging station with the properties of the given templates, the given effective EVSEs, and its given
        /// effective energy meters.
        /// </summary>
        private static ChargingStation Effective(ChargingStation                             ChargingStation,
                                                 POITemplateValues<ChargingStationTemplate>  Templates,
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

                                    PhysicalReference:    Templates.Single(ChargingStation.PhysicalReference,   template => template.PhysicalReference),
                                    Address:              Templates.Single(ChargingStation.Address,             template => template.Address),
                                    GeoLocation:          Templates.Single(ChargingStation.GeoLocation,         template => template.GeoLocation),
                                    HotlinePhoneNumber:   Templates.Single(ChargingStation.HotlinePhoneNumber,  template => template.HotlinePhoneNumber),
                                    ManufacturerId:       Templates.Single(ChargingStation.ManufacturerId,      template => template.ManufacturerId),
                                    ModelId:              Templates.Single(ChargingStation.ModelId,             template => template.ModelId),
                                    OpeningTimes:         Templates.Single(ChargingStation.OpeningTimes,        template => template.OpeningTimes),
                                    Accessibility:        Templates.Single(ChargingStation.Accessibility,       template => template.Accessibility),
                                    CalibrationInfo:      Templates.Single(ChargingStation.CalibrationInfo,     template => template.CalibrationInfo),
                                    CertificationInfo:    Templates.Single(ChargingStation.CertificationInfo,   template => template.CertificationInfo),
                                    MaxPower:             Templates.Single(ChargingStation.MaxPower,            template => template.MaxPower),

                                    Brands:               Templates.Union (ChargingStation.Brands, template => template.Brands, brand => brand.Id),
                                    Images:               Templates.Union (ChargingStation.Images, template => template.Images, image => image.URL.ToString()),

                                    VehicleTypes:         Templates.Union (ChargingStation.VehicleTypes,        template => template.VehicleTypes),
                                    EUVehicleCategories:  Templates.Union (ChargingStation.EUVehicleCategories, template => template.EUVehicleCategories),
                                    Features:             Templates.Union (ChargingStation.Features,            template => template.Features),
                                    ParkingSpaceIds:      Templates.Union (ChargingStation.ParkingSpaceIds,     template => template.ParkingSpaceIds),

                                    // Its effective EVSEs, and its very energy meters, with the properties of their templates.
                                    EVSEs:                EVSEs,
                                    EnergyMeters:         EnergyMeters,
                                    TemplateIds:          ChargingStation.TemplateIds,

                                    DataSource:           ChargingStation.DataSource,
                                    Created:              ChargingStation.Created,
                                    LastChange:           Latest(EVSEs.Select(evse => evse.LastChangeDate).Append(Templates.LastChange(ChargingStation.LastChangeDate)).Max(),
                                                                 [ AtLeast, .. LastChangesOfEffective(EnergyMeters, ChargingStation.EnergyMeters) ]),
                                    CustomData:           ChargingStation.CustomData

                                );

                effective.ShareRuntimeOf(ChargingStation);

                return effective;

            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"The charging station '{ChargingStation.Id}' {Templates.With} is invalid: {e.Message}", e);
            }

        }

        #endregion

        #region (private static) Effective(ChargingPool,    Templates, ChargingStations, EnergyMeters)

        /// <summary>
        /// The given charging pool with the properties of the given templates, the given effective charging stations, and
        /// its given effective energy meters.
        /// </summary>
        private static ChargingPool Effective(ChargingPool                             ChargingPool,
                                              POITemplateValues<ChargingPoolTemplate>  Templates,
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

                                    Address:                   Templates.Single(ChargingPool.Address,                          template => template.Address),
                                    GeoLocation:               Templates.Single(ChargingPool.GeoLocation,                      template => template.GeoLocation),
                                    ParkingType:               Templates.Single(ChargingPool.ParkingType,                      template => template.ParkingType),
                                    Accessibility:             Templates.Single(ChargingPool.Accessibility,                    template => template.Accessibility),
                                    HotlinePhoneNumber:        Templates.Single(ChargingPool.HotlinePhoneNumber,               template => template.HotlinePhoneNumber),
                                    GridConnectionPointId:     Templates.Single(ChargingPool.GridConnectionPointId,            template => template.GridConnectionPointId),
                                    OpeningTimes:              Templates.Single(ChargingPool.OpeningTimes,                     template => template.OpeningTimes),
                                    EnergyMix:                 Templates.Single(ChargingPool.EnergyMix,                        template => template.EnergyMix),
                                    TimeZone:                  Templates.Single(ChargingPool.TimeZone,                         template => template.TimeZone),
                                    EntranceAddress:           Templates.Single(ChargingPool.EntranceAddress,                  template => template.EntranceAddress),
                                    EntranceLocation:          Templates.Single(ChargingPool.EntranceLocation,                 template => template.EntranceLocation),
                                    ExitAddress:               Templates.Single(ChargingPool.ExitAddress,                      template => template.ExitAddress),
                                    ExitLocation:              Templates.Single(ChargingPool.ExitLocation,                     template => template.ExitLocation),
                                    ArrivalInstructions:       Templates.Single(Given(ChargingPool.ArrivalInstructions),       template => Given(template.ArrivalInstructions)),
                                    ChargingWhenClosed:        Templates.Single(ChargingPool.ChargingWhenClosed,               template => template.ChargingWhenClosed),
                                    AssistanceServiceDetails:  Templates.Single(Given(ChargingPool.AssistanceServiceDetails),  template => Given(template.AssistanceServiceDetails)),

                                    DataLicenses:              Templates.Union (ChargingPool.DataLicenses,         template => template.DataLicenses,         dataLicense         => dataLicense.Id),
                                    Brands:                    Templates.Union (ChargingPool.Brands,               template => template.Brands,               brand               => brand.Id),
                                    Images:                    Templates.Union (ChargingPool.Images,               template => template.Images,               image               => image.URL.ToString()),
                                    AuthenticationModes:       Templates.Union (ChargingPool.AuthenticationModes,  template => template.AuthenticationModes,  authenticationMode  => authenticationMode.Type),
                                    RelatedLocations:          Templates.Union (ChargingPool.RelatedLocations,     template => template.RelatedLocations,     relatedLocation     => relatedLocation.ToJSON(Embedded: true).ToString(Newtonsoft.Json.Formatting.None)),

                                    Facilities:                Templates.Union (ChargingPool.Facilities,           template => template.Facilities),
                                    PaymentOptions:            Templates.Union (ChargingPool.PaymentOptions,       template => template.PaymentOptions),
                                    UIFeatures:                Templates.Union (ChargingPool.UIFeatures,           template => template.UIFeatures),
                                    Features:                  Templates.Union (ChargingPool.Features,             template => template.Features),
                                    LocationLanguages:         Templates.Union (ChargingPool.LocationLanguages,    template => template.LocationLanguages),
                                    ParkingRestrictions:       Templates.Union (ChargingPool.ParkingRestrictions,  template => template.ParkingRestrictions),
                                    Services:                  Templates.Union (ChargingPool.Services,             template => template.Services),
                                    Standards:                 Templates.Union (ChargingPool.Standards,            template => template.Standards),
                                    NUTSCodes:                 Templates.Union (ChargingPool.NUTSCodes,            template => template.NUTSCodes),

                                    // Its effective charging stations, and its very energy meters, with the properties of their templates.
                                    ChargingStations:          ChargingStations,
                                    EnergyMeters:              EnergyMeters,
                                    TemplateIds:               ChargingPool.TemplateIds,

                                    DataSource:                ChargingPool.DataSource,
                                    Created:                   ChargingPool.Created,
                                    LastChange:                Latest(ChargingStations.Select(chargingStation => chargingStation.LastChangeDate).Append(Templates.LastChange(ChargingPool.LastChangeDate)).Max(),
                                                                      [ AtLeast, .. LastChangesOfEffective(EnergyMeters, ChargingPool.EnergyMeters) ]),
                                    CustomData:                ChargingPool.CustomData

                                );

                effective.ShareRuntimeOf(ChargingPool);

                return effective;

            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"The charging pool '{ChargingPool.Id}' {Templates.With} is invalid: {e.Message}", e);
            }

        }

        #endregion

    }

}
