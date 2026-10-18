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

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A template of parking spaces of one parking operator, embedded completely in it: what the parking spaces naming it
    /// in their templateIds share, e.g. the garage, the dimensions and the roof of all spaces of a level, and the parking
    /// products offered at them. The spaces take it at runtime, as the effective parking spaces of their parking operator: a
    /// value of their own wins, else the one of the first of their templates giving one; arrays are united.
    /// </summary>
    public sealed class ParkingSpaceTemplate : APOITemplate<ParkingSpaceTemplate_Id>,
                                               IEquatable<ParkingSpaceTemplate>,
                                               IComparable<ParkingSpaceTemplate>
    {

        #region Properties

        /// <summary>
        /// The parking products of its parking operator offered at its members, besides their own, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ParkingProduct_Id>  ParkingProductIds  { get; }

        /// <summary>
        /// The geographical location of its members, unless they have their own.
        /// </summary>
        public GeoCoordinate?                           GeoLocation               { get; }

        /// <summary>
        /// The parking garage of its parking operator its members are in, unless they name their own.
        /// </summary>
        public ParkingGarage_Id?                        ParkingGarageId           { get; }

        /// <summary>
        /// Parking sensors of its parking operator observing its members, besides their own, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ParkingSensor_Id>         ParkingSensorIds          { get; }

        /// <summary>
        /// Charging stations of the roaming network its members are for, besides their own, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ChargingStation_Id>       ChargingStationIds        { get; }

        /// <summary>
        /// Parking restrictions of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<ParkingRestrictionGroup>  ParkingRestrictions       { get; }

        /// <summary>
        /// EU vehicle categories its members are for, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<EUVehicleCategory>        EUVehicleCategories       { get; }

        /// <summary>
        /// The reference printed on its members, unless they have their own.
        /// </summary>
        public String?                                  PhysicalReference         { get; }

        /// <summary>
        /// Types of vehicles its members are for, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<VehicleType>              VehicleTypes              { get; }

        /// <summary>
        /// The maximum vehicle weight of its members, unless they have their own.
        /// </summary>
        public Kilogram?                                MaxVehicleWeight          { get; }

        /// <summary>
        /// The maximum vehicle height of its members, unless they have their own.
        /// </summary>
        public Meter?                                   MaxVehicleHeight          { get; }

        /// <summary>
        /// The maximum vehicle length of its members, unless they have their own.
        /// </summary>
        public Meter?                                   MaxVehicleLength          { get; }

        /// <summary>
        /// The maximum vehicle width of its members, unless they have their own.
        /// </summary>
        public Meter?                                   MaxVehicleWidth           { get; }

        /// <summary>
        /// The length of its members, unless they have their own.
        /// </summary>
        public Meter?                                   Length                    { get; }

        /// <summary>
        /// The width of its members, unless they have their own.
        /// </summary>
        public Meter?                                   Width                     { get; }

        /// <summary>
        /// Where the EVSE is at its members, unless they tell it themselves.
        /// </summary>
        public EVSEPosition?                            EVSEPosition              { get; }

        /// <summary>
        /// How vehicles park at its members, unless they tell it themselves.
        /// </summary>
        public ParkingDirection?                        Direction                 { get; }

        /// <summary>
        /// The surface of its members, unless they have their own.
        /// </summary>
        public ParkingSurface?                          Surface                   { get; }

        /// <summary>
        /// The slope of its members, unless they have their own.
        /// </summary>
        public ParkingSlope?                            Slope                     { get; }

        /// <summary>
        /// The time limit of its members, unless they have their own.
        /// </summary>
        public TimeSpan?                                TimeLimit                 { get; }

        /// <summary>
        /// Whether its members are restricted to their vehicle types, unless they tell it themselves.
        /// </summary>
        public Boolean?                                 RestrictedToVehicleTypes  { get; }

        /// <summary>
        /// Whether its members are drive-through, unless they tell it themselves.
        /// </summary>
        public Boolean?                                 DriveThrough              { get; }

        /// <summary>
        /// Whether its members require a reservation, unless they tell it themselves.
        /// </summary>
        public Boolean?                                 ReservationRequired       { get; }

        /// <summary>
        /// Whether its members are roofed, unless they tell it themselves.
        /// </summary>
        public Boolean?                                 Roofed                    { get; }

        /// <summary>
        /// Whether its members are illuminated, unless they tell it themselves.
        /// </summary>
        public Boolean?                                 Illuminated               { get; }

        /// <summary>
        /// Whether its members have a refrigeration outlet, unless they tell it themselves.
        /// </summary>
        public Boolean?                                 RefrigerationOutlet       { get; }

        /// <summary>
        /// Whether dangerous goods are allowed at its members, unless they tell it themselves.
        /// </summary>
        public Boolean?                                 DangerousGoodsAllowed     { get; }

        /// <summary>
        /// Whether its members are in a protected area, unless they tell it themselves.
        /// </summary>
        public Boolean?                                 ProtectedArea             { get; }

        /// <summary>
        /// Standards of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<Standard>                 Standards                 { get; }

        /// <summary>
        /// Images of its members, besides their own, in their given order.
        /// </summary>
        public ImmutableArray<Image>                    Images                    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking space template.
        /// </summary>
        public ParkingSpaceTemplate(ParkingSpaceTemplate_Id                             Id,
                                 I18NString?                                      Name                         = null,
                                 I18NString?                                      Description                  = null,
                                 IEnumerable<ParkingProduct_Id>?                  ParkingProductIds            = null,

                                 GeoCoordinate?                                   GeoLocation                  = null,
                                 ParkingGarage_Id?                                ParkingGarageId              = null,
                                 IEnumerable<ParkingSensor_Id>?                   ParkingSensorIds             = null,
                                 IEnumerable<ChargingStation_Id>?                 ChargingStationIds           = null,
                                 IEnumerable<ParkingRestrictionGroup>?            ParkingRestrictions          = null,
                                 IEnumerable<EUVehicleCategory>?                  EUVehicleCategories          = null,
                                 String?                                          PhysicalReference            = null,
                                 IEnumerable<VehicleType>?                        VehicleTypes                 = null,
                                 Kilogram?                                        MaxVehicleWeight             = null,
                                 Meter?                                           MaxVehicleHeight             = null,
                                 Meter?                                           MaxVehicleLength             = null,
                                 Meter?                                           MaxVehicleWidth              = null,
                                 Meter?                                           Length                       = null,
                                 Meter?                                           Width                        = null,
                                 EVSEPosition?                                    EVSEPosition                 = null,
                                 ParkingDirection?                                Direction                    = null,
                                 ParkingSurface?                                  Surface                      = null,
                                 ParkingSlope?                                    Slope                        = null,
                                 TimeSpan?                                        TimeLimit                    = null,
                                 Boolean?                                         RestrictedToVehicleTypes     = null,
                                 Boolean?                                         DriveThrough                 = null,
                                 Boolean?                                         ReservationRequired          = null,
                                 Boolean?                                         Roofed                       = null,
                                 Boolean?                                         Illuminated                  = null,
                                 Boolean?                                         RefrigerationOutlet          = null,
                                 Boolean?                                         DangerousGoodsAllowed        = null,
                                 Boolean?                                         ProtectedArea                = null,
                                 IEnumerable<Standard>?                           Standards                    = null,
                                 IEnumerable<Image>?                              Images                       = null,

                                 String?                                          DataSource                   = null,
                                 DateTimeOffset?                                  Created                      = null,
                                 DateTimeOffset?                                  LastChange                   = null,

                                 CustomDataNew?                                   CustomData                   = null,
                                 UserDefinedDictionary?                           InternalData                 = null)

            : base(Id,
                   Name,
                   Description,
                   DataSource,
                   Created,
                   LastChange,
                   CustomData,
                   InternalData)

        {

            if (MaxVehicleWeight?.Value <= 0 || MaxVehicleHeight?.m <= 0 || MaxVehicleLength?.m <= 0 || MaxVehicleWidth?.m <= 0 || Length?.m <= 0 || Width?.m <= 0)
                throw new ArgumentException($"The vehicle limits and dimensions of the parking space template '{Id}' must be positive!");

            if (TimeLimit < TimeSpan.Zero)
                throw new ArgumentException($"The time limit of the parking space template '{Id}' must not be negative!", nameof(TimeLimit));
            this.ParkingProductIds         = POIJSON.OrderedIds(ParkingProductIds,   $"parking product of the parking space template '{Id}'");

            this.GeoLocation               = GeoLocation;
            this.ParkingGarageId           = ParkingGarageId;
            this.ParkingSensorIds          = POIJSON.OrderedIds(ParkingSensorIds,    $"parking sensor of the parking space template '{Id}'");
            this.ChargingStationIds        = POIJSON.OrderedIds(ChargingStationIds,  $"charging station of the parking space template '{Id}'");
            this.ParkingRestrictions       = POIJSON.OrderedIds(ParkingRestrictions, $"parking restriction of the parking space template '{Id}'");
            this.EUVehicleCategories       = POIJSON.OrderedIds(EUVehicleCategories, $"EU vehicle category of the parking space template '{Id}'");
            this.PhysicalReference         = PhysicalReference;
            this.VehicleTypes              = POIJSON.OrderedIds(VehicleTypes,        $"vehicle type of the parking space template '{Id}'");
            this.MaxVehicleWeight          = MaxVehicleWeight;
            this.MaxVehicleHeight          = MaxVehicleHeight;
            this.MaxVehicleLength          = MaxVehicleLength;
            this.MaxVehicleWidth           = MaxVehicleWidth;
            this.Length                    = Length;
            this.Width                     = Width;
            this.EVSEPosition              = EVSEPosition;
            this.Direction                 = Direction;
            this.Surface                   = Surface;
            this.Slope                     = Slope;
            this.TimeLimit                 = TimeLimit;
            this.RestrictedToVehicleTypes  = RestrictedToVehicleTypes;
            this.DriveThrough              = DriveThrough;
            this.ReservationRequired       = ReservationRequired;
            this.Roofed                    = Roofed;
            this.Illuminated               = Illuminated;
            this.RefrigerationOutlet       = RefrigerationOutlet;
            this.DangerousGoodsAllowed     = DangerousGoodsAllowed;
            this.ProtectedArea             = ProtectedArea;
            this.Standards                 = POIJSON.OrderedIds(Standards,           $"standard of the parking space template '{Id}'");
            this.Images                    = POIJSON.Distinct  (Images, image => image.URL.ToString(), $"image of the parking space template '{Id}'");

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this parking space template, as its parking operator embeds it.
        /// </summary>
        public override JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",         Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",  Description.ToJSON());

            if (DataSource is not null)
                json.Add("dataSource",   DataSource);

            if (ParkingProductIds.Length > 0)
                json.Add("parkingProductIds",  POIJSON.IdsToJSON(ParkingProductIds));

            // The properties of a parking space, as a parking space writes them.
            if (GeoLocation.HasValue)
                json.Add("geoLocation",               GeoLocation.Value.ToJSON(Embedded: true));

            if (ParkingGarageId.HasValue)
                json.Add("parkingGarageId",           ParkingGarageId.Value.ToString());

            if (ParkingSensorIds.Length > 0)
                json.Add("parkingSensorIds",          POIJSON.IdsToJSON(ParkingSensorIds));

            if (ChargingStationIds.Length > 0)
                json.Add("chargingStationIds",        POIJSON.IdsToJSON(ChargingStationIds));

            if (ParkingRestrictions.Length > 0)
                json.Add("parkingRestrictions",       POIJSON.IdsToJSON(ParkingRestrictions));

            if (EUVehicleCategories.Length > 0)
                json.Add("euVehicleCategories",       POIJSON.IdsToJSON(EUVehicleCategories));

            if (PhysicalReference is not null)
                json.Add("physicalReference",         PhysicalReference);

            if (VehicleTypes.Length > 0)
                json.Add("vehicleTypes",              POIJSON.IdsToJSON(VehicleTypes));

            if (MaxVehicleWeight.HasValue)
                json.Add("maxVehicleWeight",          POIJSON.Number(MaxVehicleWeight.Value.Value));

            if (MaxVehicleHeight.HasValue)
                json.Add("maxVehicleHeight",          POIJSON.Number(MaxVehicleHeight.Value.m));

            if (MaxVehicleLength.HasValue)
                json.Add("maxVehicleLength",          POIJSON.Number(MaxVehicleLength.Value.m));

            if (MaxVehicleWidth.HasValue)
                json.Add("maxVehicleWidth",           POIJSON.Number(MaxVehicleWidth.Value.m));

            if (Length.HasValue)
                json.Add("length",                    POIJSON.Number(Length.Value.m));

            if (Width.HasValue)
                json.Add("width",                     POIJSON.Number(Width.Value.m));

            if (EVSEPosition.HasValue)
                json.Add("evsePosition",              EVSEPosition.Value.ToString());

            if (Direction.HasValue)
                json.Add("direction",                 Direction.Value.ToString());

            if (Surface.HasValue)
                json.Add("surface",                   Surface.Value.ToString());

            if (Slope.HasValue)
                json.Add("slope",                     Slope.Value.ToString());

            if (TimeLimit.HasValue)
                json.Add("timeLimit",                 POIJSON.Seconds(TimeLimit.Value));

            if (RestrictedToVehicleTypes.HasValue)
                json.Add("restrictedToVehicleTypes",  RestrictedToVehicleTypes.Value);

            if (DriveThrough.HasValue)
                json.Add("driveThrough",              DriveThrough.Value);

            if (ReservationRequired.HasValue)
                json.Add("reservationRequired",       ReservationRequired.Value);

            if (Roofed.HasValue)
                json.Add("roofed",                    Roofed.Value);

            if (Illuminated.HasValue)
                json.Add("illuminated",               Illuminated.Value);

            if (RefrigerationOutlet.HasValue)
                json.Add("refrigerationOutlet",       RefrigerationOutlet.Value);

            if (DangerousGoodsAllowed.HasValue)
                json.Add("dangerousGoodsAllowed",     DangerousGoodsAllowed.Value);

            if (ProtectedArea.HasValue)
                json.Add("protectedArea",             ProtectedArea.Value);

            if (Standards.Length > 0)
                json.Add("standards",                 POIJSON.IdsToJSON(Standards));

            if (Images.Length > 0)
                json.Add("images",                    new JArray(Images.Select(image => image.ToJSON())));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out ParkingSpaceTemplate, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a parking space template.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ParkingSpaceTemplate">The parsed parking space template.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out ParkingSpaceTemplate?   ParkingSpaceTemplate,
                                       [NotNullWhen(false)] out String?              ErrorResponse)
        {

            ParkingSpaceTemplate = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "parking space template identification",
                                         ParkingSpaceTemplate_Id.TryParse,
                                         out ParkingSpaceTemplate_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, "name",         "parking space template name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, "description",  "parking space template description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "dataSource",   "data source",                    out var dataSource,   out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseIds(JSON, "parkingProductIds", "parking product identification", ParkingProduct_Id.TryParse, out ImmutableArray<ParkingProduct_Id> parkingProductIds, out ErrorResponse))
                    return false;

                // The properties of a parking space, as a parking space reads them.
                if (!POIJSON.TryParseGeoLocation(JSON,                                                             out var geoLocation,  out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, "parkingGarageId",     "parking garage identification",     ParkingGarage_Id.  TryParse, out ParkingGarage_Id? parkingGarageId,   out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "parkingSensorIds",    "parking sensor identification",     ParkingSensor_Id.  TryParse, out ImmutableArray<ParkingSensor_Id>   parkingSensorIds,   out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "chargingStationIds",  "charging station identification",   ChargingStation_Id.TryParse, out ImmutableArray<ChargingStation_Id> chargingStationIds, out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "parkingRestrictions", "parking restriction",               ParkingRestrictionGroup.TryParse, out ImmutableArray<ParkingRestrictionGroup> parkingRestrictions,  out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "euVehicleCategories", "EU vehicle category",               EUVehicleCategory.      TryParse, out ImmutableArray<EUVehicleCategory>       euVehicleCategories,  out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, "physicalReference",   "physical reference",                out var physicalReference,  out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "vehicleTypes",        "vehicle type",  VehicleType.TryParse, out ImmutableArray<VehicleType> vehicleTypes, out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "standards",           "standard",      Standard.   TryParse, out ImmutableArray<Standard>    standards,    out ErrorResponse) ||
                    !POIJSON.TryParseObjects<Image>(JSON, "images",           "images",        Image.      TryParse, out var images,                               out ErrorResponse) ||
                    !POIJSON.TryParseSeconds    (JSON, "timeLimit",           "time limit",                        out var timeLimit,          out ErrorResponse) ||
                    !POIJSON.TryParseNonNegative(JSON, "maxVehicleWeight",    "maximum vehicle weight",            out var maxVehicleWeight,   out ErrorResponse))
                {
                    return false;
                }

                var meters = new Dictionary<String, Decimal?>();

                foreach (var key in new[] { "maxVehicleHeight", "maxVehicleLength", "maxVehicleWidth", "length", "width" })
                {
                    if (!POIJSON.TryParseNonNegative(JSON, key, key, out var meter, out ErrorResponse))
                        return false;
                    meters[key] = meter;
                }

                if (!POIJSON.TryParseEnum(JSON, "evsePosition", "evsePosition", out EVSEPosition?     eVSEPosition, out ErrorResponse) ||
                    !POIJSON.TryParseEnum(JSON, "direction",    "direction",    out ParkingDirection? direction,    out ErrorResponse) ||
                    !POIJSON.TryParseEnum(JSON, "surface",      "surface",      out ParkingSurface?   surface,      out ErrorResponse) ||
                    !POIJSON.TryParseEnum(JSON, "slope",        "slope",        out ParkingSlope?     slope,        out ErrorResponse))
                {
                    return false;
                }

                var flags = new Dictionary<String, Boolean?>();

                foreach (var key in new[] { "restrictedToVehicleTypes", "driveThrough", "reservationRequired", "roofed", "illuminated", "refrigerationOutlet", "dangerousGoodsAllowed", "protectedArea" })
                {
                    flags[key] = null;
                    if (JSON.ParseOptional(key, key, out Boolean? flag, out ErrorResponse))
                    {
                        if (ErrorResponse is not null)
                            return false;
                        flags[key] = flag;
                    }
                }

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                ParkingSpaceTemplate = new ParkingSpaceTemplate(id,
                                                          name,
                                                          description,
                                                          parkingProductIds,
                                                          geoLocation,
                                                          parkingGarageId,
                                                          parkingSensorIds,
                                                          chargingStationIds,
                                                          parkingRestrictions,
                                                          euVehicleCategories,
                                                          physicalReference,
                                                          vehicleTypes,
                                                          maxVehicleWeight.HasValue ? Kilogram.FromKG(maxVehicleWeight.Value) : null,
                                                          meters["maxVehicleHeight"].HasValue ? Meter.From_m(meters["maxVehicleHeight"]!.Value) : null,
                                                          meters["maxVehicleLength"].HasValue ? Meter.From_m(meters["maxVehicleLength"]!.Value) : null,
                                                          meters["maxVehicleWidth"]. HasValue ? Meter.From_m(meters["maxVehicleWidth"]!. Value) : null,
                                                          meters["length"].          HasValue ? Meter.From_m(meters["length"]!.          Value) : null,
                                                          meters["width"].           HasValue ? Meter.From_m(meters["width"]!.           Value) : null,
                                                          eVSEPosition,
                                                          direction,
                                                          surface,
                                                          slope,
                                                          timeLimit,
                                                          flags["restrictedToVehicleTypes"],
                                                          flags["driveThrough"],
                                                          flags["reservationRequired"],
                                                          flags["roofed"],
                                                          flags["illuminated"],
                                                          flags["refrigerationOutlet"],
                                                          flags["dangerousGoodsAllowed"],
                                                          flags["protectedArea"],
                                                          standards,
                                                          images,
                                                          DataSource:  dataSource,
                                                          Created:     created,
                                                          LastChange:  lastChange,
                                                          CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ParkingSpaceTemplate  = null;
                ErrorResponse      = "The given JSON representation of a parking space template is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two parking space templates for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ParkingSpaceTemplate? ParkingSpaceTemplate1, ParkingSpaceTemplate? ParkingSpaceTemplate2)

            => ReferenceEquals(ParkingSpaceTemplate1, ParkingSpaceTemplate2) ||
               (ParkingSpaceTemplate1 is not null && ParkingSpaceTemplate1.Equals(ParkingSpaceTemplate2));

        /// <summary>
        /// Compares two parking space templates for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ParkingSpaceTemplate? ParkingSpaceTemplate1, ParkingSpaceTemplate? ParkingSpaceTemplate2)

            => !(ParkingSpaceTemplate1 == ParkingSpaceTemplate2);

        /// <inheritdoc/>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingSpaceTemplate parkingSpaceTemplate
                   ? CompareTo(parkingSpaceTemplate)
                   : throw new ArgumentException("The given object is not a parking space template!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ParkingSpaceTemplate? ParkingSpaceTemplate)

            => ParkingSpaceTemplate is null
                   ? throw new ArgumentNullException(nameof(ParkingSpaceTemplate), "The given parking space template must not be null!")
                   : Id.CompareTo(ParkingSpaceTemplate.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSpaceTemplate parkingSpaceTemplate &&
               Equals(parkingSpaceTemplate);

        /// <inheritdoc/>
        public Boolean Equals(ParkingSpaceTemplate? ParkingSpaceTemplate)

            => ParkingSpaceTemplate is not null &&
               Id.Equals(ParkingSpaceTemplate.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
