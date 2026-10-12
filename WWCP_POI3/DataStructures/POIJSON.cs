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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Parses the values of a text, e.g. an identification.
    /// </summary>
    internal delegate Boolean POITextParser<T>(String Text, out T Value);

    /// <summary>
    /// Parses an embedded JSON object, e.g. a charging station of a charging pool.
    /// </summary>
    internal delegate Boolean POIObjectParser<T>(JObject JSON, [NotNullWhen(true)] out T? Value, [NotNullWhen(false)] out String? ErrorResponse);


    /// <summary>
    /// Helpers for the JSON representations of the POI3 entities.
    /// </summary>
    internal static class POIJSON
    {

        #region TryParseI18N        (JSON, PropertyName, PropertyDescription, out Value, out ErrorResponse)

        /// <summary>
        /// An optional multi-language text.
        /// </summary>
        public static Boolean TryParseI18N(JObject                           JSON,
                                           String                            PropertyName,
                                           String                            PropertyDescription,
                                           out ImmutableI18NString?          Value,
                                           [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value = null;

            if (JSON.ParseOptionalJSON(PropertyName,
                                       PropertyDescription,
                                       ImmutableI18NString.TryParse,
                                       out ImmutableI18NString? value,
                                       out ErrorResponse))
            {

                if (ErrorResponse is not null)
                    return false;

                Value = value;

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region TryParseString      (JSON, PropertyName, PropertyDescription, out Value, out ErrorResponse)

        /// <summary>
        /// An optional text.
        /// </summary>
        public static Boolean TryParseString(JObject                           JSON,
                                             String                            PropertyName,
                                             String                            PropertyDescription,
                                             out String?                       Value,
                                             [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value = null;

            if (JSON[PropertyName] is not JToken token)
            {
                ErrorResponse = null;
                return true;
            }

            if (token.Type != JTokenType.String)
            {
                ErrorResponse = $"The {PropertyDescription} must be a text!";
                return false;
            }

            Value          = token.Value<String>();
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryParseText<T>     (JSON, PropertyName, PropertyDescription, Parser, out Value, out ErrorResponse)

        /// <summary>
        /// An optional value given as a text, e.g. an identification or a phone number.
        /// </summary>
        public static Boolean TryParseText<T>(JObject                           JSON,
                                              String                            PropertyName,
                                              String                            PropertyDescription,
                                              POITextParser<T>                  Parser,
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

        #region TryParseAddress     (JSON, out Address, out ErrorResponse)

        /// <summary>
        /// An optional address.
        /// </summary>
        public static Boolean TryParseAddress(JObject                           JSON,
                                              out Address?                      Address,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Address = null;

            if (JSON["address"] is not JToken addressJSON)
            {
                ErrorResponse = null;
                return true;
            }

            if (addressJSON is not JObject addressObject)
            {
                ErrorResponse = "The address must be a JSON object!";
                return false;
            }

            if (!Address.TryParse(addressObject, out Address, out var addressError))
            {
                ErrorResponse = "Invalid address: " + addressError;
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region TryParseGeoLocation (JSON, out GeoLocation, out ErrorResponse)

        /// <summary>
        /// An optional geographical location.
        /// </summary>
        public static Boolean TryParseGeoLocation(JObject                           JSON,
                                                  out GeoCoordinate?                GeoLocation,
                                                  [NotNullWhen(false)] out String?  ErrorResponse)
        {

            GeoLocation = null;

            if (JSON["geoLocation"] is not JToken geoLocationJSON)
            {
                ErrorResponse = null;
                return true;
            }

            if (geoLocationJSON is not JObject geoLocationObject)
            {
                ErrorResponse = "The geo location must be a JSON object!";
                return false;
            }

            if (!GeoCoordinate.TryParse(geoLocationObject, out var geoLocation, out var geoLocationError))
            {
                ErrorResponse = "Invalid geo location: " + geoLocationError;
                return false;
            }

            GeoLocation    = geoLocation;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryParseFlags<TEnum>(JSON, PropertyName, PropertyDescription, out Value, out ErrorResponse)

        /// <summary>
        /// Optional flags, given as an array of their names.
        /// </summary>
        public static Boolean TryParseFlags<TEnum>(JObject                           JSON,
                                                   String                            PropertyName,
                                                   String                            PropertyDescription,
                                                   out TEnum                         Value,
                                                   [NotNullWhen(false)] out String?  ErrorResponse)

            where TEnum : struct, Enum

        {

            Value = default;

            if (JSON[PropertyName] is not JToken token)
            {
                ErrorResponse = null;
                return true;
            }

            if (token is not JArray array)
            {
                ErrorResponse = $"The {PropertyDescription} must be a JSON array!";
                return false;
            }

            var flags = 0L;

            foreach (var item in array)
            {

                if (item.Type != JTokenType.String ||
                    !Enum.TryParse<TEnum>(item.Value<String>(), ignoreCase: false, out var flag) ||
                    !Enum.IsDefined(flag) ||
                    Convert.ToInt64(flag) == 0)
                {
                    ErrorResponse = $"Invalid {PropertyDescription} '{item}'!";
                    return false;
                }

                flags |= Convert.ToInt64(flag);

            }

            Value          = (TEnum) Enum.ToObject(typeof(TEnum), flags);
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region FlagsToJSON<TEnum>  (Flags)

        /// <summary>
        /// The names of the set flags, without the empty one, in the order of their values.
        /// </summary>
        public static JArray FlagsToJSON<TEnum>(TEnum Flags)

            where TEnum : struct, Enum

            => new (Enum.GetValues<TEnum>().
                         Where (flag => Convert.ToInt64(flag) != 0 && Flags.HasFlag(flag)).
                         Select(flag => flag.ToString()));

        #endregion

        #region TryParseEnum<TEnum> (JSON, PropertyName, PropertyDescription, out Value, out ErrorResponse)

        /// <summary>
        /// An optional value given by its name, e.g. a voltage level.
        /// </summary>
        public static Boolean TryParseEnum<TEnum>(JObject                           JSON,
                                                  String                            PropertyName,
                                                  String                            PropertyDescription,
                                                  out TEnum?                        Value,
                                                  [NotNullWhen(false)] out String?  ErrorResponse)

            where TEnum : struct, Enum

        {

            Value = null;

            if (JSON[PropertyName] is not JToken token)
            {
                ErrorResponse = null;
                return true;
            }

            if (token.Type != JTokenType.String ||
                !Enum.TryParse<TEnum>(token.Value<String>(), ignoreCase: false, out var value) ||
                !Enum.IsDefined(value) ||
                value.ToString() != token.Value<String>())
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{token}'!";
                return false;
            }

            Value          = value;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryParseStrings     (JSON, PropertyName, PropertyDescription, out Values, out ErrorResponse)

        /// <summary>
        /// Optional texts, given as an array, in their given order.
        /// </summary>
        public static Boolean TryParseStrings(JObject                           JSON,
                                              String                            PropertyName,
                                              String                            PropertyDescription,
                                              out ImmutableArray<String>        Values,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Values = [];

            if (JSON[PropertyName] is not JToken token)
            {
                ErrorResponse = null;
                return true;
            }

            if (token is not JArray array || array.Any(item => item.Type != JTokenType.String))
            {
                ErrorResponse = $"The {PropertyDescription} must be a JSON array of texts!";
                return false;
            }

            Values         = [.. array.Select(item => item.Value<String>()!)];
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryParseObjects<T>  (JSON, PropertyName, PropertyDescription, Parser, out Values, out ErrorResponse)

        /// <summary>
        /// Optional embedded values without identifications, given as an array, in their given order, e.g. images.
        /// </summary>
        public static Boolean TryParseObjects<T>(JObject                           JSON,
                                                 String                            PropertyName,
                                                 String                            PropertyDescription,
                                                 POIObjectParser<T>                Parser,
                                                 out ImmutableArray<T>             Values,
                                                 [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Values = [];

            if (JSON[PropertyName] is not JToken token)
            {
                ErrorResponse = null;
                return true;
            }

            if (token is not JArray array)
            {
                ErrorResponse = $"The {PropertyDescription} must be a JSON array!";
                return false;
            }

            var values = ImmutableArray.CreateBuilder<T>(array.Count);

            foreach (var item in array)
            {

                if (item is not JObject itemJSON || !Parser(itemJSON, out var value, out var error))
                {
                    ErrorResponse = $"Invalid {PropertyDescription} '{item.ToString(Newtonsoft.Json.Formatting.None)}'!";
                    return false;
                }

                values.Add(value);

            }

            Values         = values.MoveToImmutable();
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region Distinct<T>         (Values, Key, Description)

        /// <summary>
        /// The given values in their given order, without null entries and without one given twice by its key.
        /// </summary>
        public static ImmutableArray<T> Distinct<T>(IEnumerable<T>?  Values,
                                                    Func<T, String>  Key,
                                                    String           Description)
        {

            var values = (Values ?? []).ToArray();

            if (values.Any(value => value is null))
                throw new ArgumentException($"An {Description} must not be null!");

            if (values.Select(Key).Distinct(StringComparer.Ordinal).Count() != values.Length)
                throw new ArgumentException($"An {Description} must not be given twice!");

            return [.. values];

        }

        #endregion

        #region TryParseObject<T>   (JSON, PropertyName, PropertyDescription, Parser, out Value, out ErrorResponse)

        /// <summary>
        /// An optional embedded value, e.g. the opening times of a charging pool.
        /// </summary>
        public static Boolean TryParseObject<T>(JObject                           JSON,
                                                String                            PropertyName,
                                                String                            PropertyDescription,
                                                POIObjectParser<T>                Parser,
                                                out T?                            Value,
                                                [NotNullWhen(false)] out String?  ErrorResponse)

            where T : class

        {

            Value = null;

            if (JSON[PropertyName] is not JToken token)
            {
                ErrorResponse = null;
                return true;
            }

            if (token is not JObject json)
            {
                ErrorResponse = $"The {PropertyDescription} must be a JSON object!";
                return false;
            }

            if (!Parser(json, out var value, out var error))
            {
                ErrorResponse = $"Invalid {PropertyDescription}: {error}";
                return false;
            }

            Value          = value;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryParseIds<T>      (JSON, PropertyName, PropertyDescription, Parser, out Values, out ErrorResponse)

        /// <summary>
        /// Optional references, given as an array of identifications, in their given order.
        /// </summary>
        public static Boolean TryParseIds<T>(JObject                           JSON,
                                             String                            PropertyName,
                                             String                            PropertyDescription,
                                             POITextParser<T>                  Parser,
                                             out ImmutableArray<T>             Values,
                                             [NotNullWhen(false)] out String?  ErrorResponse)

            where T : struct

        {

            Values = [];

            if (JSON[PropertyName] is not JToken token)
            {
                ErrorResponse = null;
                return true;
            }

            if (token is not JArray array)
            {
                ErrorResponse = $"The {PropertyDescription} must be a JSON array!";
                return false;
            }

            var values = ImmutableArray.CreateBuilder<T>(array.Count);

            foreach (var item in array)
            {

                if (item.Type != JTokenType.String || !Parser(item.Value<String>() ?? "", out var value))
                {
                    ErrorResponse = $"Invalid {PropertyDescription} '{item}'!";
                    return false;
                }

                values.Add(value);

            }

            Values         = values.MoveToImmutable();
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region OrderedIds<T>       (Ids, Description)

        /// <summary>
        /// The given references in the order of their identifications (ordinal), without one given twice.
        /// </summary>
        public static ImmutableArray<T> OrderedIds<T>(IEnumerable<T>?  Ids,
                                                      String           Description)

            where T : struct

        {

            var ids = (Ids ?? []).ToArray();

            if (ids.Distinct().Count() != ids.Length)
                throw new ArgumentException($"A {Description} must not be given twice!");

            return [.. ids.OrderBy(id => id.ToString(), StringComparer.Ordinal)];

        }

        #endregion

        #region IdsToJSON<T>        (Ids)

        /// <summary>
        /// The given references as a JSON array of their identifications.
        /// </summary>
        public static JArray IdsToJSON<T>(IEnumerable<T> Ids)

            where T : struct

            => new (Ids.Select(id => id.ToString()));

        #endregion

        #region TryParseChildren<T> (JSON, PropertyName, IdsPropertyName, Description, Parser, out Children, out ErrorResponse)

        /// <summary>
        /// The optional embedded children of an entity, e.g. the charging stations of a charging pool.
        /// They must be given completely: their identifications alone are refused.
        /// </summary>
        public static Boolean TryParseChildren<T>(JObject                           JSON,
                                                  String                            PropertyName,
                                                  String                            IdsPropertyName,
                                                  String                            Description,
                                                  POIObjectParser<T>                Parser,
                                                  out ImmutableArray<T>             Children,
                                                  [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Children = [];

            if (JSON[IdsPropertyName] is not null)
            {
                ErrorResponse = $"The {Description} must be given completely in '{PropertyName}', not by '{IdsPropertyName}'!";
                return false;
            }

            if (JSON[PropertyName] is not JToken token)
            {
                ErrorResponse = null;
                return true;
            }

            if (token is not JArray array)
            {
                ErrorResponse = $"The {Description} must be a JSON array!";
                return false;
            }

            var children = ImmutableArray.CreateBuilder<T>(array.Count);

            foreach (var item in array)
            {

                if (item is not JObject itemJSON)
                {
                    ErrorResponse = $"The {Description} must be JSON objects!";
                    return false;
                }

                if (!Parser(itemJSON, out var child, out var childError))
                {
                    ErrorResponse = childError;
                    return false;
                }

                children.Add(child);

            }

            Children       = children.MoveToImmutable();
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }

}
