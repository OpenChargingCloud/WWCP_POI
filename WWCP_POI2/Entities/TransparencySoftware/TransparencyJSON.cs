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
    /// Strict JSON reading for transparency software, its certificates and its status:
    /// unknown properties, wrong types and empty texts are errors.
    /// </summary>
    internal static partial class TransparencyJSON
    {

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

        #region Open source licenses

        /// <summary>
        /// The mandatory, nonempty array of complete open source license objects.
        /// </summary>
        internal static List<OpenSourceLicense> Licenses(JObject  JSON,
                                                         String   Property)
        {

            if (JSON[Property] is not JArray array || array.Count == 0)
                throw new ArgumentException($"{Property}: expected a nonempty array of license objects.");

            return Array(JSON, Property, token => License(new JObject(new JProperty("license", token.DeepClone())), "license"));

        }

        /// <summary>
        /// The mandatory, complete open source license object.
        /// </summary>
        internal static OpenSourceLicense License(JObject  JSON,
                                                  String   Property)
        {

            if (JSON[Property] is not { } token || token.Type == JTokenType.Null)
                throw new ArgumentException($"{Property}: missing license object.");

            if (token is not JObject license)
                throw new ArgumentException($"{Property}: expected an object.");

            if (license["exception"] is not null)
                throw new ArgumentException("Cannot parse a serializer error document.");

            if (license["@context"] is { } context &&
                (context.Type != JTokenType.String || context.Value<String>() != OpenSourceLicense.JSONLDContext))
                throw new ArgumentException("Invalid '@context'.");

            OnlyProperties(license, "@id", "@context", "description", "URLs");

            var text = Text(license, "@id");
            if (text is null || !OpenSourceLicense_Id.TryParse(text, out var id))
                throw new ArgumentException($"{Property}.@id: invalid or missing license identifier.");

            var description = I18NString.Empty;
            if (license["description"] is { } descriptionToken && descriptionToken.Type != JTokenType.Null)
            {

                if (descriptionToken is not JObject descriptionJSON)
                    throw new ArgumentException("description: expected an object.");

                if (descriptionJSON.HasValues)
                {

                    if (!I18NString.TryParse(descriptionJSON, out I18NString? parsed, out var error))
                        throw new ArgumentException($"description: {error}");

                    description = parsed;

                }

            }

            var urls = Array(license, "URLs", token => Link(new JObject(new JProperty("url", token.DeepClone())), "url") ??
                                                       throw new ArgumentException("URLs: expected a URL."));

            return new OpenSourceLicense(id, description, [.. urls]);

        }

        /// <summary>
        /// A stable order of licenses, including their description and URLs.
        /// </summary>
        internal static String LicenseOrder(OpenSourceLicense License)

            => new JObject(
                   new JProperty("id",           License.Id.ToString().ToUpperInvariant()),
                   new JProperty("description",  LicenseDescription(License)),
                   new JProperty("URLs",         new JArray(License.URLs.
                                                                Select(url => url.ToString()).
                                                                Order(StringComparer.Ordinal)))
               ).ToString(Formatting.None);

        /// <summary>
        /// A hash code matching the license order: URL sets are unordered,
        /// and URLs ignore the case of their scheme and host.
        /// </summary>
        internal static Int32 LicenseHash(OpenSourceLicense License)

            => HashCode.Combine(
                   StringComparer.OrdinalIgnoreCase.GetHashCode(License.Id.ToString()),
                   StringComparer.Ordinal.          GetHashCode(LicenseDescription(License).ToString(Formatting.None)),
                   License.URLs.Aggregate(0, (hash, url) => hash ^ url.GetHashCode())
               );

        private static JObject LicenseDescription(OpenSourceLicense License)

            => new (License.Description.ToJSON().
                        Properties().
                        OrderBy(property => property.Name, StringComparer.Ordinal).
                        Select (property => new JProperty(property.Name, property.Value.DeepClone())));

        #endregion

    }

}
