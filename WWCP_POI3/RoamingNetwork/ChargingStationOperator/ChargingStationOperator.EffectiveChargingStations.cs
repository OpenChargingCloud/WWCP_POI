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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The effective charging stations of a charging station operator: its charging stations as they are at runtime,
    /// with the properties of a charging station their groups give them.
    /// </summary>
    /// <remarks>
    /// The network keeps the properties where they are given, so a charging station may have only few of its own and
    /// take the others from its groups. Of every property of a charging station:
    /// - a single value, e.g. the address, is its own, else the one of its group with the highest priority giving it,
    /// - an array, e.g. the features, is its own united with those of all its groups; of objects with the same
    ///   identification, e.g. brands, or the same URL, e.g. images, its own wins, else the one of the group with the
    ///   higher priority.
    /// The priority of a group is 0, unless given. Groups of a charging station with the same priority must not give it
    /// different values where these decide what it takes; neither must they make it invalid, e.g. by a maximum power
    /// above the one of all its EVSEs together: the operator is refused, and so a change set making it so.
    /// An effective charging station is a version of its own, with the runtime status of the charging station, its very
    /// EVSEs and energy meters; its last change is the latest of the charging station and its groups giving it properties.
    /// </remarks>
    public sealed partial class ChargingStationOperator
    {

        #region Data

        /// <summary>
        /// The effective charging stations differing from the charging stations, by their identifications.
        /// </summary>
        private readonly ImmutableDictionary<ChargingStation_Id, ChargingStation>  effectiveChargingStations
            = ImmutableDictionary<ChargingStation_Id, ChargingStation>.Empty;

        /// <summary>
        /// The charging pools with their effective charging stations, calculated when asked for.
        /// </summary>
        private readonly Lazy<POICollection<ChargingPool_Id, ChargingPool>>       effectiveChargingPools;

        #endregion

        #region Properties

        /// <summary>
        /// The charging stations of all its charging pools as they are at runtime, with the properties their groups give them.
        /// </summary>
        public IEnumerable<ChargingStation> EffectiveChargingStations
            => ChargingStations.Select(EffectiveChargingStation);

        /// <summary>
        /// The charging pools, embedded completely, with their effective charging stations, in the order of their identifications:
        /// a charging pool without any is the charging pool itself.
        /// </summary>
        public POICollection<ChargingPool_Id, ChargingPool> EffectiveChargingPools
            => effectiveChargingPools.Value;

        #endregion


        #region EffectiveChargingStation       (ChargingStation)

        /// <summary>
        /// The given charging station of this operator as it is at runtime, with the properties its groups give it:
        /// the charging station itself, when they give it none.
        /// </summary>
        /// <param name="ChargingStation">A charging station of this operator.</param>
        public ChargingStation EffectiveChargingStation(ChargingStation ChargingStation)

            => effectiveChargingStations.TryGetValue(ChargingStation.Id, out var effectiveChargingStation)
                   ? effectiveChargingStation
                   : ChargingStation;

        #endregion

        #region TryGetEffectiveChargingStation (ChargingStationId, out ChargingStation)

        /// <summary>
        /// Try to return the charging station with the given identification as it is at runtime, with the properties its groups give it.
        /// </summary>
        public Boolean TryGetEffectiveChargingStation(ChargingStation_Id                           ChargingStationId,
                                                      [NotNullWhen(true)] out ChargingStation?     ChargingStation)
        {

            if (!TryGetChargingStation(ChargingStationId, out ChargingStation))
                return false;

            ChargingStation = EffectiveChargingStation(ChargingStation);
            return true;

        }

        #endregion

        #region TryGetEffectiveChargingPool    (ChargingPoolId, out ChargingPool)

        /// <summary>
        /// Try to return the charging pool with the given identification, with its effective charging stations.
        /// </summary>
        public Boolean TryGetEffectiveChargingPool(ChargingPool_Id                        ChargingPoolId,
                                                   [NotNullWhen(true)] out ChargingPool?  ChargingPool)

            => EffectiveChargingPools.TryGet(ChargingPoolId, out ChargingPool);

        #endregion

        #region ChargingStationGroupsOf        (ChargingStationId)

        /// <summary>
        /// The groups the given charging station is a member of, in the order of their identifications.
        /// </summary>
        public IEnumerable<ChargingStationGroup> ChargingStationGroupsOf(ChargingStation_Id ChargingStationId)

            => ChargingStationGroups.Where(chargingStationGroup => chargingStationGroup.ChargingStationIds.Contains(ChargingStationId));

        #endregion


        #region (private) CalculateEffectiveChargingStations()

        /// <summary>
        /// The effective charging stations differing from the charging stations, by their identifications.
        /// </summary>
        private ImmutableDictionary<ChargingStation_Id, ChargingStation> CalculateEffectiveChargingStations()
        {

            var groupsGivingProperties = ChargingStationGroups.Where(chargingStationGroup => chargingStationGroup.HasChargingStationProperties).ToArray();

            if (groupsGivingProperties.Length == 0)
                return ImmutableDictionary<ChargingStation_Id, ChargingStation>.Empty;

            // The groups of each member, the highest priority first.
            var groupsOfMembers = groupsGivingProperties.SelectMany(chargingStationGroup => chargingStationGroup.ChargingStationIds.Select(memberId => (MemberId: memberId, Group: chargingStationGroup))).
                                                         GroupBy   (membership => membership.MemberId).
                                                         ToDictionary(members => members.Key,
                                                                      members => members.Select(membership => membership.Group).
                                                                                         OrderByDescending(chargingStationGroup => chargingStationGroup.Priority ?? 0).
                                                                                         ThenBy           (chargingStationGroup => chargingStationGroup.Id).
                                                                                         ToArray());

            var groupsJSON = groupsGivingProperties.ToDictionary(chargingStationGroup => chargingStationGroup.Id,
                                                                 chargingStationGroup => chargingStationGroup.ToJSON());

            var effective  = ImmutableDictionary.CreateBuilder<ChargingStation_Id, ChargingStation>();

            foreach (var (memberId, groups) in groupsOfMembers)
                if (TryGetChargingStation(memberId, out var chargingStation))
                    effective.Add(chargingStation.Id, Effective(chargingStation, groups, groupsJSON));

            return effective.ToImmutable();

        }

        #endregion

        #region (private) CalculateEffectiveChargingPools()

        /// <summary>
        /// The charging pools with their effective charging stations, sharing the runtime status of the charging pools.
        /// </summary>
        private POICollection<ChargingPool_Id, ChargingPool> CalculateEffectiveChargingPools()
        {

            if (effectiveChargingStations.IsEmpty)
                return ChargingPools;

            var chargingPools = new List<ChargingPool>();

            foreach (var chargingPool in ChargingPools)
            {

                if (!chargingPool.ChargingStations.Any(chargingStation => effectiveChargingStations.ContainsKey(chargingStation.Id)))
                {
                    chargingPools.Add(chargingPool);
                    continue;
                }

                var chargingStations = chargingPool.ChargingStations.Select(EffectiveChargingStation).ToArray();

                chargingPools.Add(chargingPool.WithChargingStations(chargingStations,
                                                                    chargingStations.Select(chargingStation => chargingStation.LastChangeDate).
                                                                                     Append(chargingPool.LastChangeDate).
                                                                                     Max()));

            }

            return new (chargingPools, chargingPool => chargingPool.Id, "charging pool", $"the operator '{Id}'");

        }

        #endregion

        #region (private) ShareRuntimeOfEffective()

        /// <summary>
        /// Let the effective charging stations, and the charging pools with them, take the runtime status of the
        /// charging stations and charging pools again, after these took the one of their previous versions.
        /// </summary>
        private void ShareRuntimeOfEffective()
        {

            foreach (var (chargingStationId, effectiveChargingStation) in effectiveChargingStations)
                if (TryGetChargingStation(chargingStationId, out var chargingStation))
                    effectiveChargingStation.ShareRuntimeOf(chargingStation);

            if (effectiveChargingPools.IsValueCreated)
                foreach (var effectiveChargingPool in effectiveChargingPools.Value)
                    if (TryGetChargingPool(effectiveChargingPool.Id, out var chargingPool) &&
                        !ReferenceEquals(effectiveChargingPool, chargingPool))
                    {
                        effectiveChargingPool.ShareRuntimeOf(chargingPool);
                    }

        }

        #endregion


        #region (private static) Effective(ChargingStation, Groups, GroupsJSON)

        /// <summary>
        /// The given charging station with the properties of the given groups, the highest priority first.
        /// </summary>
        private static ChargingStation Effective(ChargingStation                                         ChargingStation,
                                                 ChargingStationGroup[]                                  Groups,
                                                 IReadOnlyDictionary<ChargingStationGroup_Id, JObject>   GroupsJSON)
        {

            try
            {

                var effective = new ChargingStation(

                                    ChargingStation.Id,
                                    ChargingStation.Name,
                                    ChargingStation.Description,

                                    PhysicalReference:    Single(ChargingStation, Groups, GroupsJSON, "physicalReference",  ChargingStation.PhysicalReference,   group => group.PhysicalReference),
                                    Address:              Single(ChargingStation, Groups, GroupsJSON, "address",            ChargingStation.Address,             group => group.Address),
                                    GeoLocation:          Single(ChargingStation, Groups, GroupsJSON, "geoLocation",        ChargingStation.GeoLocation,         group => group.GeoLocation),
                                    HotlinePhoneNumber:   Single(ChargingStation, Groups, GroupsJSON, "hotline",            ChargingStation.HotlinePhoneNumber,  group => group.HotlinePhoneNumber),
                                    ManufacturerId:       Single(ChargingStation, Groups, GroupsJSON, "manufacturerId",     ChargingStation.ManufacturerId,      group => group.ManufacturerId),
                                    ModelId:              Single(ChargingStation, Groups, GroupsJSON, "modelId",            ChargingStation.ModelId,             group => group.ModelId),
                                    OpeningTimes:         Single(ChargingStation, Groups, GroupsJSON, "openingTimes",       ChargingStation.OpeningTimes,        group => group.OpeningTimes),
                                    Accessibility:        Single(ChargingStation, Groups, GroupsJSON, "accessibility",      ChargingStation.Accessibility,       group => group.Accessibility),
                                    CalibrationInfo:      Single(ChargingStation, Groups, GroupsJSON, "calibrationInfo",    ChargingStation.CalibrationInfo,     group => group.CalibrationInfo),
                                    CertificationInfo:    Single(ChargingStation, Groups, GroupsJSON, "certificationInfo",  ChargingStation.CertificationInfo,   group => group.CertificationInfo),
                                    MaxPower:             Single(ChargingStation, Groups, GroupsJSON, "maxPower",           ChargingStation.MaxPower,            group => group.MaxPower),

                                    Brands:               Union (ChargingStation, Groups, "brand",  ChargingStation.Brands, group => group.Brands, brand => brand.Id,             brand => brand.ToJSON()),
                                    Images:               Union (ChargingStation, Groups, "image",  ChargingStation.Images, group => group.Images, image => image.URL.ToString(), image => image.ToJSON()),

                                    VehicleTypes:         ChargingStation.VehicleTypes.       Concat(Groups.SelectMany(group => group.VehicleTypes)).       Distinct(),
                                    EUVehicleCategories:  ChargingStation.EUVehicleCategories.Concat(Groups.SelectMany(group => group.EUVehicleCategories)).Distinct(),
                                    Features:             ChargingStation.Features.           Concat(Groups.SelectMany(group => group.Features)).           Distinct(),
                                    ParkingSpaceIds:      ChargingStation.ParkingSpaceIds.    Concat(Groups.SelectMany(group => group.ParkingSpaceIds)).    Distinct(),

                                    // Its very EVSEs and energy meters, with their runtime status.
                                    EVSEs:                ChargingStation.EVSEs,
                                    EnergyMeters:         ChargingStation.EnergyMeters,

                                    DataSource:           ChargingStation.DataSource,
                                    Created:              ChargingStation.Created,
                                    LastChange:           Groups.Select(group => group.LastChangeDate).Append(ChargingStation.LastChangeDate).Max(),
                                    CustomData:           ChargingStation.CustomData

                                );

                effective.ShareRuntimeOf(ChargingStation);

                return effective;

            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"The charging station '{ChargingStation.Id}' with the properties of its groups {String.Join(", ", Groups.Select(group => $"'{group.Id}'"))} is invalid: {e.Message}", e);
            }

        }

        #endregion

        #region (private static) Single(ChargingStation, Groups, GroupsJSON, PropertyName, Own, ValueOf)

        /// <summary>
        /// The own value of the given charging station, else the one of its group with the highest priority giving one.
        /// </summary>
        private static T? Single<T>(ChargingStation                                        ChargingStation,
                                    ChargingStationGroup[]                                 Groups,
                                    IReadOnlyDictionary<ChargingStationGroup_Id, JObject>  GroupsJSON,
                                    String                                                 PropertyName,
                                    T?                                                     Own,
                                    Func<ChargingStationGroup, T?>                         ValueOf)

            where T : class

            => Own ?? (T?) Decided(ChargingStation, Groups, GroupsJSON, PropertyName, group => ValueOf(group));

        /// <summary>
        /// The own value of the given charging station, else the one of its group with the highest priority giving one.
        /// </summary>
        private static T? Single<T>(ChargingStation                                        ChargingStation,
                                    ChargingStationGroup[]                                 Groups,
                                    IReadOnlyDictionary<ChargingStationGroup_Id, JObject>  GroupsJSON,
                                    String                                                 PropertyName,
                                    T?                                                     Own,
                                    Func<ChargingStationGroup, T?>                         ValueOf)

            where T : struct

            => Own ?? (T?) Decided(ChargingStation, Groups, GroupsJSON, PropertyName, group => ValueOf(group));

        /// <summary>
        /// The value of the group with the highest priority giving one, unless another one with the same priority gives another.
        /// </summary>
        private static Object? Decided(ChargingStation                                        ChargingStation,
                                       ChargingStationGroup[]                                 Groups,
                                       IReadOnlyDictionary<ChargingStationGroup_Id, JObject>  GroupsJSON,
                                       String                                                 PropertyName,
                                       Func<ChargingStationGroup, Object?>                    ValueOf)
        {

            ChargingStationGroup? decidingGroup = null;
            Object?               value         = null;

            foreach (var group in Groups)
            {

                var candidate = ValueOf(group);

                if (candidate is null)
                    continue;

                if (decidingGroup is null)
                {
                    decidingGroup  = group;
                    value          = candidate;
                    continue;
                }

                if ((group.Priority ?? 0) < (decidingGroup.Priority ?? 0))
                    break;

                // As they write it: e.g. two addresses, or two opening times.
                if (!JToken.DeepEquals(GroupsJSON[group.Id][PropertyName], GroupsJSON[decidingGroup.Id][PropertyName]))
                    throw Conflict(ChargingStation, decidingGroup, group, $"'{PropertyName}'");

            }

            return value;

        }

        #endregion

        #region (private static) Union(ChargingStation, Groups, ElementName, Own, ValuesOf, KeyOf, ToJSON)

        /// <summary>
        /// The own elements of the given charging station and those of its groups: of the same key its own,
        /// else the one of the group with the higher priority.
        /// </summary>
        private static IEnumerable<T> Union<T, TKey>(ChargingStation                                ChargingStation,
                                                     ChargingStationGroup[]                         Groups,
                                                     String                                         ElementName,
                                                     IEnumerable<T>                                 Own,
                                                     Func<ChargingStationGroup, IEnumerable<T>>     ValuesOf,
                                                     Func<T, TKey>                                  KeyOf,
                                                     Func<T, JToken>                                ToJSON)

            where TKey : notnull

        {

            var elements  = new List<T>(Own);
            var taken     = elements.ToDictionary(KeyOf, element => (Element: element, Group: (ChargingStationGroup?) null));

            foreach (var group in Groups)
            {
                foreach (var element in ValuesOf(group))
                {

                    var key = KeyOf(element);

                    if (!taken.TryGetValue(key, out var before))
                    {
                        taken.Add(key, (element, group));
                        elements.Add(element);
                        continue;
                    }

                    if (before.Group is not null &&
                        (before.Group.Priority ?? 0) == (group.Priority ?? 0) &&
                        !JToken.DeepEquals(ToJSON(before.Element), ToJSON(element)))
                    {
                        throw Conflict(ChargingStation, before.Group, group, $"{ElementName} '{key}'");
                    }

                }
            }

            return elements;

        }

        #endregion

        #region (private static) Conflict(ChargingStation, Group1, Group2, What)

        /// <summary>
        /// Two groups of the same priority give the given charging station different values.
        /// </summary>
        private static ArgumentException Conflict(ChargingStation       ChargingStation,
                                                  ChargingStationGroup  Group1,
                                                  ChargingStationGroup  Group2,
                                                  String                What)

            => new ($"The groups '{Group1.Id}' and '{Group2.Id}' give the charging station '{ChargingStation.Id}' different values of {What} with the same priority {Group1.Priority ?? 0}: their priorities must tell which one it takes!");

        #endregion

    }

}
