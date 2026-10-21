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
    /// the parking space templates they name give them.
    /// </summary>
    /// <remarks>
    /// As the effective members of a charging station operator (see ChargingStationOperator.EffectiveChargingPools):
    /// a single value is the parking space's own, else the one of the first of its templates giving one; an array is its
    /// own united with those of all its templates, of images of the same URL its own, else the one of the first template
    /// giving it. Its templates must not make it invalid. An effective parking space has the runtime status of the parking
    /// space; its last change is the latest of it and its templates, and, where it is other than in the version before,
    /// not before the last change of the parking operator.
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
        /// The parking spaces as they are at runtime, with the properties their templates give them, in the order of their
        /// identifications: a parking space naming no templates is itself.
        /// </summary>
        public POICollection<ParkingSpace_Id, ParkingSpace> EffectiveParkingSpaces
            => effectiveParkingSpaces.Value;

        #endregion


        #region EffectiveParkingSpace       (ParkingSpace)

        /// <summary>
        /// The given parking space of this parking operator as it is at runtime, with the properties its templates give it:
        /// the parking space itself, when it names none.
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

        #region (internal) EffectiveParkingSpacesOfTheContent()

        /// <summary>
        /// The effective parking spaces differing from the parking spaces, by their identifications, as the effective content
        /// of a version has them (SPECIFICATION.md, section 8.1): their last changes without the floors kept at runtime.
        /// </summary>
        internal IReadOnlyDictionary<ParkingSpace_Id, ParkingSpace> EffectiveParkingSpacesOfTheContent()

            => effectiveFloors.Count == 0
                   ? effectiveParkingSpaceIndex
                   : CalculateEffective();

        #endregion

        #region TemplatesOf                 (ParkingSpace)

        /// <summary>
        /// The templates the given parking space names, in its order.
        /// </summary>
        public IEnumerable<ParkingSpaceTemplate> TemplatesOf(ParkingSpace ParkingSpace)

            => ChargingStationOperator.TemplatesOf(ParkingSpace.TemplateIds, ParkingSpaceTemplates);

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

            var effective = ImmutableDictionary.CreateBuilder<ParkingSpace_Id, ParkingSpace>();

            foreach (var parkingSpace in ParkingSpaces)
            {

                DateTimeOffset? atLeast = AtLeast?.TryGetValue(parkingSpace.Id, out var floor) == true ? floor : null;

                if (parkingSpace.TemplateIds.Length == 0 && !atLeast.HasValue)
                    continue;

                effective.Add(parkingSpace.Id,
                              Effective(parkingSpace,
                                        ChargingStationOperator.Values(parkingSpace.TemplateIds, ParkingSpaceTemplates),
                                        atLeast));

            }

            return effective.ToImmutable();

        }

        #endregion

        #region (private) StampChangedEffective(Previous)

        /// <summary>
        /// Calculate the effective parking spaces again where they are other than in the given previous version of this
        /// parking operator, with at least its last change: so a template that stops giving a parking space something
        /// changes its last change as well, not only its content; and where they are the
        /// same, with at least the last change they had in it, e.g. after an earlier change of the same change set.
        /// Floors found before are kept, the later one of two: a merge compares with both of its parents, one after the
        /// other.
        /// </summary>
        private void StampChangedEffective(ParkingOperator Previous)
        {

            static JObject WithoutLastChange(JObject JSON)
            {
                JSON.Remove(BasePropertyKeys.LastChange);
                return JSON;
            }

            // Only where something may be other: the parking spaces naming templates that changed, added or removed, and those
            // having a floor before. A parking space whose own representation changed has the last change of the change set
            // anyway.
            var candidates        = new HashSet<ParkingSpace_Id>(Previous.effectiveFloors.Keys);
            var changedTemplates  = ChargingStationOperator.ChangedTemplates(ParkingSpaceTemplates, Previous.ParkingSpaceTemplates, template => template.Id);

            foreach (var parkingSpace in ParkingSpaces)
                if (parkingSpace.TemplateIds.Any(changedTemplates.Contains))
                    candidates.Add(parkingSpace.Id);

            // Other than before: at least the last change of the parking operator; the same: at least the one it had.
            // The floors found before, e.g. comparing a merge with its other parent, are kept.
            var floors  = new Dictionary<ParkingSpace_Id, DateTimeOffset>(effectiveFloors);
            var found   = false;

            foreach (var parkingSpaceId in candidates)
            {

                if (!TryGetParkingSpace(parkingSpaceId, out var parkingSpace) || !Previous.TryGetParkingSpace(parkingSpaceId, out var previousParkingSpace))
                    continue;

                var current  = EffectiveParkingSpace(parkingSpace);
                var before   = Previous.EffectiveParkingSpace(previousParkingSpace);

                if (!POIJSON.SameValue(WithoutLastChange(current.ToJSON(Embedded: true)), WithoutLastChange(before.ToJSON(Embedded: true))))
                    found = ChargingStationOperator.Raise(floors, parkingSpaceId, LastChangeDate) | found;

                else if (current.LastChangeDate < before.LastChangeDate)
                    found = ChargingStationOperator.Raise(floors, parkingSpaceId, before.LastChangeDate) | found;

            }

            if (!found)
                return;

            effectiveFloors             = floors;
            effectiveParkingSpaceIndex  = CalculateEffective(floors);
            effectiveParkingSpaces      = new (CalculateEffectiveParkingSpaces);

        }

        #endregion

        #region (internal) EffectiveFloors()

        /// <summary>
        /// The last changes its effective parking spaces have at least, beyond what they and their templates tell: what a
        /// snapshot of a version keeps.
        /// </summary>
        internal IEnumerable<POIEffectiveLastChange> EffectiveFloors()

            => effectiveFloors.Select(floor => new POIEffectiveLastChange(nameof(ParkingSpace), floor.Key.ToString(), floor.Value));

        #endregion

        #region (internal) TakeEffectiveFloors(Floors)

        /// <summary>
        /// Take the given floors of the last changes of effective parking spaces, e.g. of a snapshot, and calculate the
        /// effective parking spaces again with them. Floors of parking spaces it does not have are passed over; of two the
        /// later one is kept.
        /// </summary>
        /// <param name="Floors">Floors, also of other operators and of other kinds of members.</param>
        internal void TakeEffectiveFloors(IEnumerable<POIEffectiveLastChange> Floors)
        {

            var floors  = new Dictionary<ParkingSpace_Id, DateTimeOffset>(effectiveFloors);
            var found   = false;

            foreach (var floor in Floors)
                if (floor.EntityType == nameof(ParkingSpace) &&
                    ParkingSpace_Id.TryParse(floor.EntityId, out var parkingSpaceId) &&
                    TryGetParkingSpace(parkingSpaceId, out var parkingSpace))
                {
                    found = ChargingStationOperator.Raise(floors, parkingSpace.Id, floor.LastChange) | found;
                }

            if (!found)
                return;

            effectiveFloors             = floors;
            effectiveParkingSpaceIndex  = CalculateEffective(floors);
            effectiveParkingSpaces      = new (CalculateEffectiveParkingSpaces);

            ShareRuntimeOfEffective();

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


        #region (private static) Effective(ParkingSpace, Templates, AtLeast)

        /// <summary>
        /// The given parking space with the properties of the given templates, in the order of its templateIds.
        /// </summary>
        private static ParkingSpace Effective(ParkingSpace                         ParkingSpace,
                                              POITemplateValues<ParkingSpaceTemplate>  Templates,
                                              DateTimeOffset?                      AtLeast)
        {

            try
            {

                var lastChange = Templates.LastChange(ParkingSpace.LastChangeDate);

                var effective  = new ParkingSpace(

                                     ParkingSpace.Id,
                                     ParkingSpace.Name,
                                     ParkingSpace.Description,

                                     GeoLocation:               Templates.Single(ParkingSpace.GeoLocation,               template => template.GeoLocation),
                                     ParkingGarageId:           Templates.Single(ParkingSpace.ParkingGarageId,           template => template.ParkingGarageId),
                                     PhysicalReference:         Templates.Single(ParkingSpace.PhysicalReference,         template => template.PhysicalReference),
                                     MaxVehicleWeight:          Templates.Single(ParkingSpace.MaxVehicleWeight,          template => template.MaxVehicleWeight),
                                     MaxVehicleHeight:          Templates.Single(ParkingSpace.MaxVehicleHeight,          template => template.MaxVehicleHeight),
                                     MaxVehicleLength:          Templates.Single(ParkingSpace.MaxVehicleLength,          template => template.MaxVehicleLength),
                                     MaxVehicleWidth:           Templates.Single(ParkingSpace.MaxVehicleWidth,           template => template.MaxVehicleWidth),
                                     Length:                    Templates.Single(ParkingSpace.Length,                    template => template.Length),
                                     Width:                     Templates.Single(ParkingSpace.Width,                     template => template.Width),
                                     EVSEPosition:              Templates.Single(ParkingSpace.EVSEPosition,              template => template.EVSEPosition),
                                     Direction:                 Templates.Single(ParkingSpace.Direction,                 template => template.Direction),
                                     Surface:                   Templates.Single(ParkingSpace.Surface,                   template => template.Surface),
                                     Slope:                     Templates.Single(ParkingSpace.Slope,                     template => template.Slope),
                                     TimeLimit:                 Templates.Single(ParkingSpace.TimeLimit,                 template => template.TimeLimit),
                                     RestrictedToVehicleTypes:  Templates.Single(ParkingSpace.RestrictedToVehicleTypes,  template => template.RestrictedToVehicleTypes),
                                     DriveThrough:              Templates.Single(ParkingSpace.DriveThrough,              template => template.DriveThrough),
                                     ReservationRequired:       Templates.Single(ParkingSpace.ReservationRequired,       template => template.ReservationRequired),
                                     Roofed:                    Templates.Single(ParkingSpace.Roofed,                    template => template.Roofed),
                                     Illuminated:               Templates.Single(ParkingSpace.Illuminated,               template => template.Illuminated),
                                     RefrigerationOutlet:       Templates.Single(ParkingSpace.RefrigerationOutlet,       template => template.RefrigerationOutlet),
                                     DangerousGoodsAllowed:     Templates.Single(ParkingSpace.DangerousGoodsAllowed,     template => template.DangerousGoodsAllowed),
                                     ProtectedArea:             Templates.Single(ParkingSpace.ProtectedArea,             template => template.ProtectedArea),

                                     ParkingSensorIds:          Templates.Union (ParkingSpace.ParkingSensorIds,     template => template.ParkingSensorIds),
                                     ChargingStationIds:        Templates.Union (ParkingSpace.ChargingStationIds,   template => template.ChargingStationIds),
                                     ParkingProductIds:         Templates.Union (ParkingSpace.ParkingProductIds,    template => template.ParkingProductIds),
                                     ParkingRestrictions:       Templates.Union (ParkingSpace.ParkingRestrictions,  template => template.ParkingRestrictions),
                                     EUVehicleCategories:       Templates.Union (ParkingSpace.EUVehicleCategories,  template => template.EUVehicleCategories),
                                     VehicleTypes:              Templates.Union (ParkingSpace.VehicleTypes,         template => template.VehicleTypes),
                                     Standards:                 Templates.Union (ParkingSpace.Standards,            template => template.Standards),
                                     Images:                    Templates.Union (ParkingSpace.Images, template => template.Images, image => image.URL.ToString()),

                                     // Its own identification in another system, and the templates it names.
                                     APDSReference:             ParkingSpace.APDSReference,
                                     TemplateIds:               ParkingSpace.TemplateIds,

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
                throw new ArgumentException($"The parking space '{ParkingSpace.Id}' {Templates.With} is invalid: {e.Message}", e);
            }

        }

        #endregion

    }

}
