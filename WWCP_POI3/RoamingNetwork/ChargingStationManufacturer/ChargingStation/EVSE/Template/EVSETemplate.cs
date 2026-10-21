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
    /// A template of EVSEs of one charging station operator, embedded completely in it: what the EVSEs naming it in their
    /// templateIds share, e.g. the current types, the power and the connectors of all EVSEs of a type, and a charging tariff
    /// for them. The EVSEs take it at runtime, as the effective EVSEs of their operator: a value of their own wins, else the
    /// one of the first of their templates giving one; arrays are united.
    /// </summary>
    public sealed class EVSETemplate : APOITemplate<EVSETemplate_Id>,
                                       IEquatable<EVSETemplate>,
                                       IComparable<EVSETemplate>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/evseTemplate";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization: those of the properties of an EVSE as an EVSE writes them.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its identification.
            /// </summary>
            public const String  Id                             = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context                        = "@context";

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name                           = "name";

            /// <summary>
            /// The property of its multilingual description.
            /// </summary>
            public const String  Description                    = "description";

            /// <summary>
            /// The property of its data source.
            /// </summary>
            public const String  DataSource                     = "dataSource";

            /// <summary>
            /// The property of the identification of its charging tariff.
            /// </summary>
            public const String  ChargingTariffId               = "chargingTariffId";

            /// <summary>
            /// The property of the reference printed on its members.
            /// </summary>
            public const String  PhysicalReference              = EVSE.PropertyKeys.PhysicalReference;

            /// <summary>
            /// The property of the current types of its members.
            /// </summary>
            public const String  CurrentTypes                   = EVSE.PropertyKeys.CurrentTypes;

            /// <summary>
            /// The property of the charging modes of its members.
            /// </summary>
            public const String  ChargingModes                  = EVSE.PropertyKeys.ChargingModes;

            /// <summary>
            /// The property of the maximum voltage of its members.
            /// </summary>
            public const String  MaxVoltage                     = EVSE.PropertyKeys.MaxVoltage;

            /// <summary>
            /// The property of the maximum current of its members.
            /// </summary>
            public const String  MaxCurrent                     = EVSE.PropertyKeys.MaxCurrent;

            /// <summary>
            /// The property of the maximum power of its members.
            /// </summary>
            public const String  MaxPower                       = EVSE.PropertyKeys.MaxPower;

            /// <summary>
            /// The property of the charging connectors of its members.
            /// </summary>
            public const String  ChargingConnectors             = EVSE.PropertyKeys.ChargingConnectors;

            /// <summary>
            /// The property of the identifications of the charging connectors of its members, refused: they are given completely.
            /// </summary>
            public const String  ChargingConnectorIds           = EVSE.PropertyKeys.ChargingConnectorIds;

            /// <summary>
            /// The property of the brands of its members.
            /// </summary>
            public const String  Brands                         = EVSE.PropertyKeys.Brands;

            /// <summary>
            /// The property of the identifications of the brands of its members, refused: they are given completely.
            /// </summary>
            public const String  BrandIds                       = EVSE.PropertyKeys.BrandIds;

            /// <summary>
            /// The property of the images of its members.
            /// </summary>
            public const String  Images                         = EVSE.PropertyKeys.Images;

            /// <summary>
            /// The property of the geographical location of its members.
            /// </summary>
            public const String  GeoLocation                    = EVSE.PropertyKeys.GeoLocation;

            /// <summary>
            /// The property of the maximum reservation duration of its members.
            /// </summary>
            public const String  MaxReservationDuration         = EVSE.PropertyKeys.MaxReservationDuration;

            /// <summary>
            /// The property of the reach distance of its members.
            /// </summary>
            public const String  ReachDistance                  = EVSE.PropertyKeys.ReachDistance;

            /// <summary>
            /// The property of the operation timeout of its members.
            /// </summary>
            public const String  OperationTimeout               = EVSE.PropertyKeys.OperationTimeout;

            /// <summary>
            /// The property of whether its members have an extended operation timeout.
            /// </summary>
            public const String  ExtendedOperationTimeout       = EVSE.PropertyKeys.ExtendedOperationTimeout;

            /// <summary>
            /// The property of the standards of its members.
            /// </summary>
            public const String  Standards                      = EVSE.PropertyKeys.Standards;

            /// <summary>
            /// The property of the floor level of its members.
            /// </summary>
            public const String  FloorLevel                     = EVSE.PropertyKeys.FloorLevel;

            /// <summary>
            /// The property of the e-mobility providers whose contracts its members accept.
            /// </summary>
            public const String  AcceptedEMobilityProviderIds   = EVSE.PropertyKeys.AcceptedEMobilityProviderIds;

            /// <summary>
            /// The property of its creation timestamp.
            /// </summary>
            public const String  Created                        = "created";

            /// <summary>
            /// The property of its last change timestamp.
            /// </summary>
            public const String  LastChange                     = "lastChange";

            /// <summary>
            /// The property of its custom data.
            /// </summary>
            public const String  CustomData                     = "customData";

        }

        #endregion


        /// <summary>
        /// The charging tariff of its operator for the EVSEs of its members without tariffs of their own, unless more
        /// specific templates name one: EVSE templates before charging station templates before charging pool templates,
        /// of one level the first one of a member naming one (ChargingStationOperator.EffectiveEVSE(...)).
        /// </summary>
        public ChargingTariff_Id?       ChargingTariffId  { get; }

        /// <summary>
        /// The reference printed on its members, unless they have their own.
        /// </summary>
        public String?                            PhysicalReference         { get; }

        /// <summary>
        /// The current types of its members, unless they tell their own; Unspecified gives none.
        /// </summary>
        public CurrentTypes                       CurrentTypes              { get; }

        /// <summary>
        /// The charging modes of its members, unless they tell their own; Unspecified gives none.
        /// </summary>
        public ChargingModes                      ChargingModes             { get; }

        /// <summary>
        /// The maximum voltage of its members, unless they have their own.
        /// </summary>
        public Volt?                              MaxVoltage                { get; }

        /// <summary>
        /// The maximum current of its members, unless they have their own.
        /// </summary>
        public Ampere?                            MaxCurrent                { get; }

        /// <summary>
        /// The maximum power of its members, unless they have their own.
        /// </summary>
        public Watt?                              MaxPower                  { get; }

        /// <summary>
        /// Charging connectors of its members, besides their own, in the order of their identifications;
        /// of the same identification their own.
        /// </summary>
        public ImmutableArray<ChargingConnector>  ChargingConnectors        { get; }

        /// <summary>
        /// Brands its members also appear under, besides their own, in the order of their identifications.
        /// </summary>
        public POICollection<Brand_Id, Brand>     Brands                    { get; }

        /// <summary>
        /// Images of its members, besides their own, in their given order.
        /// </summary>
        public ImmutableArray<Image>              Images                    { get; }

        /// <summary>
        /// The geographical location of its members, unless they have their own.
        /// </summary>
        public GeoCoordinate?                     GeoLocation               { get; }

        /// <summary>
        /// The maximum reservation duration of its members, unless they have their own.
        /// </summary>
        public TimeSpan?                          MaxReservationDuration    { get; }

        /// <summary>
        /// The reach distance of its members, unless they have their own.
        /// </summary>
        public Meter?                             ReachDistance             { get; }

        /// <summary>
        /// The operation timeout of its members, unless they have their own.
        /// </summary>
        public TimeSpan?                          OperationTimeout          { get; }

        /// <summary>
        /// Whether its members have an extended operation timeout, unless they tell it themselves.
        /// </summary>
        public Boolean?                           ExtendedOperationTimeout  { get; }

        /// <summary>
        /// Standards of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<Standard>           Standards                 { get; }

        /// <summary>
        /// The floor level of its members, unless they tell it themselves.
        /// </summary>
        public String?                            FloorLevel                { get; }

        /// <summary>
        /// E-mobility providers whose contracts its members accept, besides their own, in the order of their identifications.
        /// </summary>
        public ImmutableArray<EMobilityProvider_Id>  AcceptedEMobilityProviderIds  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EVSE template.
        /// </summary>
        public EVSETemplate(EVSETemplate_Id                             Id,
                         I18NString?                              Name                         = null,
                         I18NString?                              Description                  = null,
                         ChargingTariff_Id?                       ChargingTariffId             = null,

                         String?                                  PhysicalReference            = null,
                         CurrentTypes?                            CurrentTypes                 = null,
                         ChargingModes?                           ChargingModes                = null,
                         Volt?                                    MaxVoltage                   = null,
                         Ampere?                                  MaxCurrent                   = null,
                         Watt?                                    MaxPower                     = null,
                         IEnumerable<ChargingConnector>?          ChargingConnectors           = null,
                         IEnumerable<Brand>?                      Brands                       = null,
                         IEnumerable<Image>?                      Images                       = null,
                         GeoCoordinate?                           GeoLocation                  = null,
                         TimeSpan?                                MaxReservationDuration       = null,
                         Meter?                                   ReachDistance                = null,
                         TimeSpan?                                OperationTimeout             = null,
                         Boolean?                                 ExtendedOperationTimeout     = null,
                         IEnumerable<Standard>?                   Standards                    = null,
                         String?                                  FloorLevel                   = null,
                         IEnumerable<EMobilityProvider_Id>?       AcceptedEMobilityProviderIds = null,

                         String?                                  DataSource                   = null,
                         DateTimeOffset?                          Created                      = null,
                         DateTimeOffset?                          LastChange                   = null,

                         CustomDataNew?                           CustomData                   = null,
                         UserDefinedDictionary?                   InternalData                 = null)

            : base(Id,
                   Name,
                   Description,
                   DataSource,
                   Created,
                   LastChange,
                   CustomData,
                   InternalData)

        {

            if (MaxVoltage?.Value < 0 || MaxCurrent?.Value < 0 || MaxPower?.Value < 0)
                throw new ArgumentException($"The electrical limits of the EVSE template '{Id}' must not be negative!");

            if (MaxReservationDuration < TimeSpan.Zero)
                throw new ArgumentException($"The maximum reservation duration of the EVSE template '{Id}' must not be negative!", nameof(MaxReservationDuration));

            if (ReachDistance?.m < 0)
                throw new ArgumentException($"The reach distance of the EVSE template '{Id}' must not be negative!", nameof(ReachDistance));

            if (OperationTimeout < TimeSpan.Zero)
                throw new ArgumentException($"The operation timeout of the EVSE template '{Id}' must not be negative!", nameof(OperationTimeout));

            var chargingConnectors = (ChargingConnectors ?? []).ToArray();

            if (chargingConnectors.Any(chargingConnector => chargingConnector is null))
                throw new ArgumentException("Charging connectors must not contain null entries!", nameof(ChargingConnectors));

            if (chargingConnectors.Select(chargingConnector => chargingConnector.Id).Distinct().Count() != chargingConnectors.Length)
                throw new ArgumentException($"A charging connector of the EVSE template '{Id}' must not be given twice!", nameof(ChargingConnectors));

            foreach (var chargingConnector in chargingConnectors)
                if (chargingConnector.Validate() is String error)
                    throw new ArgumentException(error, nameof(ChargingConnectors));
            this.ChargingTariffId          = ChargingTariffId;

            this.PhysicalReference         = PhysicalReference;
            this.CurrentTypes              = CurrentTypes  ?? POI.CurrentTypes. Unspecified;
            this.ChargingModes             = ChargingModes ?? POI.ChargingModes.Unspecified;
            this.MaxVoltage                = MaxVoltage;
            this.MaxCurrent                = MaxCurrent;
            this.MaxPower                  = MaxPower;
            this.ChargingConnectors        = [.. chargingConnectors.OrderBy(chargingConnector => chargingConnector.Id.ToString(), StringComparer.Ordinal)];
            this.Brands                    = new (Brands, brand => brand.Id, "brand", $"the EVSE template '{Id}'");
            this.Images                    = POIJSON.Distinct(Images, image => image.URL.ToString(), $"image of the EVSE template '{Id}'");
            this.GeoLocation               = GeoLocation;
            this.MaxReservationDuration    = MaxReservationDuration;
            this.ReachDistance             = ReachDistance;
            this.OperationTimeout          = OperationTimeout;
            this.ExtendedOperationTimeout  = ExtendedOperationTimeout;
            this.Standards                 = POIJSON.OrderedIds(Standards, $"standard of the EVSE template '{Id}'");
            this.FloorLevel                = FloorLevel;
            this.AcceptedEMobilityProviderIds  = POIJSON.OrderedIds(AcceptedEMobilityProviderIds, $"accepted e-mobility provider of the EVSE template '{Id}'");

        }

        #endregion

        #region ToJSON(Embedded = false)

        /// <inheritdoc/>
        public override JObject ToJSON(Boolean Embedded = false)

            => ToJSON(Embedded,
                      CustomEVSETemplateSerializer: null);

        #endregion

        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return the JSON representation of this EVSE template; embedded, as its charging station operator embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomEVSETemplateSerializer">A delegate to serialize custom EVSE template JSON objects.</param>
        public JObject ToJSON(Boolean                                         Embedded                       = false,
                              CustomJObjectSerializerDelegate<EVSETemplate>?  CustomEVSETemplateSerializer   = null)
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

            // The properties of an EVSE, as an EVSE writes them.
            if (PhysicalReference is not null)
                json.Add(PropertyKeys.PhysicalReference,             PhysicalReference);

            if (CurrentTypes  != POI.CurrentTypes. Unspecified)
                json.Add(PropertyKeys.CurrentTypes,                  POIJSON.FlagsToJSON(CurrentTypes));

            if (ChargingModes != POI.ChargingModes.Unspecified)
                json.Add(PropertyKeys.ChargingModes,                 POIJSON.FlagsToJSON(ChargingModes));

            if (MaxVoltage.HasValue)
                json.Add(PropertyKeys.MaxVoltage,                    POIJSON.Number(MaxVoltage.Value.Value));

            if (MaxCurrent.HasValue)
                json.Add(PropertyKeys.MaxCurrent,                    POIJSON.Number(MaxCurrent.Value.Value));

            if (MaxPower.HasValue)
                json.Add(PropertyKeys.MaxPower,                      POIJSON.Number(MaxPower.Value.Value));

            if (ChargingConnectors.Length > 0)
                json.Add(PropertyKeys.ChargingConnectors,            new JArray(ChargingConnectors.Select(chargingConnector => chargingConnector.ToJSON(Embedded: true))));

            if (Brands.Count > 0)
                json.Add(PropertyKeys.Brands,                        new JArray(Brands.Select(brand => brand.ToJSON(Embedded: true))));

            if (Images.Length > 0)
                json.Add(PropertyKeys.Images,                        new JArray(Images.Select(image => image.ToJSON(Embedded: true))));

            if (GeoLocation.HasValue)
                json.Add(PropertyKeys.GeoLocation,                   GeoLocation.Value.ToJSON(Embedded: true));

            if (MaxReservationDuration.HasValue)
                json.Add(PropertyKeys.MaxReservationDuration,        POIJSON.Seconds(MaxReservationDuration.Value));

            if (ReachDistance.HasValue)
                json.Add(PropertyKeys.ReachDistance,                 POIJSON.Number(ReachDistance.Value.m));

            if (OperationTimeout.HasValue)
                json.Add(PropertyKeys.OperationTimeout,              POIJSON.Seconds(OperationTimeout.Value));

            if (ExtendedOperationTimeout.HasValue)
                json.Add(PropertyKeys.ExtendedOperationTimeout,      ExtendedOperationTimeout.Value);

            if (Standards.Length > 0)
                json.Add(PropertyKeys.Standards,                     POIJSON.IdsToJSON(Standards));

            if (FloorLevel is not null)
                json.Add(PropertyKeys.FloorLevel,                    FloorLevel);

            if (AcceptedEMobilityProviderIds.Length > 0)
                json.Add(PropertyKeys.AcceptedEMobilityProviderIds,  POIJSON.IdsToJSON(AcceptedEMobilityProviderIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return CustomEVSETemplateSerializer is not null
                       ? CustomEVSETemplateSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out EVSETemplate, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an EVSE template.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EVSETemplate">The parsed EVSE template.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                               JSON,
                                       [NotNullWhen(true)]  out EVSETemplate?   EVSETemplate,
                                       [NotNullWhen(false)] out String?      ErrorResponse)
        {

            EVSETemplate = null;

            try
            {

                if (!JSON.ParseMandatory(PropertyKeys.Id,
                                         "EVSE template identification",
                                         EVSETemplate_Id.TryParse,
                                         out EVSETemplate_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the EVSE template '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, PropertyKeys.Name,         "EVSE template name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, PropertyKeys.Description,  "EVSE template description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, PropertyKeys.DataSource,   "data source",           out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, PropertyKeys.ChargingTariffId,  "charging tariff identification",  ChargingTariff_Id.TryParse, out ChargingTariff_Id? chargingTariffId, out ErrorResponse))
                {
                    return false;
                }

                // The properties of an EVSE, as an EVSE reads them.
                if (!POIJSON.TryParseString(JSON, PropertyKeys.PhysicalReference,  "physical reference",  out var physicalReference,  out ErrorResponse) ||
                    !POIJSON.TryParseFlags (JSON, PropertyKeys.CurrentTypes,       "current type",        out CurrentTypes  currentTypes,  out ErrorResponse) ||
                    !POIJSON.TryParseFlags (JSON, PropertyKeys.ChargingModes,      "charging mode",       out ChargingModes chargingModes, out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional(PropertyKeys.MaxVoltage, "maximum voltage", out Volt?   maxVoltage, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional(PropertyKeys.MaxCurrent, "maximum current", out Ampere? maxCurrent, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional(PropertyKeys.MaxPower,   "maximum power",   out Watt?   maxPower,   out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (!POIJSON.TryParseChildren<ChargingConnector>(JSON,
                                                                 PropertyKeys.ChargingConnectors,
                                                                 PropertyKeys.ChargingConnectorIds,
                                                                 "charging connectors",
                                                                 ChargingConnector.TryParse,
                                                                 out var chargingConnectors,
                                                                 out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<Brand>(JSON, PropertyKeys.Brands, PropertyKeys.BrandIds, "brands", Brand.TryParse, out var brands, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObjects<Image>(JSON, PropertyKeys.Images, "images", Image.TryParse, out var images, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseGeoLocation(JSON, PropertyKeys.GeoLocation,
                                                       "geo location",                 out var geoLocation,             out ErrorResponse) ||
                    !POIJSON.TryParseSeconds    (JSON, PropertyKeys.MaxReservationDuration,
                                                       "maximum reservation duration", out var maxReservationDuration,  out ErrorResponse) ||
                    !POIJSON.TryParseSeconds    (JSON, PropertyKeys.OperationTimeout,
                                                       "operation timeout",            out var operationTimeout,        out ErrorResponse) ||
                    !POIJSON.TryParseNonNegative(JSON, PropertyKeys.ReachDistance,
                                                       "reach distance",               out var reachDistance,           out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.Standards, "standard", Standard.TryParse, out ImmutableArray<Standard> standards, out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, PropertyKeys.FloorLevel,           "floor level",                  out var floorLevel,              out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.AcceptedEMobilityProviderIds, "accepted e-mobility provider identification",
                                                 EMobilityProvider_Id.TryParse, out ImmutableArray<EMobilityProvider_Id> acceptedEMobilityProviderIds, out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional(PropertyKeys.ExtendedOperationTimeout, "extended operation timeout", out Boolean? extendedOperationTimeout, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                EVSETemplate = new EVSETemplate(id,
                                          name,
                                          description,
                                          chargingTariffId,
                                          physicalReference,
                                          currentTypes,
                                          chargingModes,
                                          maxVoltage,
                                          maxCurrent,
                                          maxPower,
                                          chargingConnectors,
                                          brands,
                                          images,
                                          geoLocation,
                                          maxReservationDuration,
                                          reachDistance.HasValue ? Meter.From_m(reachDistance.Value) : null,
                                          operationTimeout,
                                          extendedOperationTimeout,
                                          standards,
                                          floorLevel,
                                          acceptedEMobilityProviderIds,
                                          DataSource:  dataSource,
                                          Created:     created,
                                          LastChange:  lastChange,
                                          CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                EVSETemplate      = null;
                ErrorResponse  = "The given JSON representation of an EVSE template is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two EVSE templates for equality by their identifications.
        /// </summary>
        public static Boolean operator == (EVSETemplate? EVSETemplate1, EVSETemplate? EVSETemplate2)

            => ReferenceEquals(EVSETemplate1, EVSETemplate2) ||
               (EVSETemplate1 is not null && EVSETemplate1.Equals(EVSETemplate2));

        /// <summary>
        /// Compares two EVSE templates for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (EVSETemplate? EVSETemplate1, EVSETemplate? EVSETemplate2)

            => !(EVSETemplate1 == EVSETemplate2);

        /// <inheritdoc/>
        public Int32 CompareTo(Object? Object)

            => Object is EVSETemplate evseTemplate
                   ? CompareTo(evseTemplate)
                   : throw new ArgumentException("The given object is not an EVSE template!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(EVSETemplate? EVSETemplate)

            => EVSETemplate is null
                   ? throw new ArgumentNullException(nameof(EVSETemplate), "The given EVSE template must not be null!")
                   : Id.CompareTo(EVSETemplate.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is EVSETemplate evseTemplate &&
               Equals(evseTemplate);

        /// <inheritdoc/>
        public Boolean Equals(EVSETemplate? EVSETemplate)

            => EVSETemplate is not null &&
               Id.Equals(EVSETemplate.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
