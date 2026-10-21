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

using System.Globalization;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Strict JSON reading for the plain data structures of WWCP POI2:
    /// unknown properties, wrong types and empty texts are errors,
    /// and every error names the path where it occurred.
    /// </summary>
    internal static partial class StrictJSON
    {

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys every strictly read data structure shares.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context.
            /// </summary>
            public const String  Context   = "@context";

        }

        #endregion

        #region Properties

        /// <summary>
        /// Reject properties other than the given ones.
        /// </summary>
        /// <param name="JSON">A JSON object.</param>
        /// <param name="Properties">The allowed properties.</param>
        internal static void OnlyProperties(JObject          JSON,
                                            params String[]  Properties)
        {

            ArgumentNullException.ThrowIfNull(JSON);

            foreach (var property in JSON.Properties())
                if (!Properties.Contains(property.Name))
                    throw new ArgumentException($"Unknown property '{property.Name}'.");

        }

        #endregion

        #region Text, links and dates

        /// <summary>
        /// An optional text; null when missing or null.
        /// </summary>
        internal static String? Text(JObject  JSON,
                                     String   Property)
        {

            if (JSON[Property] is not { } token || token.Type == JTokenType.Null)
                return null;

            if (token.Type != JTokenType.String)
                throw new ArgumentException($"{Property}: expected a string.");

            return token.Value<String>();

        }

        /// <summary>
        /// A mandatory, nonempty text.
        /// </summary>
        internal static String RequiredText(JObject  JSON,
                                            String   Property)

            => Text(JSON, Property) is { } text && !String.IsNullOrWhiteSpace(text)
                   ? text
                   : throw new ArgumentException($"{Property}: expected a nonempty string.");

        /// <summary>
        /// An optional absolute URL.
        /// </summary>
        internal static URL? Link(JObject  JSON,
                                  String   Property)
        {

            var text = Text(JSON, Property);

            if (text is null)
                return null;

            if (!Uri.TryCreate(text, UriKind.Absolute, out _) || !URL.TryParse(text, out var url))
                throw new ArgumentException($"{Property}: expected an absolute URL.");

            return url;

        }

        /// <summary>
        /// An optional timestamp with an explicit offset, in UTC.
        /// </summary>
        internal static DateTimeOffset? Date(JObject  JSON,
                                             String   Property)
        {

            if (JSON[Property] is not { } token || token.Type == JTokenType.Null)
                return null;

            if (token.Type == JTokenType.Date)
                return ((JValue) token).Value switch {
                           DateTimeOffset offset                                     => offset.ToUniversalTime(),
                           DateTime dateTime when dateTime.Kind != DateTimeKind.Unspecified => new DateTimeOffset(dateTime).ToUniversalTime(),
                           _                                                         => throw new ArgumentException($"{Property}: invalid timestamp.")
                       };

            if (token.Type != JTokenType.String ||
                !ExplicitOffset().IsMatch(token.Value<String>()!) ||
                !DateTimeOffset.TryParse(token.Value<String>(),
                                         CultureInfo.InvariantCulture,
                                         DateTimeStyles.RoundtripKind,
                                         out var date))
            {
                throw new ArgumentException($"{Property}: invalid timestamp.");
            }

            return date.ToUniversalTime();

        }

        [GeneratedRegex(@"(?:Z|[+-][0-9]{2}:[0-9]{2})$")]
        private static partial Regex ExplicitOffset();

        #endregion

        #region Arrays

        /// <summary>
        /// An optional array; each element's error names its index.
        /// </summary>
        internal static List<T> Array<T>(JObject          JSON,
                                         String           Property,
                                         Func<JToken, T>  Parse)
        {

            var result = new List<T>();

            if (JSON[Property] is not { } token)
                return result;

            if (token is not JArray array)
                throw new ArgumentException($"{Property}: expected an array.");

            for (var index = 0; index < array.Count; index++)
                result.Add(At($"{Property}[{index}]", () => Parse(array[index])));

            return result;

        }

        /// <summary>
        /// An identifier string within an array.
        /// </summary>
        internal static String Identifier(JToken Token)

            => Token.Type == JTokenType.String
                   ? Token.Value<String>()!
                   : throw new ArgumentException("Expected an identifier string.");

        /// <summary>
        /// Prefix an error with the path where it occurred.
        /// </summary>
        internal static T At<T>(String   Path,
                                Func<T>  Parse)
        {
            try
            {
                return Parse();
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"{Path}: {exception.Message}", exception);
            }
        }

        #endregion

        #region Names

        /// <summary>
        /// A mandatory multi-language name, e.g. {"en": "Verifier"}.
        /// </summary>
        internal static ImmutableI18NString RequiredName(JObject  JSON,
                                                         String   Property)
        {

            if (JSON[Property] is not JObject name)
                throw new ArgumentException($"{Property}: expected a multi-language object.");

            if (!I18NString.TryParse(name, out I18NString? text, out var error))
                throw new ArgumentException($"{Property}: {error}");

            if (text is null || text.Count == 0 || text.Any(pair => String.IsNullOrWhiteSpace(pair.Text)))
                throw new ArgumentException($"{Property}: expected a nonempty text in each language.");

            return new ImmutableI18NString(text);

        }

        /// <summary>
        /// A stable order of names, independent of the order of their languages.
        /// </summary>
        internal static String NameOrder(ImmutableI18NString Name)

            => String.Join("|", Name.Select(text => text.Language.ToString() + "=" + text.Text).Order(StringComparer.Ordinal));

        #endregion

        #region Context

        /// <summary>
        /// An optional JSON-LD context; when present, it must be the expected one.
        /// </summary>
        internal static void Context(JObject  JSON,
                                     String   Expected)
        {

            if (JSON[PropertyKeys.Context] is { } context &&
                (context.Type != JTokenType.String || context.Value<String>() != Expected))
                throw new ArgumentException($"Invalid '{PropertyKeys.Context}'.");

        }

        #endregion

        #region Multi-language texts

        /// <summary>
        /// A mandatory multi-language object, e.g. {"en": "Supplier"}; {} is an empty text.
        /// </summary>
        internal static ImmutableI18NString RequiredI18N(JObject  JSON,
                                                         String   Property)

            => I18N(JSON, Property) ??
               throw new ArgumentException($"{Property}: expected a multi-language object.");

        /// <summary>
        /// An optional multi-language object; null when missing or null, {} is an empty text.
        /// </summary>
        internal static ImmutableI18NString? I18N(JObject  JSON,
                                                  String   Property)
        {

            if (JSON[Property] is not { } token || token.Type == JTokenType.Null)
                return null;

            if (token is not JObject text)
                throw new ArgumentException($"{Property}: expected a multi-language object.");

            if (!text.HasValues)
                return ImmutableI18NString.Empty;

            if (!I18NString.TryParse(text, out I18NString? parsed, out var error))
                throw new ArgumentException($"{Property}: {error}");

            return new ImmutableI18NString(parsed);

        }

        #endregion

        #region Percentages

        /// <summary>
        /// The text of a percentage with its unit, e.g. "42.5 %".
        /// </summary>
        internal static String Percent(Single Value)

            => Value.ToString("R", CultureInfo.InvariantCulture) + " %";

        /// <summary>
        /// A mandatory percentage between zero and 100, written as a string with its unit, e.g. "42.5 %".
        /// </summary>
        internal static Single RequiredPercent(JObject  JSON,
                                               String   Property)
        {

            var text = JSON[Property] is { Type: JTokenType.String } token
                           ? token.Value<String>()!.Trim()
                           : null;

            if (text is null || !text.EndsWith('%'))
                throw new ArgumentException($"{Property}: expected a percentage string with '%' unit.");

            if (!Single.TryParse(text[..^1].TrimEnd(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                !Single.IsFinite(value) || value < 0 || value > 100)
                throw new ArgumentException($"{Property}: expected a percentage between zero and 100.");

            return value;

        }

        #endregion

    }

}
