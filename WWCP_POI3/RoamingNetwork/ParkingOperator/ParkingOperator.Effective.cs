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
    /// The effective parking spaces of a parking operator: its parking spaces as they are at runtime, with the properties
    /// their parking space groups give them.
    /// </summary>
    /// <remarks>
    /// As the effective members of a charging station operator (see ChargingStationOperator.EffectiveChargingPools):
    /// a single value is the parking space's own, else the one of its group with the highest priority; an array is its own
    /// united with those of all its groups, of images of the same URL its own, else the one of the higher priority. Groups
    /// of the same priority must not give different values that decide, nor make it invalid. An effective parking space has
    /// the runtime status of the parking space; its last change is the latest of it and its groups, and, where it is other
    /// than in the version before, not before the last change of the parking operator.
    /// </remarks>
    public sealed partial class ParkingOperator
    {

        #region Data

        /// <summary>
        /// The effective parking spaces differing from the parking spaces, by their identifications.
        /// </summary>
        private ImmutableDictionary<ParkingSpace_Id, ParkingSpace>          effectiveParkingSpaceIndex
            = ImmutableDictionary<ParkingSpace_Id, ParkingSpace>.Empty;

        /// <summary>
        /// The effective parking spaces, calculated when asked for.
        /// </summary>
        private Lazy<POICollection<ParkingSpace_Id, ParkingSpace>>          effectiveParkingSpaces;

        /// <summary>
        /// The last changes effective parking spaces have at least, as StampChangedEffective(…) found them: kept by the
        /// next version while they are the same.
        /// </summary>
        private Dictionary<ParkingSpace_Id, DateTimeOffset>                 effectiveFloors   = [];

        #endregion

        #region Properties

        /// <summary>
        /// The parking spaces as they are at runtime, with the properties their groups give them, in the order of their
        /// identifications: a parking space they give nothing is itself.
        /// </summary>
        public POICollection<ParkingSpace_Id, ParkingSpace> EffectiveParkingSpaces
            => effectiveParkingSpaces.Value;

        #endregion


        #region EffectiveParkingSpace       (ParkingSpace)

        /// <summary>
        /// The given parking space of this parking operator as it is at runtime, with the properties its groups give it:
        /// the parking space itself, when they give it none.
        /// </summary>
        /// <param name="ParkingSpace">A parking space of this parking operator.</param>
        public ParkingSpace EffectiveParkingSpace(ParkingSpace ParkingSpace)

            => effectiveParkingSpaceIndex.TryGetValue(ParkingSpace.Id, out var effectiveParkingSpace)
                   ? effectiveParkingSpace
                   : ParkingSpace;

        #endregion

        #region TryGetEffectiveParkingSpace (ParkingSpaceId, out ParkingSpace)

        /// <summary>
        /// Try to return the parking space with the given identification as it is at runtime.
        /// </summary>
        public Boolean TryGetEffectiveParkingSpace(ParkingSpace_Id                        ParkingSpaceId,
                                                   [NotNullWhen(true)] out ParkingSpace?  ParkingSpace)
        {

            if (!TryGetParkingSpace(ParkingSpaceId, out ParkingSpace))
                return false;

            ParkingSpace = EffectiveParkingSpace(ParkingSpace);
            return true;

        }

        #endregion

        #region ParkingSpaceGroupsOf        (ParkingSpaceId)

        /// <summary>
        /// The groups the given parking space is a member of, in the order of their identifications.
        /// </summary>
        public IEnumerable<ParkingSpaceGroup> ParkingSpaceGroupsOf(ParkingSpace_Id ParkingSpaceId)

            => ParkingSpaceGroups.Where(parkingSpaceGroup => parkingSpaceGroup.ParkingSpaceIds.Contains(ParkingSpaceId));

        #endregion


        #region (private) CalculateEffective(Changed = null, AtLeast = null)

        /// <summary>
        /// The effective parking spaces differing from the parking spaces, by their identifications; the given ones as well,
        /// with at least the given last change.
        /// </summary>
        private ImmutableDictionary<ParkingSpace_Id, ParkingSpace> CalculateEffective(Dictionary<ParkingSpace_Id, DateTimeOffset>? AtLeast = null)
        {

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

            var groupsOf    = ParkingSpaceGroups.Where(parkingSpaceGroup => parkingSpaceGroup.HasParkingSpaceProperties).
                                                 SelectMany(parkingSpaceGroup => parkingSpaceGroup.ParkingSpaceIds.Select(memberId => (MemberId: memberId, Group: parkingSpaceGroup))).
                                                 GroupBy   (membership => membership.MemberId).
                                                 ToDictionary(members => members.Key,
                                                              members => members.Select(membership => membership.Group).ToArray());

            var effective   = ImmutableDictionary.CreateBuilder<ParkingSpace_Id, ParkingSpace>();

            foreach (var parkingSpace in ParkingSpaces)
            {

                var hasGroups  = groupsOf.TryGetValue(parkingSpace.Id, out var groups);
                DateTimeOffset? atLeast = AtLeast?.TryGetValue(parkingSpace.Id, out var floor) == true ? floor : null;

                if (!hasGroups && !atLeast.HasValue)
                    continue;

                effective.Add(parkingSpace.Id,
                              Effective(parkingSpace,
                                        new POIGroupValues<ParkingSpaceGroup>($"the parking space '{parkingSpace.Id}'", groups ?? [], JSONOf),
                                        atLeast));

            }

            return effective.ToImmutable();

        }

        #endregion

        #region (private) StampChangedEffective(Previous)

        /// <summary>
        /// Calculate the effective parking spaces again where they are other than in the given previous version of this
        /// parking operator, with at least its last change: so a group that stops giving a parking space something, or a
        /// parking space that leaves a group, changes its last change as well, not only its content; and where they are the
        /// same, with at least the last change they had in it, e.g. after an earlier change of the same change set.
        /// </summary>
        private void StampChangedEffective(ParkingOperator Previous)
        {

            static JObject WithoutLastChange(JObject JSON)
            {
                JSON.Remove("lastChange");
                return JSON;
            }

            // Only where something may be other: the members of groups that changed, added or removed, and those having a
            // floor before. A parking space whose own representation changed has the last change of the change set anyway.
            var candidates = new HashSet<ParkingSpace_Id>(Previous.effectiveFloors.Keys);

            foreach (var parkingSpaceGroup in ParkingSpaceGroups)
            {

                var before = Previous.TryGetParkingSpaceGroup(parkingSpaceGroup.Id, out var previous) ? previous : null;

                if (before is not null && JToken.DeepEquals(parkingSpaceGroup.ToJSON(), before.ToJSON()))
                    continue;

                candidates.UnionWith(parkingSpaceGroup.ParkingSpaceIds);

                if (before is not null)
                    candidates.UnionWith(before.ParkingSpaceIds);

            }

            foreach (var before in Previous.ParkingSpaceGroups)
                if (!TryGetParkingSpaceGroup(before.Id, out _))
                    candidates.UnionWith(before.ParkingSpaceIds);

            // Other than before: at least the last change of the parking operator; the same: at least the one it had.
            var floors = new Dictionary<ParkingSpace_Id, DateTimeOffset>();

            foreach (var parkingSpaceId in candidates)
            {

                if (!TryGetParkingSpace(parkingSpaceId, out var parkingSpace) || !Previous.TryGetParkingSpace(parkingSpaceId, out var previousParkingSpace))
                    continue;

                var current  = EffectiveParkingSpace(parkingSpace);
                var before   = Previous.EffectiveParkingSpace(previousParkingSpace);

                if (!JToken.DeepEquals(WithoutLastChange(current.ToJSON()), WithoutLastChange(before.ToJSON())))
                    floors.Add(parkingSpaceId, LastChangeDate);

                else if (current.LastChangeDate < before.LastChangeDate)
                    floors.Add(parkingSpaceId, before.LastChangeDate);

            }

            if (floors.Count == 0)
                return;

            effectiveFloors             = floors;
            effectiveParkingSpaceIndex  = CalculateEffective(floors);
            effectiveParkingSpaces      = new (CalculateEffectiveParkingSpaces);

        }

        #endregion

        #region (private) CalculateEffectiveParkingSpaces()

        /// <summary>
        /// The parking spaces, the effective ones instead of those differing.
        /// </summary>
        private POICollection<ParkingSpace_Id, ParkingSpace> CalculateEffectiveParkingSpaces()

            => effectiveParkingSpaceIndex.IsEmpty
                   ? ParkingSpaces
                   : new (ParkingSpaces.Select(EffectiveParkingSpace), parkingSpace => parkingSpace.Id, "parking space", $"the parking operator '{Id}'");

        #endregion

        #region (private) ShareRuntimeOfEffective()

        /// <summary>
        /// Let the effective parking spaces take the runtime status of the parking spaces again, after these took the one
        /// of their previous versions.
        /// </summary>
        private void ShareRuntimeOfEffective()
        {
            foreach (var (parkingSpaceId, effectiveParkingSpace) in effectiveParkingSpaceIndex)
                if (TryGetParkingSpace(parkingSpaceId, out var parkingSpace))
                    effectiveParkingSpace.ShareRuntimeOf(parkingSpace);
        }

        #endregion


        #region (private static) Effective(ParkingSpace, Groups, AtLeast)

        /// <summary>
        /// The given parking space with the properties of the given groups.
        /// </summary>
        private static ParkingSpace Effective(ParkingSpace                         ParkingSpace,
                                              POIGroupValues<ParkingSpaceGroup>    Groups,
                                              DateTimeOffset?                      AtLeast)
        {

            try
            {

                var lastChange = Groups.LastChange(ParkingSpace.LastChangeDate);

                var effective  = new ParkingSpace(

                                     ParkingSpace.Id,
                                     ParkingSpace.Name,
                                     ParkingSpace.Description,

                                     GeoLocation:               Groups.Single("geoLocation",               ParkingSpace.GeoLocation,               group => group.GeoLocation),
                                     ParkingGarageId:           Groups.Single("parkingGarageId",           ParkingSpace.ParkingGarageId,           group => group.ParkingGarageId),
                                     PhysicalReference:         Groups.Single("physicalReference",         ParkingSpace.PhysicalReference,         group => group.PhysicalReference),
                                     MaxVehicleWeight:          Groups.Single("maxVehicleWeight",          ParkingSpace.MaxVehicleWeight,          group => group.MaxVehicleWeight),
                                     MaxVehicleHeight:          Groups.Single("maxVehicleHeight",          ParkingSpace.MaxVehicleHeight,          group => group.MaxVehicleHeight),
                                     MaxVehicleLength:          Groups.Single("maxVehicleLength",          ParkingSpace.MaxVehicleLength,          group => group.MaxVehicleLength),
                                     MaxVehicleWidth:           Groups.Single("maxVehicleWidth",           ParkingSpace.MaxVehicleWidth,           group => group.MaxVehicleWidth),
                                     Length:                    Groups.Single("length",                    ParkingSpace.Length,                    group => group.Length),
                                     Width:                     Groups.Single("width",                     ParkingSpace.Width,                     group => group.Width),
                                     EVSEPosition:              Groups.Single("evsePosition",              ParkingSpace.EVSEPosition,              group => group.EVSEPosition),
                                     Direction:                 Groups.Single("direction",                 ParkingSpace.Direction,                 group => group.Direction),
                                     Surface:                   Groups.Single("surface",                   ParkingSpace.Surface,                   group => group.Surface),
                                     Slope:                     Groups.Single("slope",                     ParkingSpace.Slope,                     group => group.Slope),
                                     TimeLimit:                 Groups.Single("timeLimit",                 ParkingSpace.TimeLimit,                 group => group.TimeLimit),
                                     RestrictedToVehicleTypes:  Groups.Single("restrictedToVehicleTypes",  ParkingSpace.RestrictedToVehicleTypes,  group => group.RestrictedToVehicleTypes),
                                     DriveThrough:              Groups.Single("driveThrough",              ParkingSpace.DriveThrough,              group => group.DriveThrough),
                                     ReservationRequired:       Groups.Single("reservationRequired",       ParkingSpace.ReservationRequired,       group => group.ReservationRequired),
                                     Roofed:                    Groups.Single("roofed",                    ParkingSpace.Roofed,                    group => group.Roofed),
                                     Illuminated:               Groups.Single("illuminated",               ParkingSpace.Illuminated,               group => group.Illuminated),
                                     RefrigerationOutlet:       Groups.Single("refrigerationOutlet",       ParkingSpace.RefrigerationOutlet,       group => group.RefrigerationOutlet),
                                     DangerousGoodsAllowed:     Groups.Single("dangerousGoodsAllowed",     ParkingSpace.DangerousGoodsAllowed,     group => group.DangerousGoodsAllowed),
                                     ProtectedArea:             Groups.Single("protectedArea",             ParkingSpace.ProtectedArea,             group => group.ProtectedArea),

                                     ParkingSensorIds:          Groups.Union (ParkingSpace.ParkingSensorIds,     group => group.ParkingSensorIds),
                                     ChargingStationIds:        Groups.Union (ParkingSpace.ChargingStationIds,   group => group.ChargingStationIds),
                                     ParkingProductIds:         Groups.Union (ParkingSpace.ParkingProductIds,    group => group.ParkingProductIds),
                                     ParkingRestrictions:       Groups.Union (ParkingSpace.ParkingRestrictions,  group => group.ParkingRestrictions),
                                     EUVehicleCategories:       Groups.Union (ParkingSpace.EUVehicleCategories,  group => group.EUVehicleCategories),
                                     VehicleTypes:              Groups.Union (ParkingSpace.VehicleTypes,         group => group.VehicleTypes),
                                     Standards:                 Groups.Union (ParkingSpace.Standards,            group => group.Standards),
                                     Images:                    Groups.Union ("image", ParkingSpace.Images, group => group.Images, image => image.URL.ToString(), image => image.ToJSON()),

                                     // Its own identification in another system.
                                     APDSReference:             ParkingSpace.APDSReference,

                                     DataSource:                ParkingSpace.DataSource,
                                     Created:                   ParkingSpace.Created,
                                     LastChange:                AtLeast > lastChange ? AtLeast : lastChange,
                                     CustomData:                ParkingSpace.CustomData

                                 );

                effective.ShareRuntimeOf(ParkingSpace);

                return effective;

            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"The parking space '{ParkingSpace.Id}' {Groups.With} is invalid: {e.Message}", e);
            }

        }

        #endregion

    }

}
