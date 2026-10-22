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
using System.Globalization;
using System.Numerics;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.Mail;

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

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization the helpers read by default.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its address.
            /// </summary>
            public const String  Address       = "address";

            /// <summary>
            /// The property of its geographical location.
            /// </summary>
            public const String  GeoLocation   = "geoLocation";

        }

        #endregion

        #region (static) class ContactKeys

        /// <summary>
        /// The property keys of the contact data of an organization, as TryParseContact(…) reads them.
        /// </summary>
        public static class ContactKeys
        {

            /// <summary>
            /// The property of its logos: at most one.
            /// </summary>
            public const String  Logos                = "logos";

            /// <summary>
            /// The property of the URI of its logo.
            /// </summary>
            public const String  LogoURI              = "uri";

            /// <summary>
            /// The property of its address.
            /// </summary>
            public const String  Address              = "address";

            /// <summary>
            /// The property of its geographical location.
            /// </summary>
            public const String  GeoLocation          = "geoLocation";

            /// <summary>
            /// The property of its telephone number.
            /// </summary>
            public const String  Telephone            = "telephone";

            /// <summary>
            /// The property of its e-mail address.
            /// </summary>
            public const String  EMailAddress         = "eMailAddress";

            /// <summary>
            /// The property of its homepage.
            /// </summary>
            public const String  Homepage             = "homepage";

            /// <summary>
            /// The property of its hotline phone number.
            /// </summary>
            public const String  HotlinePhoneNumber   = "hotline";

            /// <summary>
            /// The property of the URL of its terms and conditions.
            /// </summary>
            public const String  TermsAndConditions   = "termsAndConditions";

            /// <summary>
            /// The property of its data licenses, given completely.
            /// </summary>
            public const String  DataLicenses         = "dataLicenses";

            /// <summary>
            /// The property of the identifications of its data licenses.
            /// </summary>
            public const String  DataLicenseIds       = "dataLicenseIds";

        }

        #endregion

        #region ToText              (JSON)

        /// <summary>
        /// The JSON text of the given JSON, every number as it is: for a store, a response and a WebSocket message,
        /// whatever is read again.
        /// </summary>
        /// <remarks>
        /// Decimals are read as decimals, keeping their decimal places, and the canonical JSON and CBOR the ETags and
        /// signatures are calculated over keep them as well. Newtonsoft writes a decimal without decimal places with
        /// one, 150000 as 150000.0, which reads back as another decimal: a roaming network read again would not have
        /// the ETags it was written with. Written so, the text is canonical JSON, with its properties ordinally sorted.
        /// </remarks>
        public static String ToText(JToken JSON)

            => CanonicalJSON.Serialize(JSON);

        #endregion

        #region Read                (Text, MaxDepth = 64)

        /// <summary>
        /// JSON text read as it was written, for whatever is hashed or signed again (section 1 of the specification):
        /// strings stay strings, also those that look like a point in time, numbers with a fraction or an exponent are
        /// decimals, as written, and an object names each of its properties once.
        /// </summary>
        /// <remarks>
        /// A number a decimal cannot hold as written is refused, not rounded: one with more than 28 decimal places, as
        /// 1e-30, or with a mantissa above 96 bits, as 7922816251426433759354395033.6. Read as decimals they were 0 and
        /// 7922816251426433759354395034, and a change set with such a value was taken with a content its sender had
        /// not signed. Of a property named twice the last one was taken, where other readers take the first.
        /// </remarks>
        /// <param name="Text">The JSON text.</param>
        /// <param name="MaxDepth">The maximum depth of the JSON.</param>
        /// <exception cref="JsonReaderException">When the text is no JSON, or no JSON so read.</exception>
        public static JToken Read(String  Text,
                                  Int32   MaxDepth   = 64)
        {

            using var reader = Reader(Text, MaxDepth);

            return ReadFrom(reader, Text);

        }

        #endregion

        #region Reader              (Text, MaxDepth = 64)

        /// <summary>
        /// A reader of the given JSON text, as Read() reads it: dates stay text, numbers with a fraction decimals.
        /// </summary>
        /// <param name="Text">The JSON text.</param>
        /// <param name="MaxDepth">The maximum depth of the JSON.</param>
        public static JsonTextReader Reader(String  Text,
                                            Int32   MaxDepth   = 64)

            => new (new StringReader(Text)) {
                   DateParseHandling   = DateParseHandling.None,
                   FloatParseHandling  = FloatParseHandling.Decimal,
                   MaxDepth            = MaxDepth
               };

        #endregion

        #region ReadFrom            (Reader, Text)

        /// <summary>
        /// The next JSON value of the given reader of the given JSON text, as Read() reads it, for a reader that goes on
        /// after it, e.g. to find out whether there is more.
        /// </summary>
        /// <param name="Reader">A reader of the JSON text, from Reader().</param>
        /// <param name="Text">The JSON text the reader reads.</param>
        /// <exception cref="JsonReaderException">When the text is no JSON, or no JSON so read.</exception>
        public static JToken ReadFrom(JsonTextReader  Reader,
                                      String          Text)
        {

            var json = JToken.ReadFrom(Reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });

            if (HasDecimals(json))
                CheckDecimals(Text, Reader.MaxDepth ?? 64);

            return json;

        }

        #endregion

        #region (private static) HasDecimals  (JSON)

        private static Boolean HasDecimals(JToken JSON)

            => JSON.Type == JTokenType.Float ||
               (JSON is JContainer container && container.Descendants().Any(token => token.Type == JTokenType.Float));

        #endregion

        #region (private static) CheckDecimals(Text, MaxDepth)

        /// <summary>
        /// Whether a decimal holds each number of the first JSON value of the given text as written, for Newtonsoft,
        /// which has read them, does not tell which it has rounded.
        /// </summary>
        private static void CheckDecimals(String  Text,
                                          Int32   MaxDepth)
        {

            var reader = new System.Text.Json.Utf8JsonReader(Encoding.UTF8.GetBytes(Text),
                                                              new System.Text.Json.JsonReaderOptions {
                                                                  CommentHandling      = System.Text.Json.JsonCommentHandling.Skip,
                                                                  AllowTrailingCommas  = true,
                                                                  AllowMultipleValues  = true,
                                                                  MaxDepth             = MaxDepth + 1
                                                              });

            while (reader.Read())
            {

                if (reader.TokenType == System.Text.Json.JsonTokenType.Number &&
                    !IsHeldByADecimal(reader.ValueSpan))
                {
                    throw new JsonReaderException($"The number {Encoding.UTF8.GetString(reader.ValueSpan)} is no decimal as written: it has more than 28 decimal places or a mantissa above 96 bits!");
                }

                if (reader.CurrentDepth == 0 &&
                    reader.TokenType is not System.Text.Json.JsonTokenType.StartObject
                                     and not System.Text.Json.JsonTokenType.StartArray
                                     and not System.Text.Json.JsonTokenType.PropertyName)
                {
                    break;
                }

            }

        }

        #endregion

        #region (private static) IsHeldByADecimal(Number)

        private static readonly BigInteger MaxDecimalMantissa = (BigInteger.One << 96) - 1;

        /// <summary>
        /// Whether a decimal holds the given JSON number as written, with as many decimal places: an integer, which
        /// Newtonsoft reads as an integer, always.
        /// </summary>
        private static Boolean IsHeldByADecimal(ReadOnlySpan<Byte> Number)
        {

            var text         = Encoding.ASCII.GetString(Number);
            var e            = text.IndexOfAny(['e', 'E']);
            var significand  = e < 0 ? text : text[..e];
            var point        = significand.IndexOf('.');

            if (e < 0 && point < 0)
                return true;

            if (!Int32.TryParse(e < 0 ? "0" : text[(e + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var exponent))
                return false;

            var mantissa     = BigInteger.Abs(BigInteger.Parse(point < 0 ? significand : significand.Remove(point, 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture));
            var scale        = (Int64) (point < 0 ? 0 : significand.Length - point - 1) - exponent;

            if (mantissa.IsZero)
                return scale <= 28;

            if (scale < 0)
            {

                if (scale < -29)
                    return false;

                mantissa *= BigInteger.Pow(10, (Int32) (-scale));
                scale     = 0;

            }

            return scale    <= 28 &&
                   mantissa <= MaxDecimalMantissa;

        }

        #endregion

        #region Number              (Value)

        /// <summary>
        /// A decimal as JSON: one without decimal places as the integer it is, as its canonical JSON and its CBOR write
        /// it anyway; one with decimal places as the decimal it is, with them.
        /// </summary>
        /// <remarks>
        /// Every writer of a POI3 entity writes its decimals so. Newtonsoft writes the decimal 22000 as 22000.0, a
        /// decimal with one decimal place, which a reader takes for other content: a roaming network with other ETags, a
        /// change set whose signatures no longer fit it, a commit of another identification. As an integer it writes
        /// 22000. Neither the canonical JSON nor the CBOR changes, and so neither do ETags, signatures and
        /// identifications. And as JSON read from a text has the integer 22000 as well, JToken.DeepEquals takes the
        /// JSON of an entity and the same JSON read again for the same.
        /// </remarks>
        public static JValue Number(Decimal Value)

            => IsWhole(Value)
                   ? new JValue((Int64) Value)
                   : new JValue(Value);

        #endregion

        #region (private static) IsWhole(Value)

        /// <summary>
        /// Whether the given decimal is one without decimal places that is written as an integer: within 64 bits, and
        /// not the negative zero.
        /// </summary>
        private static Boolean IsWhole(Decimal Value)

            => Value.Scale == 0                              &&
               !(Value == 0 && Decimal.IsNegative(Value))   &&
               Value >= Int64.MinValue && Value <= Int64.MaxValue;

        #endregion

        #region WholeNumbersAsIntegers(JSON)

        /// <summary>
        /// The given JSON with every decimal without decimal places as the integer it is, as Number() writes it: for
        /// JSON with decimals not written by POI3 itself, e.g. the values and the metadata a change set is given.
        /// Changed in place, for JSON made for the purpose, e.g. by ToJSON(); the JSON given is the one returned.
        /// </summary>
        public static T WholeNumbersAsIntegers<T>(T JSON)
            where T : JToken
        {

            switch (JSON)
            {

                case JValue { Value: Decimal number } value when IsWhole(number):
                    value.Value = (Int64) number;
                    break;

                case JContainer container:
                    foreach (var child in container.Children())
                        WholeNumbersAsIntegers(child);
                    break;

            }

            return JSON;

        }

        #endregion

        #region SameContent(Value1, Value2)

        /// <summary>
        /// Whether the given values are the same content - the same canonical JSON - absent meaning null: the integer
        /// 22000 is the decimal 22000, as the model holds it, and the decimal 22000.0 is not. JToken.DeepEquals has it
        /// the other way round, as it compares numbers of different types as different and decimals by their values.
        /// </summary>
        public static Boolean SameContent(JToken? Value1,
                                          JToken? Value2)

            => Value1 is null || Value2 is null
                   ? Value1 is null && Value2 is null
                   : CanonicalJSON.Serialize(Value1) == CanonicalJSON.Serialize(Value2);

        #endregion

        #region SameValue(Value1, Value2)

        /// <summary>
        /// Whether the given values are the same, their numbers compared by what they are worth, whatever their JSON
        /// type and their decimal places: 6, 6.0 and 6.00 are; null means absent. The question whether something
        /// changed - of a merge, of the differences an importer commits, of what groups give - as JToken.DeepEquals
        /// answered it while every decimal was written as a decimal: an importer computing a price per hour of 6.0 from
        /// one per minute of 0.1 does not change the price 6.
        /// </summary>
        /// <remarks>
        /// Not the question whether a value is the content it was: an old value is compared as canonical JSON
        /// (SameContent), and so is a merge commit with the merge of its parents.
        /// </remarks>
        public static Boolean SameValue(JToken? Value1,
                                        JToken? Value2)
        {

            if (Value1 is null || Value2 is null)
                return Value1 is null && Value2 is null;

            switch (Value1)
            {

                case JValue value1 when Value2 is JValue value2:

                    if (value1.Type is JTokenType.Integer or JTokenType.Float &&
                        value2.Type is JTokenType.Integer or JTokenType.Float)
                    {

                        if (value1.Value is Double or Single || value2.Value is Double or Single)
                            return Convert.ToDouble(value1.Value, CultureInfo.InvariantCulture) == Convert.ToDouble(value2.Value, CultureInfo.InvariantCulture);

                        try
                        {
                            return Convert.ToDecimal(value1.Value, CultureInfo.InvariantCulture) == Convert.ToDecimal(value2.Value, CultureInfo.InvariantCulture);
                        }
                        catch (OverflowException)
                        {
                            return JToken.DeepEquals(value1, value2);
                        }

                    }

                    return JToken.DeepEquals(value1, value2);

                case JArray array1 when Value2 is JArray array2:

                    if (array1.Count != array2.Count)
                        return false;

                    for (var index = 0; index < array1.Count; index++)
                        if (!SameValue(array1[index], array2[index]))
                            return false;

                    return true;

                case JObject object1 when Value2 is JObject object2:

                    if (object1.Count != object2.Count)
                        return false;

                    foreach (var property in object1.Properties())
                        if (!object2.TryGetValue(property.Name, out var other) || !SameValue(property.Value, other))
                            return false;

                    return true;

                case JProperty property1 when Value2 is JProperty property2:

                    return property1.Name == property2.Name && SameValue(property1.Value, property2.Value);

                default:
                    return JToken.DeepEquals(Value1, Value2);

            }

        }

        #endregion

        #region ContentText(Value)

        /// <summary>
        /// A value as its content is, its canonical JSON, to say it in an error response; "absent" for none.
        /// </summary>
        public static String ContentText(JToken? Value)

            => Value is null
                   ? "absent"
                   : CanonicalJSON.Serialize(Value);

        #endregion

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

            => TryParseAddress(JSON, PropertyKeys.Address, "address", out Address, out ErrorResponse);


        /// <summary>
        /// An optional address, e.g. of an entrance.
        /// </summary>
        public static Boolean TryParseAddress(JObject                           JSON,
                                              String                            PropertyName,
                                              String                            PropertyDescription,
                                              out Address?                      Address,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Address = null;

            if (JSON[PropertyName] is not JToken addressJSON)
            {
                ErrorResponse = null;
                return true;
            }

            if (addressJSON is not JObject addressObject)
            {
                ErrorResponse = $"The {PropertyDescription} must be a JSON object!";
                return false;
            }

            if (!Address.TryParse(addressObject, out Address, out var addressError))
            {
                ErrorResponse = $"Invalid {PropertyDescription}: " + addressError;
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

            => TryParseGeoLocation(JSON, PropertyKeys.GeoLocation, "geo location", out GeoLocation, out ErrorResponse);


        /// <summary>
        /// An optional geographical location, e.g. of an entrance.
        /// </summary>
        public static Boolean TryParseGeoLocation(JObject                           JSON,
                                                  String                            PropertyName,
                                                  String                            PropertyDescription,
                                                  out GeoCoordinate?                GeoLocation,
                                                  [NotNullWhen(false)] out String?  ErrorResponse)
        {

            GeoLocation = null;

            if (JSON[PropertyName] is not JToken geoLocationJSON)
            {
                ErrorResponse = null;
                return true;
            }

            if (geoLocationJSON is not JObject geoLocationObject)
            {
                ErrorResponse = $"The {PropertyDescription} must be a JSON object!";
                return false;
            }

            if (!TryParseGeoCoordinate(geoLocationObject, out var geoLocation, out var geoLocationError))
            {
                ErrorResponse = $"Invalid {PropertyDescription}: " + geoLocationError;
                return false;
            }

            GeoLocation    = geoLocation;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryParseGeoCoordinate(JSON, out GeoCoordinate, out ErrorResponse)

        /// <summary>
        /// A geographical coordinate, its numbers the doubles their texts are.
        /// </summary>
        /// <remarks>
        /// Read as decimals, as the POI JSON is, a coordinate's numbers are exactly their texts, but a decimal cast to
        /// a double is not always the double its text is: 51.050599999999996, the sum of two doubles, became 51.0506,
        /// and the roaming network read again would not have the ETags it was written with. Parsed from their texts,
        /// they are the doubles that were written.
        /// </remarks>
        public static Boolean TryParseGeoCoordinate(JObject                           JSON,
                                                    out GeoCoordinate                 GeoCoordinate,
                                                    [NotNullWhen(false)] out String?  ErrorResponse)
        {

            var exact = (JObject) JSON.DeepClone();

            foreach (var number in exact.Properties().Select(property => property.Value).OfType<JValue>().Where(value => value.Value is Decimal))
                number.Value = Double.Parse(((Decimal) number.Value!).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

            return org.GraphDefined.Vanaheimr.Aegir.GeoCoordinate.TryParse(exact, out GeoCoordinate, out ErrorResponse);

        }

        #endregion

        #region TryParseSeconds     (JSON, PropertyName, PropertyDescription, out Value, out ErrorResponse)

        /// <summary>
        /// An optional, not negative duration, given in seconds.
        /// </summary>
        public static Boolean TryParseSeconds(JObject                           JSON,
                                              String                            PropertyName,
                                              String                            PropertyDescription,
                                              out TimeSpan?                     Value,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value = null;

            if (JSON.ParseOptional(PropertyName, PropertyDescription, out Decimal? seconds, out ErrorResponse) &&
                ErrorResponse is not null)
            {
                return false;
            }

            if (seconds < 0)
            {
                ErrorResponse = $"The {PropertyDescription} must not be negative!";
                return false;
            }

            Value          = seconds.HasValue ? FromSeconds(seconds.Value) : null;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryParseNonNegative (JSON, PropertyName, PropertyDescription, out Value, out ErrorResponse)

        /// <summary>
        /// An optional number, not negative, e.g. a distance in meters.
        /// </summary>
        public static Boolean TryParseNonNegative(JObject                           JSON,
                                                  String                            PropertyName,
                                                  String                            PropertyDescription,
                                                  out Decimal?                      Value,
                                                  [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value = null;

            if (JSON.ParseOptional(PropertyName, PropertyDescription, out Decimal? number, out ErrorResponse) &&
                ErrorResponse is not null)
            {
                return false;
            }

            if (number < 0)
            {
                ErrorResponse = $"The {PropertyDescription} must not be negative!";
                return false;
            }

            Value          = number;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryParseInteger     (JSON, PropertyName, PropertyDescription, out Value, out ErrorResponse)

        /// <summary>
        /// An optional integer, e.g. the priority of a group: a JSON number without fraction, neither a text nor 1.0.
        /// </summary>
        public static Boolean TryParseInteger(JObject                           JSON,
                                              String                            PropertyName,
                                              String                            PropertyDescription,
                                              out Int32?                        Value,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value = null;

            if (JSON[PropertyName] is not JToken token)
            {
                ErrorResponse = null;
                return true;
            }

            if (token.Type != JTokenType.Integer)
            {
                ErrorResponse = $"The {PropertyDescription} must be an integer!";
                return false;
            }

            try
            {
                Value = token.Value<Int32>();
            }
            catch (OverflowException)
            {
                ErrorResponse = $"The {PropertyDescription} must be an integer from {Int32.MinValue} to {Int32.MaxValue}!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region Seconds             (Duration)

        /// <summary>
        /// The given duration in seconds, as JSON: a whole number of them as an integer, see Number().
        /// </summary>
        public static JValue Seconds(TimeSpan Duration)

            => Number((Decimal) Duration.Ticks / TimeSpan.TicksPerSecond);

        #endregion

        #region FromSeconds         (Seconds)

        /// <summary>
        /// The duration of the given seconds.
        /// </summary>
        public static TimeSpan FromSeconds(Decimal Seconds)

            => TimeSpan.FromTicks((Int64) (Seconds * TimeSpan.TicksPerSecond));

        #endregion

        #region TryParseName<TEnum> (Text, out Value)

        /// <summary>
        /// A value of the given enumeration by its exact name, e.g. a payment option; not by its number.
        /// </summary>
        public static Boolean TryParseName<TEnum>(String     Text,
                                                  out TEnum  Value)

            where TEnum : struct, Enum

            => Enum.TryParse(Text, ignoreCase: false, out Value) &&
               Enum.IsDefined(Value) &&
               Value.ToString() == Text;

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

                // Names only, exactly: Enum.TryParse would also take numbers and names with blanks around them.
                if (item.Type != JTokenType.String ||
                    !Enum.GetNames<TEnum>().Contains(item.Value<String>(), StringComparer.Ordinal) ||
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

                if (item is not JObject itemJSON)
                {
                    ErrorResponse = $"Invalid {PropertyDescription} '{item.ToString(Newtonsoft.Json.Formatting.None)}'!";
                    return false;
                }

                if (!Parser(itemJSON, out var value, out var error))
                {
                    ErrorResponse = $"Invalid {PropertyDescription} '{item.ToString(Newtonsoft.Json.Formatting.None)}': {error}";
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

        #region UniqueIds<T>        (Ids, Description)

        /// <summary>
        /// The given values in their given order, e.g. languages by preference, without one given twice.
        /// </summary>
        public static ImmutableArray<T> UniqueIds<T>(IEnumerable<T>?  Ids,
                                                     String           Description)

            where T : struct
        {

            var ids = (Ids ?? []).ToArray();

            if (ids.Distinct().Count() != ids.Length)
                throw new ArgumentException($"A {Description} must not be given twice!");

            return [.. ids];

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

        #region TryParseEMailAddress(Value, PropertyDescription, out EMailAddress, out ErrorResponse)

        /// <summary>
        /// An optional e-mail address, as its text: "info@example.org", or with the name of its owner,
        /// "Example Hotline &lt;hotline@example.org&gt;", as EMailAddress.ToString() writes it.
        /// </summary>
        public static Boolean TryParseEMailAddress(JToken?                           Value,
                                                    String                            PropertyDescription,
                                                    out EMailAddress?                 EMailAddress,
                                                    [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (Value is null)
            {
                EMailAddress   = null;
                ErrorResponse  = null;
                return true;
            }

            if (Value.Type != JTokenType.String)
            {
                EMailAddress   = null;
                ErrorResponse  = $"Invalid {PropertyDescription} '{Value}'!";
                return false;
            }

            return TryParseEMailAddress(Value.Value<String>(), PropertyDescription, out EMailAddress, out ErrorResponse);

        }

        public static Boolean TryParseEMailAddress(String?                           Text,
                                                    String                            PropertyDescription,
                                                    out EMailAddress?                 EMailAddress,
                                                    [NotNullWhen(false)] out String?  ErrorResponse)
        {

            EMailAddress   = null;
            ErrorResponse  = null;

            if (Text is null)
                return true;

            try
            {
                EMailAddress = EMailAddress.Parse(Text);
            }
            catch (Exception)
            { }

            if (EMailAddress is null)
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{Text}'!";
                return false;
            }

            return true;

        }

        #endregion

        #region TryParseContact(JSON, out Logo, out Address, ..., out DataLicenses, out ErrorResponse)

        /// <summary>
        /// The optional contact data and data licenses of the given JSON representation of an organization, e.g. an
        /// e-mobility provider or a manufacturer, as a charging station operator writes them: its logo, as "logos" of at
        /// most one { "uri" }, its address, geo location, telephone number, e-mail address, homepage, hotline and the URL
        /// of its terms and conditions.
        /// </summary>
        public static Boolean TryParseContact(JObject                           JSON,
                                              out URL?                          Logo,
                                              out Address?                      Address,
                                              out GeoCoordinate?                GeoLocation,
                                              out PhoneNumber?                  Telephone,
                                              out EMailAddress?                 EMailAddress,
                                              out URL?                          Homepage,
                                              out PhoneNumber?                  HotlinePhoneNumber,
                                              out URL?                          TermsAndConditionsURL,
                                              out ImmutableArray<DataLicense>   DataLicenses,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Logo                   = null;
            Address                = null;
            GeoLocation            = null;
            Telephone              = null;
            EMailAddress           = null;
            Homepage               = null;
            HotlinePhoneNumber     = null;
            TermsAndConditionsURL  = null;
            DataLicenses           = [];

            if (JSON[ContactKeys.Logos] is JToken logosJSON)
            {

                if (logosJSON is not JArray logosArray || logosArray.Count > 1 ||
                    (logosArray.Count == 1 && logosArray[0] is not JObject))
                {
                    ErrorResponse = "The logos must be a JSON array of at most one logo object!";
                    return false;
                }

                if (logosArray.Count == 1)
                {

                    if (!TryParseText((JObject) logosArray[0], ContactKeys.LogoURI, "logo URI", URL.TryParse, out Logo, out ErrorResponse))
                        return false;

                    if (Logo is null)
                    {
                        ErrorResponse = "A logo requires an URI!";
                        return false;
                    }

                }

            }

            if (!TryParseAddress     (JSON, ContactKeys.Address,             "address",                   out Address,      out ErrorResponse) ||
                !TryParseGeoLocation (JSON, ContactKeys.GeoLocation,         "geo location",              out GeoLocation,  out ErrorResponse) ||
                !TryParseText        (JSON, ContactKeys.Telephone,           "telephone number",          PhoneNumber.TryParse, out Telephone,              out ErrorResponse) ||
                !TryParseEMailAddress(JSON[ ContactKeys.EMailAddress],       "e-mail address",            out EMailAddress, out ErrorResponse) ||
                !TryParseText        (JSON, ContactKeys.Homepage,            "homepage",                  URL.        TryParse, out Homepage,               out ErrorResponse) ||
                !TryParseText        (JSON, ContactKeys.HotlinePhoneNumber,  "hotline phone number",      PhoneNumber.TryParse, out HotlinePhoneNumber,     out ErrorResponse) ||
                !TryParseText        (JSON, ContactKeys.TermsAndConditions,  "terms and conditions URL",  URL.        TryParse, out TermsAndConditionsURL,  out ErrorResponse))
            {
                return false;
            }

            return POIDataLicenses.TryParse(JSON[ContactKeys.DataLicenses],
                                            JSON[ContactKeys.DataLicenseIds],
                                            out DataLicenses,
                                            out ErrorResponse);

        }

        #endregion

    }

}
