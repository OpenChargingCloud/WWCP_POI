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

                if (!JSON.ParseMandatory("@id",
                                         "charging station operator identification",
                                         ChargingStationOperator_Id.TryParse,
                                         out ChargingStationOperator_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse Name                     [optional]

                if (JSON.ParseOptionalJSON("name",
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

                if (JSON.ParseOptionalJSON("description",
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

                if (JSON.ParseOptional("dataSource",
                                       "data source",
                                       out String? dataSource,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse DataLicenses             [optional]

                if (!POIDataLicenses.TryParse(JSON["dataLicenses"],
                                              JSON["dataLicenseIds"],
                                              out var dataLicenses,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse Address                  [optional]

                Address? address = null;

                if (JSON["address"] is JToken addressJSON)
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

                if (JSON["geoLocation"] is JToken geoLocationJSON)
                {

                    if (geoLocationJSON is not JObject geoLocationObject)
                    {
                        ErrorResponse = "The geo location must be a JSON object!";
                        return false;
                    }

                    if (!GeoCoordinate.TryParse(geoLocationObject, out var parsedGeoLocation, out var geoLocationError))
                    {
                        ErrorResponse = "Invalid geo location: " + geoLocationError;
                        return false;
                    }

                    geoLocation = parsedGeoLocation;

                }

                #endregion

                #region Parse Logo                     [optional]

                URL? logo = null;

                if (JSON["logos"] is JToken logosJSON)
                {

                    if (logosJSON is not JArray logosArray || logosArray.Count > 1 ||
                        (logosArray.Count == 1 && logosArray[0] is not JObject))
                    {
                        ErrorResponse = "The logos must be a JSON array of at most one logo object!";
                        return false;
                    }

                    if (logosArray.Count == 1)
                    {

                        if (!TryParseOptionalText((JObject) logosArray[0], "uri", "logo URI", URL.TryParse, out logo, out ErrorResponse))
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

                if (!TryParseOptionalText(JSON, "telephone",           "telephone number",          PhoneNumber.       TryParse, out PhoneNumber?         telephone,             out ErrorResponse) ||
                    !TryParseOptionalText(JSON, "eMailAddress",        "e-mail address",            SimpleEMailAddress.TryParse, out SimpleEMailAddress?  eMailAddress,          out ErrorResponse) ||
                    !TryParseOptionalText(JSON, "homepage",            "homepage",                  URL.               TryParse, out URL?                 homepage,              out ErrorResponse) ||
                    !TryParseOptionalText(JSON, "hotline",             "hotline phone number",      PhoneNumber.       TryParse, out PhoneNumber?         hotlinePhoneNumber,    out ErrorResponse) ||
                    !TryParseOptionalText(JSON, "termsAndConditions",  "terms and conditions URL",  URL.               TryParse, out URL?                 termsAndConditionsURL, out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse ChargingPools            [optional]

                if (!POIJSON.TryParseChildren<ChargingPool>(JSON,
                                                            "chargingPools",
                                                            "chargingPoolIds",
                                                            "charging pools",
                                                            ChargingPool.TryParse,
                                                            out var chargingPools,
                                                            out ErrorResponse))
                {
                    return false;
                }

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

        #region ToCBOR   (Embedded = false)

        /// <summary>
        /// Return the CBOR representation of this charging station operator:
        /// the keys and values of its complete JSON representation, with its data licenses.
        /// Write it with CBORWriterOptions.Canonical for deterministic bytes.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        public CBORValue ToCBOR(Boolean Embedded = false)

            => CBORJSON.ToCBOR(ToJSON(Embedded:                Embedded,
                                      ExpandRoamingNetworkId:  InfoStatus.Hidden,
                                      ExpandDataLicenses:      InfoStatus.Expanded));

        #endregion

        #region ToEmbeddedJSON()

        /// <summary>
        /// The complete JSON representation of this charging station operator
        /// as its roaming network embeds it: without the reference to the network,
        /// with its data licenses. Changes of operators use and compare it.
        /// </summary>
        public JObject ToEmbeddedJSON()

            => ToJSON(Embedded:                true,
                      ExpandRoamingNetworkId:  InfoStatus.Hidden,
                      ExpandDataLicenses:      InfoStatus.Expanded);

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
