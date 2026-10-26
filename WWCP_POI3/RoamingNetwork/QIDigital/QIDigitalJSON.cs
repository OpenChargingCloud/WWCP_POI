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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// What the QI-Digital documents and their parts read and write alike: only their own properties, their JSON-LD
    /// context, timestamps exactly as they are signed, and decimal numbers.
    /// </summary>
    internal static class QIDigitalJSON
    {

        #region Context

        /// <summary>
        /// The JSON-LD context of a QI-Digital object, versioned like those of the roaming network: it is part of its
        /// signature input, so that a signature of one kind of document or one version is never one of another.
        /// </summary>
        /// <param name="Object">The object, e.g. "digitalCalibrationCertificate".</param>
        /// <param name="Version">Its version.</param>
        public static String Context(String  Object,
                                     UInt32  Version = 1)

            => $"https://open.charging.cloud/contexts/wwcp/qiDigital/{Object}/v{Version}";

        #endregion

        #region TryOnly(JSON, Description, out ErrorResponse, params PropertyNames)

        /// <summary>
        /// Whether the given JSON names only the given properties: anything else is refused, so that nothing unsigned
        /// travels with a signed document.
        /// </summary>
        public static Boolean TryOnly(JObject                           JSON,
                                      String                            Description,
                                      [NotNullWhen(false)] out String?  ErrorResponse,
                                      params String[]                   PropertyNames)
        {

            foreach (var property in JSON.Properties())
                if (!PropertyNames.Contains(property.Name, StringComparer.Ordinal))
                {
                    ErrorResponse = $"Unknown property '{property.Name}' of {Description}!";
                    return false;
                }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region TryContext(JSON, Context, Description, out ErrorResponse)

        /// <summary>
        /// Whether the given JSON names the given JSON-LD context, if it names one: embedded it names none.
        /// </summary>
        public static Boolean TryContext(JObject                           JSON,
                                         String                            Context,
                                         String                            Description,
                                         [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (JSON[QIDigitalSignature.PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == Context))
            {
                ErrorResponse = $"The JSON-LD context of {Description} must be '{Context}'!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region Text(JSON, PropertyName)

        /// <summary>
        /// The text of the given property, or null when it is none.
        /// </summary>
        public static String? Text(JObject JSON, String PropertyName)

            => JSON[PropertyName]?.Type == JTokenType.String
                   ? JSON[PropertyName]!.Value<String>()
                   : null;

        #endregion

        #region TryParseRequiredText(JSON, PropertyName, Description, Parser, out Value, out ErrorResponse)

        /// <summary>
        /// A mandatory value given as a text, e.g. an identification.
        /// </summary>
        public static Boolean TryParseRequiredText<T>(JObject                           JSON,
                                                      String                            PropertyName,
                                                      String                            Description,
                                                      POITextParser<T>                  Parser,
                                                      out T                             Value,
                                                      [NotNullWhen(false)] out String?  ErrorResponse)

            where T : struct

        {

            Value = default;

            if (Text(JSON, PropertyName) is not String text)
            {
                ErrorResponse = $"The {Description} is missing!";
                return false;
            }

            if (!Parser(text, out Value))
            {
                ErrorResponse = $"Invalid {Description} '{text}'!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region TryParseTimestamp(JSON, PropertyName, Description, out Timestamp, out ErrorResponse)

        /// <summary>
        /// An optional timestamp, written exactly as yyyy-MM-ddTHH:mm:ss.fffZ, as it is signed.
        /// </summary>
        public static Boolean TryParseTimestamp(JObject                           JSON,
                                                String                            PropertyName,
                                                String                            Description,
                                                out DateTimeOffset?               Timestamp,
                                                [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Timestamp = null;

            if (JSON[PropertyName] is null)
            {
                ErrorResponse = null;
                return true;
            }

            if (Text(JSON, PropertyName) is not String text ||
                !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp) ||
                timestamp.ToISO8601() != text)
            {
                ErrorResponse = $"The {Description} must be written as yyyy-MM-ddTHH:mm:ss.fffZ!";
                return false;
            }

            Timestamp      = timestamp;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryParseDecimal(JSON, PropertyName, Description, out Value, out ErrorResponse)

        /// <summary>
        /// An optional number, e.g. a measurement error: a JSON number, not a text.
        /// </summary>
        public static Boolean TryParseDecimal(JObject                           JSON,
                                              String                            PropertyName,
                                              String                            Description,
                                              out Decimal?                      Value,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value = null;

            if (JSON[PropertyName] is not JToken token)
            {
                ErrorResponse = null;
                return true;
            }

            if (token.Type is not (JTokenType.Integer or JTokenType.Float))
            {
                ErrorResponse = $"The {Description} must be a number!";
                return false;
            }

            try
            {
                Value = token.Value<Decimal>();
            }
            catch (OverflowException)
            {
                ErrorResponse = $"The {Description} is out of range!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region Timestamp(Timestamp)

        /// <summary>
        /// The given timestamp to the millisecond, as it is signed.
        /// </summary>
        public static DateTimeOffset Timestamp(DateTimeOffset Timestamp)

            => DateTimeOffset.Parse(Timestamp.ToISO8601(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

        #endregion

        #region Timestamp(Timestamp)

        /// <summary>
        /// The given optional timestamp to the millisecond, as it is signed.
        /// </summary>
        public static DateTimeOffset? Timestamp(DateTimeOffset? Timestamp)

            => Timestamp.HasValue
                   ? QIDigitalJSON.Timestamp(Timestamp.Value)
                   : null;

        #endregion

        #region Number(Value)

        /// <summary>
        /// The given number without trailing zeros, e.g. 0.50 as 0.5: as it is written, read back and signed.
        /// </summary>
        public static Decimal Number(Decimal Value)

            => Value / 1.000000000000000000000000000000000m;

        #endregion

        #region Number(Value)

        /// <summary>
        /// The given optional number without trailing zeros.
        /// </summary>
        public static Decimal? Number(Decimal? Value)

            => Value.HasValue
                   ? Number(Value.Value)
                   : null;

        #endregion

        #region Distinct<T>(Values, Key, Description)

        /// <summary>
        /// The given values in their given order, none twice by its key.
        /// </summary>
        public static ImmutableArray<T> Distinct<T>(IEnumerable<T>?  Values,
                                                    Func<T, String>  Key,
                                                    String           Description)

            => POIJSON.Distinct(Values, Key, Description);

        #endregion

    }

}
