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
    /// A template of charging pools of one charging station operator, embedded completely in it: what the charging pools
    /// naming it in their templateIds share, e.g. the hotline, the payment options and the facilities of all pools of a site
    /// partner, and a charging tariff for their EVSEs. The pools take it at runtime, as the effective charging pools of their
    /// operator: a value of their own wins, else the one of the first of their templates giving one; arrays are united.
    /// </summary>
    public sealed class ChargingPoolTemplate : APOITemplate<ChargingPoolTemplate_Id>,
                                               IEquatable<ChargingPoolTemplate>,
                                               IComparable<ChargingPoolTemplate>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/chargingPoolTemplate/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its identification.
            /// </summary>
            public const String  Id                        = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context                   = "@context";

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name                      = APOITemplate<ChargingPoolTemplate_Id>.BasePropertyKeys.Name;

            /// <summary>
            /// The property of its multilingual description.
            /// </summary>
            public const String  Description               = APOITemplate<ChargingPoolTemplate_Id>.BasePropertyKeys.Description;

            /// <summary>
            /// The property of its data source.
            /// </summary>
            public const String  DataSource                = APOITemplate<ChargingPoolTemplate_Id>.BasePropertyKeys.DataSource;

            /// <summary>
            /// The property of the identification of the charging tariff of the EVSEs of its members.
            /// </summary>
            public const String  ChargingTariffId          = "chargingTariffId";

            /// <summary>
            /// The property of its data licenses.
            /// </summary>
            public const String  DataLicenses              = "dataLicenses";

            /// <summary>
            /// The property of the identifications of its data licenses.
            /// </summary>
            public const String  DataLicenseIds            = "dataLicenseIds";

            /// <summary>
            /// The property of the address of its members.
            /// </summary>
            public const String  Address                   = ChargingPool.PropertyKeys.Address;

            /// <summary>
            /// The property of the geographical location of its members.
            /// </summary>
            public const String  GeoLocation               = ChargingPool.PropertyKeys.GeoLocation;

            /// <summary>
            /// The property of the type of parking of its members.
            /// </summary>
            public const String  ParkingType               = ChargingPool.PropertyKeys.ParkingType;

            /// <summary>
            /// The property of the accessibility of its members.
            /// </summary>
            public const String  Accessibility             = ChargingPool.PropertyKeys.Accessibility;

            /// <summary>
            /// The property of the hotline phone number of its members.
            /// </summary>
            public const String  HotlinePhoneNumber        = ChargingPool.PropertyKeys.HotlinePhoneNumber;

            /// <summary>
            /// The property of the identification of the grid connection point of its members.
            /// </summary>
            public const String  GridConnectionPointId     = ChargingPool.PropertyKeys.GridConnectionPointId;

            /// <summary>
            /// The property of the brands of its members.
            /// </summary>
            public const String  Brands                    = ChargingPool.PropertyKeys.Brands;

            /// <summary>
            /// The property of the identifications of the brands of its members.
            /// </summary>
            public const String  BrandIds                  = ChargingPool.PropertyKeys.BrandIds;

            /// <summary>
            /// The property of the opening times of its members.
            /// </summary>
            public const String  OpeningTimes              = ChargingPool.PropertyKeys.OpeningTimes;

            /// <summary>
            /// The property of the energy mix of its members.
            /// </summary>
            public const String  EnergyMix                 = ChargingPool.PropertyKeys.EnergyMix;

            /// <summary>
            /// The property of the images of its members.
            /// </summary>
            public const String  Images                    = ChargingPool.PropertyKeys.Images;

            /// <summary>
            /// The property of the time zone of its members.
            /// </summary>
            public const String  TimeZone                  = ChargingPool.PropertyKeys.TimeZone;

            /// <summary>
            /// The property of the facilities of its members.
            /// </summary>
            public const String  Facilities                = ChargingPool.PropertyKeys.Facilities;

            /// <summary>
            /// The property of the payment options of its members.
            /// </summary>
            public const String  PaymentOptions            = ChargingPool.PropertyKeys.PaymentOptions;

            /// <summary>
            /// The property of the authentication modes of its members.
            /// </summary>
            public const String  AuthenticationModes       = ChargingPool.PropertyKeys.AuthenticationModes;

            /// <summary>
            /// The property of the user interface features of its members.
            /// </summary>
            public const String  UIFeatures                = ChargingPool.PropertyKeys.UIFeatures;

            /// <summary>
            /// The property of the features of its members.
            /// </summary>
            public const String  Features                  = ChargingPool.PropertyKeys.Features;

            /// <summary>
            /// The property of the location languages of its members.
            /// </summary>
            public const String  LocationLanguages         = ChargingPool.PropertyKeys.LocationLanguages;

            /// <summary>
            /// The property of the address of the entrance of its members.
            /// </summary>
            public const String  EntranceAddress           = ChargingPool.PropertyKeys.EntranceAddress;

            /// <summary>
            /// The property of the location of the entrance of its members.
            /// </summary>
            public const String  EntranceLocation          = ChargingPool.PropertyKeys.EntranceLocation;

            /// <summary>
            /// The property of the address of the exit of its members.
            /// </summary>
            public const String  ExitAddress               = ChargingPool.PropertyKeys.ExitAddress;

            /// <summary>
            /// The property of the location of the exit of its members.
            /// </summary>
            public const String  ExitLocation              = ChargingPool.PropertyKeys.ExitLocation;

            /// <summary>
            /// The property of the arrival instructions of its members.
            /// </summary>
            public const String  ArrivalInstructions       = ChargingPool.PropertyKeys.ArrivalInstructions;

            /// <summary>
            /// The property of whether its members charge while closed.
            /// </summary>
            public const String  ChargingWhenClosed        = ChargingPool.PropertyKeys.ChargingWhenClosed;

            /// <summary>
            /// The property of the parking restrictions of its members.
            /// </summary>
            public const String  ParkingRestrictions       = ChargingPool.PropertyKeys.ParkingRestrictions;

            /// <summary>
            /// The property of the related locations of its members.
            /// </summary>
            public const String  RelatedLocations          = ChargingPool.PropertyKeys.RelatedLocations;

            /// <summary>
            /// The property of the services of its members.
            /// </summary>
            public const String  Services                  = ChargingPool.PropertyKeys.Services;

            /// <summary>
            /// The property of the details of the assistance service of its members.
            /// </summary>
            public const String  AssistanceServiceDetails  = ChargingPool.PropertyKeys.AssistanceServiceDetails;

            /// <summary>
            /// The property of the standards of its members.
            /// </summary>
            public const String  Standards                 = ChargingPool.PropertyKeys.Standards;

            /// <summary>
            /// The property of the NUTS codes of its members.
            /// </summary>
            public const String  NUTSCodes                 = ChargingPool.PropertyKeys.NUTSCodes;

            /// <summary>
            /// The property of its creation timestamp.
            /// </summary>
            public const String  Created                   = "created";

            /// <summary>
            /// The property of its last change timestamp.
            /// </summary>
            public const String  LastChange                = "lastChange";

            /// <summary>
            /// The property of its custom data.
            /// </summary>
            public const String  CustomData                = "customData";

        }

        #endregion


        /// <summary>
        /// The charging tariff of its operator for the EVSEs of its members without tariffs of their own, unless more
        /// specific templates name one: EVSE templates before charging station templates before charging pool templates,
        /// of one level the first one of a member naming one (ChargingStationOperator.EffectiveEVSE(...)).
        /// </summary>
        public ChargingTariff_Id?               ChargingTariffId  { get; }

        /// <summary>
        /// The address of its members, unless they have their own.
        /// </summary>
        public Address?                                  Address                   { get; }

        /// <summary>
        /// The geographical location of its members, unless they have their own.
        /// </summary>
        public GeoCoordinate?                            GeoLocation               { get; }

        /// <summary>
        /// The type of parking of its members, unless they tell their own.
        /// </summary>
        public ParkingType?                              ParkingType               { get; }

        /// <summary>
        /// Who may use its members, unless they tell it themselves.
        /// </summary>
        public AccessibilityType?                        Accessibility             { get; }

        /// <summary>
        /// The hotline phone number of its members, unless they have their own.
        /// </summary>
        public PhoneNumber?                              HotlinePhoneNumber        { get; }

        /// <summary>
        /// Data licenses of its members, besides their own, ordered.
        /// </summary>
        public ImmutableArray<DataLicense>               DataLicenses              { get; }

        /// <summary>
        /// The grid connection point of its members, one of its roaming network, unless they have their own.
        /// </summary>
        public GridConnectionPoint_Id?                   GridConnectionPointId     { get; }

        /// <summary>
        /// Brands its members also appear under, besides their own, in the order of their identifications.
        /// </summary>
        public POICollection<Brand_Id, Brand>            Brands                    { get; }

        /// <summary>
        /// When its members are open, unless they have their own.
        /// </summary>
        public ImmutableOpeningTimes?                    OpeningTimes              { get; }

        /// <summary>
        /// The energy mix of its members, unless they have their own.
        /// </summary>
        public EnergyMix?                                EnergyMix                 { get; }

        /// <summary>
        /// Images of its members, besides their own, in their given order.
        /// </summary>
        public ImmutableArray<Image>                     Images                    { get; }

        /// <summary>
        /// The time zone of its members, unless they have their own.
        /// </summary>
        public Time_Zone?                                TimeZone                  { get; }

        /// <summary>
        /// Facilities of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<Facility>                  Facilities                { get; }

        /// <summary>
        /// Payment options of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<PaymentOptions>            PaymentOptions            { get; }

        /// <summary>
        /// Authentication modes of its members, besides their own, in the order of their types; of the same type their own.
        /// </summary>
        public ImmutableArray<AuthenticationMode>        AuthenticationModes       { get; }

        /// <summary>
        /// User interface features of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<UIFeatures>                UIFeatures                { get; }

        /// <summary>
        /// Features of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<ChargingPoolFeature>       Features                  { get; }

        /// <summary>
        /// Languages of its members, after their own, in the given order.
        /// </summary>
        public ImmutableArray<Languages>                 LocationLanguages         { get; }

        /// <summary>
        /// The address of the entrance of its members, unless they have their own.
        /// </summary>
        public Address?                                  EntranceAddress           { get; }

        /// <summary>
        /// The location of the entrance of its members, unless they have their own.
        /// </summary>
        public GeoCoordinate?                            EntranceLocation          { get; }

        /// <summary>
        /// The address of the exit of its members, unless they have their own.
        /// </summary>
        public Address?                                  ExitAddress               { get; }

        /// <summary>
        /// The location of the exit of its members, unless they have their own.
        /// </summary>
        public GeoCoordinate?                            ExitLocation              { get; }

        /// <summary>
        /// The arrival instructions of its members, unless they have their own; empty gives none.
        /// </summary>
        public ImmutableI18NString                       ArrivalInstructions       { get; }

        /// <summary>
        /// Whether its members charge while they are closed, unless they tell it themselves.
        /// </summary>
        public Boolean?                                  ChargingWhenClosed        { get; }

        /// <summary>
        /// Parking restrictions of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<ParkingRestrictionGroup>   ParkingRestrictions       { get; }

        /// <summary>
        /// Related locations of its members, besides their own, in their given order.
        /// </summary>
        public ImmutableArray<AdditionalGeoLocation>     RelatedLocations          { get; }

        /// <summary>
        /// Services of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<LocationService>           Services                  { get; }

        /// <summary>
        /// The details of the assistance service of its members, unless they have their own; empty gives none.
        /// </summary>
        public ImmutableI18NString                       AssistanceServiceDetails  { get; }

        /// <summary>
        /// Standards of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<Standard>                  Standards                 { get; }

        /// <summary>
        /// NUTS codes of its members, besides their own, in the order of their codes.
        /// </summary>
        public ImmutableArray<NUTSCode>                  NUTSCodes                 { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging pool template.
        /// </summary>
        public ChargingPoolTemplate(ChargingPoolTemplate_Id                             Id,
                                 I18NString?                                      Name                         = null,
                                 I18NString?                                      Description                  = null,
                                 ChargingTariff_Id?                               ChargingTariffId             = null,

                                 Address?                                         Address                      = null,
                                 GeoCoordinate?                                   GeoLocation                  = null,
                                 ParkingType?                                     ParkingType                  = null,
                                 AccessibilityType?                               Accessibility                = null,
                                 PhoneNumber?                                     HotlinePhoneNumber           = null,
                                 IEnumerable<DataLicense>?                        DataLicenses                 = null,
                                 GridConnectionPoint_Id?                          GridConnectionPointId        = null,
                                 IEnumerable<Brand>?                              Brands                       = null,
                                 ImmutableOpeningTimes?                           OpeningTimes                 = null,
                                 EnergyMix?                                       EnergyMix                    = null,
                                 IEnumerable<Image>?                              Images                       = null,
                                 Time_Zone?                                       TimeZone                     = null,
                                 IEnumerable<Facility>?                           Facilities                   = null,
                                 IEnumerable<PaymentOptions>?                     PaymentOptions               = null,
                                 IEnumerable<AuthenticationMode>?                 AuthenticationModes          = null,
                                 IEnumerable<UIFeatures>?                         UIFeatures                   = null,
                                 IEnumerable<ChargingPoolFeature>?                Features                     = null,
                                 IEnumerable<Languages>?                          LocationLanguages            = null,
                                 Address?                                         EntranceAddress              = null,
                                 GeoCoordinate?                                   EntranceLocation             = null,
                                 Address?                                         ExitAddress                  = null,
                                 GeoCoordinate?                                   ExitLocation                 = null,
                                 I18NString?                                      ArrivalInstructions          = null,
                                 Boolean?                                         ChargingWhenClosed           = null,
                                 IEnumerable<ParkingRestrictionGroup>?            ParkingRestrictions          = null,
                                 IEnumerable<AdditionalGeoLocation>?              RelatedLocations             = null,
                                 IEnumerable<LocationService>?                    Services                     = null,
                                 I18NString?                                      AssistanceServiceDetails     = null,
                                 IEnumerable<Standard>?                           Standards                    = null,
                                 IEnumerable<NUTSCode>?                           NUTSCodes                    = null,

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

            var dataLicenses = (DataLicenses ?? []).ToArray();

            if (dataLicenses.Any(dataLicense => dataLicense is null))
                throw new ArgumentException("Data licenses must not contain null entries!", nameof(DataLicenses));

            if (dataLicenses.Select(dataLicense => dataLicense.Id).Distinct().Count() != dataLicenses.Length)
                throw new ArgumentException("A data license must not be given twice!", nameof(DataLicenses));
            this.ChargingTariffId          = ChargingTariffId;

            this.Address                   = Address;
            this.GeoLocation               = GeoLocation;
            this.ParkingType               = ParkingType;
            this.Accessibility             = Accessibility;
            this.HotlinePhoneNumber        = HotlinePhoneNumber;
            this.DataLicenses              = [.. dataLicenses.Ordered()];
            this.GridConnectionPointId     = GridConnectionPointId;
            this.Brands                    = new (Brands, brand => brand.Id, "brand", $"the charging pool template '{Id}'");
            this.OpeningTimes              = OpeningTimes;
            this.EnergyMix                 = EnergyMix;
            this.Images                    = POIJSON.Distinct  (Images,              image => image.URL.ToString(), $"image of the charging pool template '{Id}'");
            this.TimeZone                  = TimeZone;
            this.Facilities                = POIJSON.OrderedIds(Facilities,          $"facility of the charging pool template '{Id}'");
            this.PaymentOptions            = POIJSON.OrderedIds(PaymentOptions,      $"payment option of the charging pool template '{Id}'");
            this.AuthenticationModes       = [.. POIJSON.Distinct(AuthenticationModes, authenticationMode => authenticationMode.Type, $"authentication mode of the charging pool template '{Id}'").
                                                     OrderBy(authenticationMode => authenticationMode.Type, StringComparer.Ordinal)];
            this.UIFeatures                = POIJSON.OrderedIds(UIFeatures,          $"user interface feature of the charging pool template '{Id}'");
            this.Features                  = POIJSON.OrderedIds(Features,            $"feature of the charging pool template '{Id}'");
            this.LocationLanguages         = POIJSON.UniqueIds (LocationLanguages,   $"language of the charging pool template '{Id}'");
            this.EntranceAddress           = EntranceAddress;
            this.EntranceLocation          = EntranceLocation;
            this.ExitAddress               = ExitAddress;
            this.ExitLocation              = ExitLocation;
            this.ArrivalInstructions       = ArrivalInstructions ?? ImmutableI18NString.Empty;
            this.ChargingWhenClosed        = ChargingWhenClosed;
            this.ParkingRestrictions       = POIJSON.OrderedIds(ParkingRestrictions, $"parking restriction of the charging pool template '{Id}'");
            this.RelatedLocations          = POIJSON.Distinct  (RelatedLocations,    relatedLocation => relatedLocation.ToJSON(Embedded: true).ToString(Newtonsoft.Json.Formatting.None), $"related location of the charging pool template '{Id}'");
            this.Services                  = POIJSON.OrderedIds(Services,            $"service of the charging pool template '{Id}'");
            this.AssistanceServiceDetails  = AssistanceServiceDetails ?? ImmutableI18NString.Empty;
            this.Standards                 = POIJSON.OrderedIds(Standards,           $"standard of the charging pool template '{Id}'");
            this.NUTSCodes                 = POIJSON.OrderedIds(NUTSCodes,           $"NUTS code of the charging pool template '{Id}'");

        }

        #endregion

        #region ToJSON(Embedded = false)

        /// <inheritdoc/>
        public override JObject ToJSON(Boolean Embedded = false)

            => ToJSON(Embedded,
                      CustomChargingPoolTemplateSerializer: null);

        #endregion

        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return the JSON representation of this charging pool template; embedded, as its charging station operator embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomChargingPoolTemplateSerializer">An optional delegate to customize the JSON representation.</param>
        public JObject ToJSON(Boolean                                                 Embedded                               = false,
                              CustomJObjectSerializerDelegate<ChargingPoolTemplate>?  CustomChargingPoolTemplateSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Id, Id.ToString())
                       );

            if (!Embedded)
                json.Add(PropertyKeys.Context,       JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Name,         Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Description,  Description.ToJSON());

            if (DataSource is not null)
                json.Add(PropertyKeys.DataSource,   DataSource);

            if (ChargingTariffId.HasValue)
                json.Add(PropertyKeys.ChargingTariffId,  ChargingTariffId.Value.ToString());

            // The properties of a charging pool, as a charging pool writes them.
            if (DataLicenses.Length > 0)
                json.Add(PropertyKeys.DataLicenses,              DataLicenses.ToPOIJSON());

            if (Address is not null)
                json.Add(PropertyKeys.Address,                   Address.ToJSON(Embedded: true));

            if (GeoLocation.HasValue)
                json.Add(PropertyKeys.GeoLocation,               POIJSON.GeoCoordinateJSON(GeoLocation.Value));

            if (ParkingType.HasValue)
                json.Add(PropertyKeys.ParkingType,               ParkingType.Value.ToString());

            if (Accessibility.HasValue)
                json.Add(PropertyKeys.Accessibility,             Accessibility.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add(PropertyKeys.HotlinePhoneNumber,                   HotlinePhoneNumber.Value.ToString());

            if (GridConnectionPointId.HasValue)
                json.Add(PropertyKeys.GridConnectionPointId,     GridConnectionPointId.Value.ToString());

            if (Brands.Count > 0)
                json.Add(PropertyKeys.Brands,                    new JArray(Brands.Select(brand => brand.ToJSON(Embedded: true))));

            if (OpeningTimes is not null)
                json.Add(PropertyKeys.OpeningTimes,              OpeningTimes.ToJSON());

            if (EnergyMix is not null)
                json.Add(PropertyKeys.EnergyMix,                 EnergyMix.ToJSON(Embedded: true));

            if (Images.Length > 0)
                json.Add(PropertyKeys.Images,                    new JArray(Images.Select(image => image.ToJSON(Embedded: true))));

            if (TimeZone.HasValue)
                json.Add(PropertyKeys.TimeZone,                  TimeZone.Value.ToString());

            if (Facilities.Length > 0)
                json.Add(PropertyKeys.Facilities,                POIJSON.IdsToJSON(Facilities));

            if (PaymentOptions.Length > 0)
                json.Add(PropertyKeys.PaymentOptions,            POIJSON.IdsToJSON(PaymentOptions));

            if (AuthenticationModes.Length > 0)
                json.Add(PropertyKeys.AuthenticationModes,       new JArray(AuthenticationModes.Select(authenticationMode => authenticationMode.ToJSON(Embedded: true))));

            if (UIFeatures.Length > 0)
                json.Add(PropertyKeys.UIFeatures,                POIJSON.IdsToJSON(UIFeatures));

            if (Features.Length > 0)
                json.Add(PropertyKeys.Features,                  POIJSON.IdsToJSON(Features));

            if (LocationLanguages.Length > 0)
                json.Add(PropertyKeys.LocationLanguages,         POIJSON.IdsToJSON(LocationLanguages));

            if (EntranceAddress is not null)
                json.Add(PropertyKeys.EntranceAddress,           EntranceAddress.ToJSON(Embedded: true));

            if (EntranceLocation.HasValue)
                json.Add(PropertyKeys.EntranceLocation,          POIJSON.GeoCoordinateJSON(EntranceLocation.Value));

            if (ExitAddress is not null)
                json.Add(PropertyKeys.ExitAddress,               ExitAddress.ToJSON(Embedded: true));

            if (ExitLocation.HasValue)
                json.Add(PropertyKeys.ExitLocation,              POIJSON.GeoCoordinateJSON(ExitLocation.Value));

            if (ArrivalInstructions.IsNotNullOrEmpty())
                json.Add(PropertyKeys.ArrivalInstructions,       ArrivalInstructions.ToJSON());

            if (ChargingWhenClosed.HasValue)
                json.Add(PropertyKeys.ChargingWhenClosed,        ChargingWhenClosed.Value);

            if (ParkingRestrictions.Length > 0)
                json.Add(PropertyKeys.ParkingRestrictions,       POIJSON.IdsToJSON(ParkingRestrictions));

            if (RelatedLocations.Length > 0)
                json.Add(PropertyKeys.RelatedLocations,          new JArray(RelatedLocations.Select(relatedLocation => relatedLocation.ToJSON(Embedded: true))));

            if (Services.Length > 0)
                json.Add(PropertyKeys.Services,                  POIJSON.IdsToJSON(Services));

            if (AssistanceServiceDetails.IsNotNullOrEmpty())
                json.Add(PropertyKeys.AssistanceServiceDetails,  AssistanceServiceDetails.ToJSON());

            if (Standards.Length > 0)
                json.Add(PropertyKeys.Standards,                 POIJSON.IdsToJSON(Standards));

            if (NUTSCodes.Length > 0)
                json.Add(PropertyKeys.NUTSCodes,                 POIJSON.IdsToJSON(NUTSCodes));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return CustomChargingPoolTemplateSerializer is not null
                       ? CustomChargingPoolTemplateSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingPoolTemplate, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging pool template.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingPoolTemplate">The parsed charging pool template.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out ChargingPoolTemplate?   ChargingPoolTemplate,
                                       [NotNullWhen(false)] out String?              ErrorResponse)
        {

            ChargingPoolTemplate = null;

            try
            {

                if (!JSON.ParseMandatory(PropertyKeys.Id,
                                         "charging pool template identification",
                                         ChargingPoolTemplate_Id.TryParse,
                                         out ChargingPoolTemplate_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging pool template '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, PropertyKeys.Name,              "charging pool template name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, PropertyKeys.Description,       "charging pool template description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, PropertyKeys.DataSource,        "data source",                    out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, PropertyKeys.ChargingTariffId,  "charging tariff identification",  ChargingTariff_Id.TryParse, out ChargingTariff_Id? chargingTariffId, out ErrorResponse))
                {
                    return false;
                }

                // The properties of a charging pool, as a charging pool reads them.
                if (!POIJSON.TryParseAddress    (JSON, PropertyKeys.Address,             "address",                    out var address,      out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON, PropertyKeys.GeoLocation,         "geo location",               out var geoLocation,  out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, PropertyKeys.ParkingType,         "parking type",               POI.ParkingType.      TryParse, out ParkingType?        parkingType,    out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, PropertyKeys.Accessibility,       "accessibility",              POI.AccessibilityType.TryParse, out AccessibilityType?  accessibility,  out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, PropertyKeys.HotlinePhoneNumber,  "hotline phone number",       PhoneNumber.          TryParse, out PhoneNumber?        hotline,        out ErrorResponse))
                {
                    return false;
                }

                if (!POIDataLicenses.TryParse(JSON[PropertyKeys.DataLicenses],
                                              JSON[PropertyKeys.DataLicenseIds],
                                              out var dataLicenses,
                                              out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseText(JSON, PropertyKeys.GridConnectionPointId,  "grid connection point identification", GridConnectionPoint_Id.TryParse, out GridConnectionPoint_Id? gridConnectionPointId, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseChildren<Brand>(JSON, PropertyKeys.Brands, PropertyKeys.BrandIds, "brands", Brand.TryParse, out var brands, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObject<ImmutableOpeningTimes>(JSON, PropertyKeys.OpeningTimes, "opening times", ImmutableOpeningTimes.TryParse, out var openingTimes, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObject<EnergyMix>(JSON, PropertyKeys.EnergyMix, "energy mix", (JObject json, out EnergyMix? energyMix, out String? error) => POI.EnergyMix.TryParse(json, out energyMix, out error), out var energyMix, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObjects<Image>(JSON, PropertyKeys.Images, "images", Image.TryParse, out var images, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseText       (JSON, PropertyKeys.TimeZone,                  "time zone",               Time_Zone.TryParse,                       out Time_Zone? timeZone,                                        out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.Facilities,                "facility",                Facility.TryParse,                        out ImmutableArray<Facility>                facilities,           out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.PaymentOptions,            "payment option",          POI.PaymentOptions.TryParse,              out ImmutableArray<POI.PaymentOptions>      paymentOptions,       out ErrorResponse) ||
                    !POIJSON.TryParseObjects    (JSON, PropertyKeys.AuthenticationModes,       "authentication mode",     AuthenticationMode.TryParse,              out ImmutableArray<AuthenticationMode>      authenticationModes,  out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.UIFeatures,                "user interface feature",  POI.UIFeatures.TryParse,                  out ImmutableArray<POI.UIFeatures>          uiFeatures,           out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.Features,                  "charging pool feature",   ChargingPoolFeature.TryParse,             out ImmutableArray<ChargingPoolFeature>     features,             out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.LocationLanguages,         "location language",       POIJSON.TryParseName<Languages>,          out ImmutableArray<Languages>               locationLanguages,    out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON, PropertyKeys.EntranceAddress,           "entrance address",                                                  out var entranceAddress,                                        out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON, PropertyKeys.EntranceLocation,          "entrance location",                                                 out var entranceLocation,                                       out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON, PropertyKeys.ExitAddress,               "exit address",                                                      out var exitAddress,                                            out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON, PropertyKeys.ExitLocation,              "exit location",                                                     out var exitLocation,                                           out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, PropertyKeys.ArrivalInstructions,       "arrival instructions",                                              out var arrivalInstructions,                                    out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.ParkingRestrictions,       "parking restriction",     ParkingRestrictionGroup.TryParse,         out ImmutableArray<ParkingRestrictionGroup> parkingRestrictions,  out ErrorResponse) ||
                    !POIJSON.TryParseObjects    (JSON, PropertyKeys.RelatedLocations,          "related location",        AdditionalGeoLocation.TryParse,           out ImmutableArray<AdditionalGeoLocation>   relatedLocations,     out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.Services,                  "location service",        LocationService.TryParse,                 out ImmutableArray<LocationService>         services,             out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, PropertyKeys.AssistanceServiceDetails,  "assistance service details",                                    out var assistanceServiceDetails,                               out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.Standards,                 "standard",                Standard.TryParse,                        out ImmutableArray<Standard>                standards,            out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.NUTSCodes,                 "NUTS code",               NUTSCode.TryParse,                        out ImmutableArray<NUTSCode>                nutsCodes,            out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional(PropertyKeys.ChargingWhenClosed, "charging when closed", out Boolean? chargingWhenClosed, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                ChargingPoolTemplate = new ChargingPoolTemplate(id,
                                                          name,
                                                          description,
                                                          chargingTariffId,
                                                          address,
                                                          geoLocation,
                                                          parkingType,
                                                          accessibility,
                                                          hotline,
                                                          dataLicenses,
                                                          gridConnectionPointId,
                                                          brands,
                                                          openingTimes,
                                                          energyMix,
                                                          images,
                                                          timeZone,
                                                          facilities,
                                                          paymentOptions,
                                                          authenticationModes,
                                                          uiFeatures,
                                                          features,
                                                          locationLanguages,
                                                          entranceAddress,
                                                          entranceLocation,
                                                          exitAddress,
                                                          exitLocation,
                                                          arrivalInstructions,
                                                          chargingWhenClosed,
                                                          parkingRestrictions,
                                                          relatedLocations,
                                                          services,
                                                          assistanceServiceDetails,
                                                          standards,
                                                          nutsCodes,
                                                          DataSource:  dataSource,
                                                          Created:     created,
                                                          LastChange:  lastChange,
                                                          CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingPoolTemplate  = null;
                ErrorResponse      = "The given JSON representation of a charging pool template is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two charging pool templates for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ChargingPoolTemplate? ChargingPoolTemplate1, ChargingPoolTemplate? ChargingPoolTemplate2)

            => ReferenceEquals(ChargingPoolTemplate1, ChargingPoolTemplate2) ||
               (ChargingPoolTemplate1 is not null && ChargingPoolTemplate1.Equals(ChargingPoolTemplate2));

        /// <summary>
        /// Compares two charging pool templates for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ChargingPoolTemplate? ChargingPoolTemplate1, ChargingPoolTemplate? ChargingPoolTemplate2)

            => !(ChargingPoolTemplate1 == ChargingPoolTemplate2);

        /// <inheritdoc/>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingPoolTemplate chargingPoolTemplate
                   ? CompareTo(chargingPoolTemplate)
                   : throw new ArgumentException("The given object is not a charging pool template!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ChargingPoolTemplate? ChargingPoolTemplate)

            => ChargingPoolTemplate is null
                   ? throw new ArgumentNullException(nameof(ChargingPoolTemplate), "The given charging pool template must not be null!")
                   : Id.CompareTo(ChargingPoolTemplate.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingPoolTemplate chargingPoolTemplate &&
               Equals(chargingPoolTemplate);

        /// <inheritdoc/>
        public Boolean Equals(ChargingPoolTemplate? ChargingPoolTemplate)

            => ChargingPoolTemplate is not null &&
               Id.Equals(ChargingPoolTemplate.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
