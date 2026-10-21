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

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.Mail;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public sealed partial class ChargingStationOperator
    {

        #region (private) delegate TextParser<T>(Text, out Value)

        private delegate Boolean TextParser<T>(String Text, out T Value);

        #endregion


        #region Parse    (JSON, ...)

        /// <summary>
        /// Parse the given JSON representation of a charging station operator.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="CustomChargingStationOperatorParser">An optional delegate to parse custom charging station operator JSON objects.</param>
        public static ChargingStationOperator Parse(JObject                                                JSON,
                                                    CustomJObjectParserDelegate<ChargingStationOperator>?  CustomChargingStationOperatorParser   = null)
        {

            if (TryParse(JSON,
                         out var chargingStationOperator,
                         out var errorResponse,
                         CustomChargingStationOperatorParser))
            {
                return chargingStationOperator;
            }

            throw new ArgumentException("The given JSON representation of a charging station operator is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region TryParse (JSON, out ChargingStationOperator, out ErrorResponse, ...)

        /// <summary>
        /// Try to parse the given JSON representation of a charging station operator.
        /// A reference to its roaming network is not read: the operator is read within it.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingStationOperator">The parsed charging station operator.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomChargingStationOperatorParser">An optional delegate to parse custom charging station operator JSON objects.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out ChargingStationOperator?      ChargingStationOperator,
                                       [NotNullWhen(false)] out String?                       ErrorResponse,
                                       CustomJObjectParserDelegate<ChargingStationOperator>?  CustomChargingStationOperatorParser   = null)

            => TryParse(JSON,
                        true,
                        out ChargingStationOperator,
                        out ErrorResponse,
                        CustomChargingStationOperatorParser);

        #endregion

        #region (internal) TryParseWithinChangeSet(JSON, out ChargingStationOperator, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging station operator a change of a change set made,
        /// without checking the members of its groups, the groups of its charging tariff offers and its offers not
        /// overlapping: the change set checks them after its last change (see InvalidGroupOrOffer()).
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingStationOperator">The parsed charging station operator.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        internal static Boolean TryParseWithinChangeSet(JObject                                            JSON,
                                                        [NotNullWhen(true)]  out ChargingStationOperator?  ChargingStationOperator,
                                                        [NotNullWhen(false)] out String?                   ErrorResponse)

            => TryParse(JSON,
                        false,
                        out ChargingStationOperator,
                        out ErrorResponse,
                        null);

        #endregion

        #region (private) TryParse (JSON, CheckGroupsAndOffers, out ChargingStationOperator, out ErrorResponse, ...)

        /// <summary>
        /// Try to parse the given JSON representation of a charging station operator, checking the members of its groups,
        /// the groups of its charging tariff offers and its offers not overlapping only when told so.
        /// </summary>
        private static Boolean TryParse(JObject                                                JSON,
                                        Boolean                                                CheckGroupsAndOffers,
                                        [NotNullWhen(true)]  out ChargingStationOperator?      ChargingStationOperator,
                                        [NotNullWhen(false)] out String?                       ErrorResponse,
                                        CustomJObjectParserDelegate<ChargingStationOperator>?  CustomChargingStationOperatorParser)
        {

            ChargingStationOperator = null;

            try
            {

                if (JSON?.HasValues != true)
                {
                    ErrorResponse = "The given JSON object must not be null or empty!";
                    return false;
                }

                #region Parse Id                       [mandatory]

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "charging station operator identification",
                                         ChargingStationOperator_Id.TryParse,
                                         out ChargingStationOperator_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Check JSON-LD context           [optional]

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging station operator '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                #endregion

                #region Parse Name                     [optional]

                if (JSON.ParseOptionalJSON(BasePropertyKeys.Name,
                                           "operator name",
                                           ImmutableI18NString.TryParse,
                                           out ImmutableI18NString? name,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse Description              [optional]

                if (JSON.ParseOptionalJSON(BasePropertyKeys.Description,
                                           "operator description",
                                           ImmutableI18NString.TryParse,
                                           out ImmutableI18NString? description,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse DataSource               [optional]

                if (JSON.ParseOptional(BasePropertyKeys.DataSource,
                                       "data source",
                                       out String? dataSource,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse DataLicenses             [optional]

                if (!POIDataLicenses.TryParse(JSON[BasePropertyKeys.DataLicenses],
                                              JSON[BasePropertyKeys.DataLicenseIds],
                                              out var dataLicenses,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse Address                  [optional]

                Address? address = null;

                if (JSON[PropertyKeys.Address] is JToken addressJSON)
                {

                    if (addressJSON is not JObject addressObject)
                    {
                        ErrorResponse = "The address must be a JSON object!";
                        return false;
                    }

                    if (!Address.TryParse(addressObject, out address, out var addressError))
                    {
                        ErrorResponse = "Invalid address: " + addressError;
                        return false;
                    }

                }

                #endregion

                #region Parse GeoLocation              [optional]

                GeoCoordinate? geoLocation = null;

                if (JSON[PropertyKeys.GeoLocation] is JToken geoLocationJSON)
                {

                    if (geoLocationJSON is not JObject geoLocationObject)
                    {
                        ErrorResponse = "The geo location must be a JSON object!";
                        return false;
                    }

                    if (!POIJSON.TryParseGeoCoordinate(geoLocationObject, out var parsedGeoLocation, out var geoLocationError))
                    {
                        ErrorResponse = "Invalid geo location: " + geoLocationError;
                        return false;
                    }

                    geoLocation = parsedGeoLocation;

                }

                #endregion

                #region Parse Logo                     [optional]

                URL? logo = null;

                if (JSON[PropertyKeys.Logos] is JToken logosJSON)
                {

                    if (logosJSON is not JArray logosArray || logosArray.Count > 1 ||
                        (logosArray.Count == 1 && logosArray[0] is not JObject))
                    {
                        ErrorResponse = "The logos must be a JSON array of at most one logo object!";
                        return false;
                    }

                    if (logosArray.Count == 1)
                    {

                        if (!TryParseOptionalText((JObject) logosArray[0], PropertyKeys.LogoURI, "logo URI", URL.TryParse, out logo, out ErrorResponse))
                            return false;

                        if (logo is null)
                        {
                            ErrorResponse = "A logo requires an URI!";
                            return false;
                        }

                    }

                }

                #endregion

                #region Parse Telephone, EMailAddress, Homepage, Hotline, TermsAndConditions [optional]

                if (!TryParseOptionalText(JSON, PropertyKeys.Telephone,           "telephone number",          PhoneNumber.       TryParse, out PhoneNumber?         telephone,             out ErrorResponse) ||
                    !TryParseOptionalText(JSON, PropertyKeys.EMailAddress,        "e-mail address",            SimpleEMailAddress.TryParse, out SimpleEMailAddress?  eMailAddress,          out ErrorResponse) ||
                    !TryParseOptionalText(JSON, PropertyKeys.Homepage,            "homepage",                  URL.               TryParse, out URL?                 homepage,              out ErrorResponse) ||
                    !TryParseOptionalText(JSON, PropertyKeys.HotlinePhoneNumber,  "hotline phone number",      PhoneNumber.       TryParse, out PhoneNumber?         hotlinePhoneNumber,    out ErrorResponse) ||
                    !TryParseOptionalText(JSON, PropertyKeys.TermsAndConditions,  "terms and conditions URL",  URL.               TryParse, out URL?                 termsAndConditionsURL, out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse ChargingPools            [optional]

                if (!POIJSON.TryParseChildren<ChargingPool>(JSON,
                                                            PropertyKeys.ChargingPools,
                                                            PropertyKeys.ChargingPoolIds,
                                                            "charging pools",
                                                            ChargingPool.TryParse,
                                                            out var chargingPools,
                                                            out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse groups                   [optional]

                if (!POIJSON.TryParseChildren<EVSEGroup>(JSON, PropertyKeys.EVSEGroups, PropertyKeys.EVSEGroupIds, "EVSEGroups", EVSEGroup.TryParse, out var evseGroups, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseChildren<ChargingStationGroup>(JSON, PropertyKeys.ChargingStationGroups, PropertyKeys.ChargingStationGroupIds, "ChargingStationGroups", ChargingStationGroup.TryParse, out var chargingStationGroups, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseChildren<ChargingPoolGroup>(JSON, PropertyKeys.ChargingPoolGroups, PropertyKeys.ChargingPoolGroupIds, "ChargingPoolGroups", ChargingPoolGroup.TryParse, out var chargingPoolGroups, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseChildren<ChargingTariffGroup>(JSON, PropertyKeys.ChargingTariffGroups, PropertyKeys.ChargingTariffGroupIds, "ChargingTariffGroups", ChargingTariffGroup.TryParse, out var chargingTariffGroups, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseChildren<EnergyMeterGroup>(JSON, PropertyKeys.EnergyMeterGroups, PropertyKeys.EnergyMeterGroupIds, "energy meter groups", EnergyMeterGroup.TryParse, out var energyMeterGroups, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseChildren<EVSETemplate>           (JSON, PropertyKeys.EVSETemplates,            PropertyKeys.EVSETemplateIds,            "EVSE templates",             EVSETemplate.           TryParse, out var evseTemplates,            out ErrorResponse) ||
                    !POIJSON.TryParseChildren<ChargingStationTemplate>(JSON, PropertyKeys.ChargingStationTemplates, PropertyKeys.ChargingStationTemplateIds, "charging station templates", ChargingStationTemplate.TryParse, out var chargingStationTemplates, out ErrorResponse) ||
                    !POIJSON.TryParseChildren<ChargingPoolTemplate>   (JSON, PropertyKeys.ChargingPoolTemplates,    PropertyKeys.ChargingPoolTemplateIds,    "charging pool templates",    ChargingPoolTemplate.   TryParse, out var chargingPoolTemplates,    out ErrorResponse) ||
                    !POIJSON.TryParseChildren<ChargingTariffTemplate> (JSON, PropertyKeys.ChargingTariffTemplates,  PropertyKeys.ChargingTariffTemplateIds,  "charging tariff templates",  ChargingTariffTemplate. TryParse, out var chargingTariffTemplates,  out ErrorResponse) ||
                    !POIJSON.TryParseChildren<EnergyMeterTemplate>    (JSON, PropertyKeys.EnergyMeterTemplates,     PropertyKeys.EnergyMeterTemplateIds,     "energy meter templates",     EnergyMeterTemplate.    TryParse, out var energyMeterTemplates,     out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<ParkingSpace>(JSON, PropertyKeys.ParkingSpaces, PropertyKeys.ParkingSpaceIds, "parking spaces", ParkingSpace.TryParse, out var parkingSpaces, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseChildren<ChargingTariffOffer>(JSON, PropertyKeys.ChargingTariffOffers, PropertyKeys.ChargingTariffOfferIds, "charging tariff offers", ChargingTariffOffer.TryParse, out var chargingTariffOffers, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseChildren<Brand>(JSON, PropertyKeys.Brands, PropertyKeys.BrandIds, "brands", Brand.TryParse, out var brands, out ErrorResponse))
                    return false;

                #endregion

                #region Parse ChargingTariffs          [optional]

                if (!POIJSON.TryParseChildren<ChargingTariff>(JSON,
                                                              PropertyKeys.ChargingTariffs,
                                                              PropertyKeys.ChargingTariffIds,
                                                              "charging tariffs",
                                                              ChargingTariff.TryParse,
                                                              out var chargingTariffs,
                                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse SigningKeys, IdentityKeys [optional]

                if (!POIPartyKeys.TryParse(JSON, out var signingKeys, out var identityKeys, out ErrorResponse))
                    return false;

                #endregion

                #region Parse Created, LastChange, CustomData [optional]

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion


                ChargingStationOperator = new ChargingStationOperator(
                                              CheckGroupsAndOffers,
                                              id,
                                              name,
                                              description,
                                              logo,
                                              address,
                                              geoLocation,
                                              telephone,
                                              eMailAddress,
                                              homepage,
                                              hotlinePhoneNumber,
                                              termsAndConditionsURL,
                                              dataLicenses,
                                              chargingPools,
                                              chargingTariffs,
                                              EVSEGroups: evseGroups,
                                              ChargingStationGroups: chargingStationGroups,
                                              ChargingPoolGroups: chargingPoolGroups,
                                              ChargingTariffGroups: chargingTariffGroups,
                                              ParkingSpaces:        parkingSpaces,
                                              Brands:               brands,
                                              SigningKeys:  signingKeys,
                                              IdentityKeys: identityKeys,
                                              EnergyMeterGroups:    energyMeterGroups,
                                              EVSETemplates:             evseTemplates,
                                              ChargingStationTemplates:  chargingStationTemplates,
                                              ChargingPoolTemplates:     chargingPoolTemplates,
                                              ChargingTariffTemplates:   chargingTariffTemplates,
                                              EnergyMeterTemplates:      energyMeterTemplates,
                                              ChargingTariffOffers:      chargingTariffOffers,
                                              DataSource:  dataSource,
                                              Created:     created,
                                              LastChange:  lastChange,
                                              CustomData:  customData
                                          );

                if (CustomChargingStationOperatorParser is not null)
                    ChargingStationOperator = CustomChargingStationOperatorParser(JSON,
                                                                                  ChargingStationOperator);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingStationOperator  = null;
                ErrorResponse            = "The given JSON representation of a charging station operator is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Parse    (CBOR)

        /// <summary>
        /// Parse the given CBOR representation of a charging station operator.
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        public static ChargingStationOperator Parse(CBORValue CBOR)
        {

            if (TryParse(CBOR,
                         out var chargingStationOperator,
                         out var errorResponse))
            {
                return chargingStationOperator;
            }

            throw new ArgumentException("The given CBOR representation of a charging station operator is invalid: " + errorResponse,
                                        nameof(CBOR));

        }

        #endregion

        #region TryParse (CBOR, out ChargingStationOperator, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of a charging station operator.
        /// It has the keys and values of the JSON representation.
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="ChargingStationOperator">The parsed charging station operator.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(CBORValue                                          CBOR,
                                       [NotNullWhen(true)]  out ChargingStationOperator?  ChargingStationOperator,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)
        {

            ChargingStationOperator = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = "The CBOR representation of a charging station operator must be a map!";
                    return false;
                }

                if (CBORJSON.ToJSON(CBOR) is not JObject json)
                {
                    ErrorResponse = "The CBOR representation of a charging station operator must be a map with text keys!";
                    return false;
                }

                return TryParse(json,
                                out ChargingStationOperator,
                                out ErrorResponse);

            }
            catch (Exception e)
            {
                ChargingStationOperator  = null;
                ErrorResponse            = "The given CBOR representation of a charging station operator is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToCBOR   (Embedded = false, ...)

        /// <summary>
        /// Return the CBOR representation of this charging station operator:
        /// the keys and values of its complete JSON representation, with its data licenses.
        /// Write it with CBORWriterOptions.Canonical for deterministic bytes.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomChargingStationOperatorSerializer">An optional delegate to customize the CBOR representation.</param>
        public CBORValue ToCBOR(Boolean                                                 Embedded                                  = false,
                                CustomCBORSerializerDelegate<ChargingStationOperator>?  CustomChargingStationOperatorSerializer   = null)
        {

            var cbor = new CBORMap {
                           { CBORValue.FromText(BasePropertyKeys.Id),                    CBORValue.FromText(Id.ToString()) }
                       };

            if (!Embedded)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Context),                   CBORValue.FromText(JSONLDContext));

            cbor.Add(CBORValue.FromText(BasePropertyKeys.Name),                          Name.ToCBOR());

            if (Description.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Description),               Description.ToCBOR());

            if (DataSource.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.DataSource),                CBORValue.FromText(DataSource));

            if (DataLicenses.Any())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.DataLicenses),              DataLicenses.ToPOICBOR());

            // The address and the geo location have no CBOR representation of their own: theirs are the keys and values
            // of their JSON representation.
            if (Address is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.Address),                       CBORJSON.ToCBOR(Address.ToJSON(Embedded: true)));

            if (Logo.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(PropertyKeys.Logos),
                         CBORValue.FromArray(new CBORValue[] {
                             new CBORMap {
                                 { CBORValue.FromText(PropertyKeys.LogoURI),                     CBORValue.FromText(Logo.ToString()) }
                             }
                         }));

            if (GeoLocation.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.GeoLocation),                   CBORJSON.ToCBOR(GeoLocation.Value.ToJSON(Embedded: true)));

            if (Telephone.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Telephone),                     CBORValue.FromText(Telephone.ToString()));

            if (EMailAddress.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.EMailAddress),                  CBORValue.FromText(EMailAddress.ToString()));

            if (Homepage.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Homepage),                      CBORValue.FromText(Homepage.ToString()));

            if (HotlinePhoneNumber.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.HotlinePhoneNumber),            CBORValue.FromText(HotlinePhoneNumber.ToString()));

            if (TermsAndConditionsURL.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.TermsAndConditions),            CBORValue.FromText(TermsAndConditionsURL.ToString()));

            // Its brands, charging tariffs, charging pools, templates, groups, offers and parking spaces have no CBOR
            // representation of their own: theirs are the keys and values of their JSON representation, as embedded.
            AddChildren(cbor, PropertyKeys.Brands,                    Brands,                    brand          => brand.         ToJSON(Embedded: true));
            AddChildren(cbor, PropertyKeys.ChargingTariffs,           ChargingTariffs,           chargingTariff => chargingTariff.ToJSON(Embedded: true));
            AddChildren(cbor, PropertyKeys.ChargingPools,             ChargingPools,             chargingPool   => chargingPool.  ToJSON(Embedded: true));
            AddChildren(cbor, PropertyKeys.EVSETemplates,             EVSETemplates,             template       => template.      ToJSON(Embedded: true));
            AddChildren(cbor, PropertyKeys.ChargingStationTemplates,  ChargingStationTemplates,  template       => template.      ToJSON(Embedded: true));
            AddChildren(cbor, PropertyKeys.ChargingPoolTemplates,     ChargingPoolTemplates,     template       => template.      ToJSON(Embedded: true));
            AddChildren(cbor, PropertyKeys.ChargingTariffTemplates,   ChargingTariffTemplates,   template       => template.      ToJSON(Embedded: true));
            AddChildren(cbor, PropertyKeys.EnergyMeterTemplates,      EnergyMeterTemplates,      template       => template.      ToJSON(Embedded: true));
            AddChildren(cbor, PropertyKeys.EVSEGroups,                EVSEGroups,                group          => group.         ToJSON(Embedded: true));
            AddChildren(cbor, PropertyKeys.ChargingStationGroups,     ChargingStationGroups,     group          => group.         ToJSON(Embedded: true));
            AddChildren(cbor, PropertyKeys.ChargingPoolGroups,        ChargingPoolGroups,        group          => group.         ToJSON(Embedded: true));
            AddChildren(cbor, PropertyKeys.ChargingTariffGroups,      ChargingTariffGroups,      group          => group.         ToJSON(Embedded: true));
            AddChildren(cbor, PropertyKeys.EnergyMeterGroups,         EnergyMeterGroups,         group          => group.         ToJSON(Embedded: true));
            AddChildren(cbor, PropertyKeys.ChargingTariffOffers,      ChargingTariffOffers,      offer          => offer.         ToJSON(Embedded: true));
            AddChildren(cbor, PropertyKeys.ParkingSpaces,             ParkingSpaces,             parkingSpace   => parkingSpace.  ToJSON(Embedded: true));

            // The keys have no CBOR representation of their own: theirs are the keys and values of their JSON representation.
            foreach (var keys in POIPartyKeys.AddTo(new JObject(), this).Properties())
                cbor.Add(CBORValue.FromText(keys.Name),                                  CBORJSON.ToCBOR(keys.Value));

            // Always part of the content, as in the JSON representation; custom data is JSON.
            cbor.Add(CBORValue.FromText(BasePropertyKeys.Created),                       CBORValue.FromText(Created.       ToISO8601()));
            cbor.Add(CBORValue.FromText(BasePropertyKeys.LastChange),                    CBORValue.FromText(LastChangeDate.ToISO8601()));

            if (CustomData.HasValues)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.CustomData),                CBORJSON.ToCBOR(CustomData.ToJObject()));

            return CustomChargingStationOperatorSerializer is not null
                       ? CustomChargingStationOperatorSerializer(this, cbor)
                       : cbor;

        }

        #endregion

        #region (private static) AddChildren(CBOR, PropertyName, Children, ToJSON)

        /// <summary>
        /// Add the given children, unless there are none, as the keys and values of their JSON representations.
        /// </summary>
        private static void AddChildren<T>(CBORMap           CBOR,
                                           String            PropertyName,
                                           IEnumerable<T>    Children,
                                           Func<T, JObject>  ToJSON)
        {

            var children = Children.ToArray();

            if (children.Length > 0)
                CBOR.Add(CBORValue.FromText(PropertyName),
                         CBORValue.FromArray(children.Select(child => CBORJSON.ToCBOR(ToJSON(child)))));

        }

        #endregion

        #region ToEmbeddedJSON()

        /// <summary>
        /// The complete JSON representation of this charging station operator
        /// as its roaming network embeds it:
        /// with its data licenses. Changes of operators use and compare it.
        /// </summary>
        public JObject ToEmbeddedJSON()

            => ToJSON(Embedded:            true,
                      ExpandDataLicenses:  InfoStatus.Expanded);

        #endregion


        #region (private static) TryParseOptionalText(JSON, PropertyName, PropertyDescription, Parser, out Value, out ErrorResponse)

        private static Boolean TryParseOptionalText<T>(JObject                           JSON,
                                                       String                            PropertyName,
                                                       String                            PropertyDescription,
                                                       TextParser<T>                     Parser,
                                                       out T?                            Value,
                                                       [NotNullWhen(false)] out String?  ErrorResponse)

            where T : struct

        {

            Value = null;

            if (JSON[PropertyName] is not JToken token)
            {
                ErrorResponse = null;
                return true;
            }

            if (token.Type != JTokenType.String || !Parser(token.Value<String>() ?? "", out var value))
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{token}'!";
                return false;
            }

            Value          = value;
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }

}
