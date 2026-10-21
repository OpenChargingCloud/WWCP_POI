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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A template of charging stations of one charging station operator, embedded completely in it: what the charging
    /// stations naming it in their templateIds share, e.g. the model, the hotline and the features of all stations of a
    /// model, and a charging tariff for their EVSEs. The stations take it at runtime, as the effective charging stations of
    /// their operator: a value of their own wins, else the one of the first of their templates giving one; arrays are united.
    /// </summary>
    public sealed class ChargingStationTemplate : APOITemplate<ChargingStationTemplate_Id>,
                                                  IEquatable<ChargingStationTemplate>,
                                                  IComparable<ChargingStationTemplate>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/chargingStationTemplate";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization: those of the properties of a charging station as a charging
        /// station writes them.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its identification.
            /// </summary>
            public const String  Id                    = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context               = "@context";

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name                  = "name";

            /// <summary>
            /// The property of its multilingual description.
            /// </summary>
            public const String  Description           = "description";

            /// <summary>
            /// The property of its data source.
            /// </summary>
            public const String  DataSource            = "dataSource";

            /// <summary>
            /// The property of the identification of its charging tariff.
            /// </summary>
            public const String  ChargingTariffId      = "chargingTariffId";

            /// <summary>
            /// The property of the reference printed on its members.
            /// </summary>
            public const String  PhysicalReference     = ChargingStation.PropertyKeys.PhysicalReference;

            /// <summary>
            /// The property of the address of its members.
            /// </summary>
            public const String  Address               = ChargingStation.PropertyKeys.Address;

            /// <summary>
            /// The property of the geographical location of its members.
            /// </summary>
            public const String  GeoLocation           = ChargingStation.PropertyKeys.GeoLocation;

            /// <summary>
            /// The property of the hotline phone number of its members.
            /// </summary>
            public const String  HotlinePhoneNumber    = ChargingStation.PropertyKeys.HotlinePhoneNumber;

            /// <summary>
            /// The property of the identification of the manufacturer of its members.
            /// </summary>
            public const String  ManufacturerId        = ChargingStation.PropertyKeys.ManufacturerId;

            /// <summary>
            /// The property of the identification of the model of its members.
            /// </summary>
            public const String  ModelId               = ChargingStation.PropertyKeys.ModelId;

            /// <summary>
            /// The property of the brands of its members.
            /// </summary>
            public const String  Brands                = ChargingStation.PropertyKeys.Brands;

            /// <summary>
            /// The property of the identifications of the brands of its members, refused: they are given completely.
            /// </summary>
            public const String  BrandIds              = ChargingStation.PropertyKeys.BrandIds;

            /// <summary>
            /// The property of the opening times of its members.
            /// </summary>
            public const String  OpeningTimes          = ChargingStation.PropertyKeys.OpeningTimes;

            /// <summary>
            /// The property of the images of its members.
            /// </summary>
            public const String  Images                = ChargingStation.PropertyKeys.Images;

            /// <summary>
            /// The property of the types of vehicles its members charge.
            /// </summary>
            public const String  VehicleTypes          = ChargingStation.PropertyKeys.VehicleTypes;

            /// <summary>
            /// The property of the EU vehicle categories its members charge.
            /// </summary>
            public const String  EUVehicleCategories   = ChargingStation.PropertyKeys.EUVehicleCategories;

            /// <summary>
            /// The property of who may use its members.
            /// </summary>
            public const String  Accessibility         = ChargingStation.PropertyKeys.Accessibility;

            /// <summary>
            /// The property of the features of its members.
            /// </summary>
            public const String  Features              = ChargingStation.PropertyKeys.Features;

            /// <summary>
            /// The property of where to find information on the calibration of the energy meters of its members.
            /// </summary>
            public const String  CalibrationInfo       = ChargingStation.PropertyKeys.CalibrationInfo;

            /// <summary>
            /// The property of where to find information on the certification of its members.
            /// </summary>
            public const String  CertificationInfo     = ChargingStation.PropertyKeys.CertificationInfo;

            /// <summary>
            /// The property of the maximum power of its members.
            /// </summary>
            public const String  MaxPower              = ChargingStation.PropertyKeys.MaxPower;

            /// <summary>
            /// The property of the identifications of the parking spaces its members charge at.
            /// </summary>
            public const String  ParkingSpaceIds       = ChargingStation.PropertyKeys.ParkingSpaceIds;

            /// <summary>
            /// The property of its creation timestamp.
            /// </summary>
            public const String  Created               = "created";

            /// <summary>
            /// The property of its last change timestamp.
            /// </summary>
            public const String  LastChange            = "lastChange";

            /// <summary>
            /// The property of its custom data.
            /// </summary>
            public const String  CustomData            = "customData";

        }

        #endregion


        /// <summary>
        /// The charging tariff of its operator for the EVSEs of its members without tariffs of their own, unless more
        /// specific templates name one: EVSE templates before charging station templates before charging pool templates,
        /// of one level the first one of a member naming one (ChargingStationOperator.EffectiveEVSE(...)).
        /// </summary>
        public ChargingTariff_Id?                  ChargingTariffId    { get; }

        /// <summary>
        /// The reference printed on its members, e.g. "Station 2", unless they have their own.
        /// </summary>
        public String?                                 PhysicalReference    { get; }

        /// <summary>
        /// The address of its members, unless they have their own.
        /// </summary>
        public Address?                                Address              { get; }

        /// <summary>
        /// The geographical location of its members, unless they have their own.
        /// </summary>
        public GeoCoordinate?                          GeoLocation          { get; }

        /// <summary>
        /// The hotline phone number of its members, unless they have their own.
        /// </summary>
        public PhoneNumber?                            HotlinePhoneNumber   { get; }

        /// <summary>
        /// The manufacturer of its members, one of its roaming network, unless they have their own.
        /// </summary>
        public ChargingStationManufacturer_Id?         ManufacturerId       { get; }

        /// <summary>
        /// The model of its members, as their manufacturer names it, unless they have their own.
        /// </summary>
        public ChargingStationModel_Id?                ModelId              { get; }

        /// <summary>
        /// Brands its members also appear under, besides their own, in the order of their identifications.
        /// </summary>
        public POICollection<Brand_Id, Brand>          Brands               { get; }

        /// <summary>
        /// When its members are open, unless they have their own.
        /// </summary>
        public ImmutableOpeningTimes?                  OpeningTimes         { get; }

        /// <summary>
        /// Images of its members, besides their own, in their given order.
        /// </summary>
        public ImmutableArray<Image>                   Images               { get; }

        /// <summary>
        /// Types of vehicles its members charge, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<VehicleType>             VehicleTypes         { get; }

        /// <summary>
        /// EU vehicle categories its members charge, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<EUVehicleCategory>       EUVehicleCategories  { get; }

        /// <summary>
        /// Who may use its members, unless they tell it themselves.
        /// </summary>
        public AccessibilityType?                      Accessibility        { get; }

        /// <summary>
        /// Features of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<ChargingStationFeature>  Features             { get; }

        /// <summary>
        /// Where to find information on the calibration of the energy meters of its members, unless they have their own.
        /// </summary>
        public URL?                                    CalibrationInfo      { get; }

        /// <summary>
        /// Where to find information on the certification of its members, unless they have their own.
        /// </summary>
        public URL?                                    CertificationInfo    { get; }

        /// <summary>
        /// The power each of its members can deliver at once, unless they have their own:
        /// never more than all the EVSEs of a member together.
        /// </summary>
        public Watt?                                   MaxPower             { get; }

        /// <summary>
        /// Parking spaces of the roaming network its members charge at, besides their own, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ParkingSpace_Id>         ParkingSpaceIds      { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station template.
        /// </summary>
        public ChargingStationTemplate(ChargingStationTemplate_Id                             Id,
                                    I18NString?                                         Name                         = null,
                                    I18NString?                                         Description                  = null,
                                    ChargingTariff_Id?                                  ChargingTariffId             = null,

                                    String?                                             PhysicalReference            = null,
                                    Address?                                            Address                      = null,
                                    GeoCoordinate?                                      GeoLocation                  = null,
                                    PhoneNumber?                                        HotlinePhoneNumber           = null,
                                    ChargingStationManufacturer_Id?                     ManufacturerId               = null,
                                    ChargingStationModel_Id?                            ModelId                      = null,
                                    IEnumerable<Brand>?                                 Brands                       = null,
                                    ImmutableOpeningTimes?                              OpeningTimes                 = null,
                                    IEnumerable<Image>?                                 Images                       = null,
                                    IEnumerable<VehicleType>?                           VehicleTypes                 = null,
                                    IEnumerable<EUVehicleCategory>?                     EUVehicleCategories          = null,
                                    AccessibilityType?                                  Accessibility                = null,
                                    IEnumerable<ChargingStationFeature>?                Features                     = null,
                                    URL?                                                CalibrationInfo              = null,
                                    URL?                                                CertificationInfo            = null,
                                    Watt?                                               MaxPower                     = null,
                                    IEnumerable<ParkingSpace_Id>?                       ParkingSpaceIds              = null,

                                    String?                                             DataSource                   = null,
                                    DateTimeOffset?                                     Created                      = null,
                                    DateTimeOffset?                                     LastChange                   = null,

                                    CustomDataNew?                                      CustomData                   = null,
                                    UserDefinedDictionary?                              InternalData                 = null)

            : base(Id,
                   Name,
                   Description,
                   DataSource,
                   Created,
                   LastChange,
                   CustomData,
                   InternalData)

        {

            if (MaxPower?.Value < 0)
                throw new ArgumentException($"The maximum power of the charging station template '{Id}' must not be negative!", nameof(MaxPower));
            this.ChargingTariffId     = ChargingTariffId;

            this.PhysicalReference    = PhysicalReference;
            this.Address              = Address;
            this.GeoLocation          = GeoLocation;
            this.HotlinePhoneNumber   = HotlinePhoneNumber;
            this.ManufacturerId       = ManufacturerId;
            this.ModelId              = ModelId;
            this.Brands               = new (Brands, brand => brand.Id, "brand", $"the charging station template '{Id}'");
            this.OpeningTimes         = OpeningTimes;
            this.Images               = POIJSON.Distinct  (Images, image => image.URL.ToString(), $"image of the charging station template '{Id}'");
            this.VehicleTypes         = POIJSON.OrderedIds(VehicleTypes,        $"vehicle type of the charging station template '{Id}'");
            this.EUVehicleCategories  = POIJSON.OrderedIds(EUVehicleCategories, $"EU vehicle category of the charging station template '{Id}'");
            this.Accessibility        = Accessibility;
            this.Features             = POIJSON.OrderedIds(Features,            $"feature of the charging station template '{Id}'");
            this.CalibrationInfo      = CalibrationInfo;
            this.CertificationInfo    = CertificationInfo;
            this.MaxPower             = MaxPower;
            this.ParkingSpaceIds      = POIJSON.OrderedIds(ParkingSpaceIds,     $"parking space of the charging station template '{Id}'");

        }

        #endregion

        #region ToJSON(Embedded = false)

        /// <inheritdoc/>
        public override JObject ToJSON(Boolean Embedded = false)

            => ToJSON(Embedded,
                      CustomChargingStationTemplateSerializer: null);

        #endregion

        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return the JSON representation of this charging station template; embedded, as its charging station operator embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomChargingStationTemplateSerializer">A delegate to serialize custom charging station template JSON objects.</param>
        public JObject ToJSON(Boolean                                                    Embedded                                  = false,
                              CustomJObjectSerializerDelegate<ChargingStationTemplate>?  CustomChargingStationTemplateSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Id,  Id.ToString())
                       );

            if (!Embedded)
                json.Add(PropertyKeys.Context,           JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Name,              Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Description,       Description.ToJSON());

            if (DataSource is not null)
                json.Add(PropertyKeys.DataSource,        DataSource);

            if (ChargingTariffId.HasValue)
                json.Add(PropertyKeys.ChargingTariffId,  ChargingTariffId.Value.ToString());

            // The properties of a charging station, as a charging station writes them.
            if (PhysicalReference is not null)
                json.Add(PropertyKeys.PhysicalReference,    PhysicalReference);

            if (Address is not null)
                json.Add(PropertyKeys.Address,              Address.ToJSON(Embedded: true));

            if (GeoLocation.HasValue)
                json.Add(PropertyKeys.GeoLocation,          GeoLocation.Value.ToJSON(Embedded: true));

            if (HotlinePhoneNumber.HasValue)
                json.Add(PropertyKeys.HotlinePhoneNumber,   HotlinePhoneNumber.Value.ToString());

            if (ManufacturerId.HasValue)
                json.Add(PropertyKeys.ManufacturerId,       ManufacturerId.Value.ToString());

            if (ModelId.HasValue)
                json.Add(PropertyKeys.ModelId,              ModelId.Value.ToString());

            if (Brands.Count > 0)
                json.Add(PropertyKeys.Brands,               new JArray(Brands.Select(brand => brand.ToJSON(Embedded: true))));

            if (OpeningTimes is not null)
                json.Add(PropertyKeys.OpeningTimes,         OpeningTimes.ToJSON());

            if (Images.Length > 0)
                json.Add(PropertyKeys.Images,               new JArray(Images.Select(image => image.ToJSON(Embedded: true))));

            if (VehicleTypes.Length > 0)
                json.Add(PropertyKeys.VehicleTypes,         POIJSON.IdsToJSON(VehicleTypes));

            if (EUVehicleCategories.Length > 0)
                json.Add(PropertyKeys.EUVehicleCategories,  POIJSON.IdsToJSON(EUVehicleCategories));

            if (Accessibility.HasValue)
                json.Add(PropertyKeys.Accessibility,        Accessibility.Value.ToString());

            if (Features.Length > 0)
                json.Add(PropertyKeys.Features,             POIJSON.IdsToJSON(Features));

            if (CalibrationInfo.HasValue)
                json.Add(PropertyKeys.CalibrationInfo,      CalibrationInfo.Value.ToString());

            if (CertificationInfo.HasValue)
                json.Add(PropertyKeys.CertificationInfo,    CertificationInfo.Value.ToString());

            if (MaxPower.HasValue)
                json.Add(PropertyKeys.MaxPower,             POIJSON.Number(MaxPower.Value.Value));

            if (ParkingSpaceIds.Length > 0)
                json.Add(PropertyKeys.ParkingSpaceIds,      POIJSON.IdsToJSON(ParkingSpaceIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return CustomChargingStationTemplateSerializer is not null
                       ? CustomChargingStationTemplateSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingStationTemplate, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging station template.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingStationTemplate">The parsed charging station template.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out ChargingStationTemplate?   ChargingStationTemplate,
                                       [NotNullWhen(false)] out String?                 ErrorResponse)
        {

            ChargingStationTemplate = null;

            try
            {

                if (!JSON.ParseMandatory(PropertyKeys.Id,
                                         "charging station template identification",
                                         ChargingStationTemplate_Id.TryParse,
                                         out ChargingStationTemplate_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging station template '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, PropertyKeys.Name,         "charging station template name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, PropertyKeys.Description,  "charging station template description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, PropertyKeys.DataSource,   "data source",                       out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, PropertyKeys.ChargingTariffId,  "charging tariff identification",  ChargingTariff_Id.TryParse, out ChargingTariff_Id? chargingTariffId, out ErrorResponse))
                {
                    return false;
                }

                // The properties of a charging station, as a charging station reads them.
                if (!POIJSON.TryParseString     (JSON, PropertyKeys.PhysicalReference,   "physical reference",            out var physicalReference,  out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON, PropertyKeys.Address,             "address",                       out var address,            out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON, PropertyKeys.GeoLocation,         "geo location",                  out var geoLocation,        out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, PropertyKeys.HotlinePhoneNumber,  "hotline phone number",          PhoneNumber.TryParse,       out PhoneNumber? hotline,  out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, PropertyKeys.ManufacturerId,      "manufacturer identification",   ChargingStationManufacturer_Id.TryParse, out ChargingStationManufacturer_Id? manufacturerId, out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, PropertyKeys.ModelId,             "model identification",          ChargingStationModel_Id.       TryParse, out ChargingStationModel_Id?        modelId,        out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<Brand>(JSON, PropertyKeys.Brands, PropertyKeys.BrandIds, "brands", Brand.TryParse, out var brands, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObject<ImmutableOpeningTimes>(JSON, PropertyKeys.OpeningTimes, "opening times", ImmutableOpeningTimes.TryParse, out var openingTimes, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObjects<Image>(JSON, PropertyKeys.Images, "images", Image.TryParse, out var images, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseIds (JSON, PropertyKeys.VehicleTypes,        "vehicle type",                  VehicleType.           TryParse, out ImmutableArray<VehicleType>             vehicleTypes,        out ErrorResponse) ||
                    !POIJSON.TryParseIds (JSON, PropertyKeys.EUVehicleCategories, "EU vehicle category",           EUVehicleCategory.     TryParse, out ImmutableArray<EUVehicleCategory>       euVehicleCategories, out ErrorResponse) ||
                    !POIJSON.TryParseText(JSON, PropertyKeys.Accessibility,       "accessibility",                 AccessibilityType.     TryParse, out AccessibilityType?                      accessibility,       out ErrorResponse) ||
                    !POIJSON.TryParseIds (JSON, PropertyKeys.Features,            "charging station feature",      ChargingStationFeature.TryParse, out ImmutableArray<ChargingStationFeature>  features,            out ErrorResponse) ||
                    !POIJSON.TryParseText(JSON, PropertyKeys.CalibrationInfo,     "calibration information",       URL.                   TryParse, out URL?                                    calibrationInfo,     out ErrorResponse) ||
                    !POIJSON.TryParseText(JSON, PropertyKeys.CertificationInfo,   "certification information",     URL.                   TryParse, out URL?                                    certificationInfo,   out ErrorResponse) ||
                    !POIJSON.TryParseIds (JSON, PropertyKeys.ParkingSpaceIds,     "parking space identification",  ParkingSpace_Id.       TryParse, out ImmutableArray<ParkingSpace_Id>         parkingSpaceIds,     out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional(PropertyKeys.MaxPower, "maximum power", out Watt? maxPower, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                ChargingStationTemplate = new ChargingStationTemplate(id,
                                                                name,
                                                                description,
                                                                chargingTariffId,
                                                                physicalReference,
                                                                address,
                                                                geoLocation,
                                                                hotline,
                                                                manufacturerId,
                                                                modelId,
                                                                brands,
                                                                openingTimes,
                                                                images,
                                                                vehicleTypes,
                                                                euVehicleCategories,
                                                                accessibility,
                                                                features,
                                                                calibrationInfo,
                                                                certificationInfo,
                                                                maxPower,
                                                                parkingSpaceIds,
                                                                DataSource:  dataSource,
                                                                Created:     created,
                                                                LastChange:  lastChange,
                                                                CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingStationTemplate  = null;
                ErrorResponse         = "The given JSON representation of a charging station template is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two charging station templates for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ChargingStationTemplate? ChargingStationTemplate1, ChargingStationTemplate? ChargingStationTemplate2)

            => ReferenceEquals(ChargingStationTemplate1, ChargingStationTemplate2) ||
               (ChargingStationTemplate1 is not null && ChargingStationTemplate1.Equals(ChargingStationTemplate2));

        /// <summary>
        /// Compares two charging station templates for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ChargingStationTemplate? ChargingStationTemplate1, ChargingStationTemplate? ChargingStationTemplate2)

            => !(ChargingStationTemplate1 == ChargingStationTemplate2);

        /// <inheritdoc/>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingStationTemplate chargingStationTemplate
                   ? CompareTo(chargingStationTemplate)
                   : throw new ArgumentException("The given object is not a charging station template!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ChargingStationTemplate? ChargingStationTemplate)

            => ChargingStationTemplate is null
                   ? throw new ArgumentNullException(nameof(ChargingStationTemplate), "The given charging station template must not be null!")
                   : Id.CompareTo(ChargingStationTemplate.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingStationTemplate chargingStationTemplate &&
               Equals(chargingStationTemplate);

        /// <inheritdoc/>
        public Boolean Equals(ChargingStationTemplate? ChargingStationTemplate)

            => ChargingStationTemplate is not null &&
               Id.Equals(ChargingStationTemplate.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
