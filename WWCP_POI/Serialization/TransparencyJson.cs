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
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// JSON value parsing and license comparison shared by transparency software and its status.
    /// </summary>
    internal static class TransparencyJson
    {

        #region Parse text, links and dates

        internal static String RequiredText(JObject JSON, String Field)

            => InfrastructureJson.Text(JSON, Field) is { } text && !String.IsNullOrWhiteSpace(text)
                   ? text
                   : throw new ArgumentException($"{Field}: expected a nonempty string.");

        internal static URL? Link(JObject JSON, String Field)
        {

            var text = InfrastructureJson.Text(JSON, Field);

            if (text is null)
                return null;

            if (!Uri.TryCreate(text, UriKind.Absolute, out _) || !URL.TryParse(text, out var url))
                throw new ArgumentException($"{Field}: expected an absolute URL.");

            return url;

        }

        internal static DateTimeOffset? Date(JObject JSON, String Field)
        {

            if (JSON[Field] is not { } token || token.Type == JTokenType.Null)
                return null;

            if (token.Type == JTokenType.Date)
                return InfrastructureJson.Date(JSON, Field)?.ToUniversalTime();

            if (token.Type != JTokenType.String ||
                !DateTimeOffset.TryParse(token.Value<String>(),
                                         CultureInfo.InvariantCulture,
                                         DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                         out var date))
            {
                throw new ArgumentException($"{Field}: invalid timestamp.");
            }

            return date;

        }

        #endregion

        #region Parse licenses

        internal static OpenSourceLicense License(JObject JSON)
        {

            var current = JSON["openSourceLicense"];
            var legacy  = JSON["open_source_license"];

            if (current is not null && legacy is not null)
                throw new ArgumentException("openSourceLicense: specify only one license representation.");

            var token = current ?? legacy ??
                        throw new ArgumentException("openSourceLicense: missing license.");

            return token is JObject document
                       ? ParseLicenseObject(document)
                       : ParseLegacyLicense(token);

        }

        private static OpenSourceLicense ParseLicenseObject(JObject JSON)
        {

            InfrastructureJson.Validate(JSON, OpenSourceLicense.JSONLDContext);

            var text = InfrastructureJson.Text(JSON, "@id") ??
                       InfrastructureJson.Text(JSON, "id");

            if (text is null || !OpenSourceLicense_Id.TryParse(text, out var id))
                throw new ArgumentException("openSourceLicense.@id: invalid or missing license identifier.");

            if (JSON["id"] is not null &&
                (!OpenSourceLicense_Id.TryParse(RequiredText(JSON, "id"), out var other) || !id.Equals(other)))
            {
                throw new ArgumentException("openSourceLicense: conflicting license identifiers.");
            }

            var links = InfrastructureJson.Array(JSON, "URLs", token =>
            {
                var document = new JObject(new JProperty("url", token.DeepClone()));

                return Link(document, "url") ??
                       throw new ArgumentException("URLs: expected a URL.");
            });

            return new OpenSourceLicense(id,
                                         InfrastructureJson.Name(JSON, "description") ?? I18NString.Empty,
                                         links.ToArray());

        }

        private static OpenSourceLicense ParseLegacyLicense(JToken Token)
        {

            if (Token.Type != JTokenType.String || String.IsNullOrWhiteSpace(Token.Value<String>()))
                throw new ArgumentException("openSourceLicense: expected a license object or legacy string.");

            var value      = Token.Value<String>()!;
            var separator  = value.IndexOf(": ", StringComparison.Ordinal);
            var identifier = separator < 0 ? value : value[..separator];

            if (!OpenSourceLicense_Id.TryParse(identifier, out var licenseId))
                throw new ArgumentException("openSourceLicense: invalid license identifier.");

            var predefined = typeof(OpenSourceLicense).
                                 GetFields(BindingFlags.Public | BindingFlags.Static).
                                 Select(field => field.GetValue(null)).
                                 OfType<OpenSourceLicense>().
                                 FirstOrDefault(license => license.Id.Equals(licenseId));

            return predefined?.Clone() ??
                   new OpenSourceLicense(licenseId,
                                         separator < 0
                                             ? I18NString.Empty
                                             : I18NString.Create(value[(separator + 2)..]));

        }

        #endregion

        #region License ordering and hashing

        /// <summary>
        /// Create a stable comparison representation including description and URLs.
        /// </summary>
        internal static String LicenseOrder(OpenSourceLicense License)

            => new JObject(
                   new JProperty("id",          License.Id.ToString().ToUpperInvariant()),
                   new JProperty("description", LicenseDescription(License)),
                   new JProperty("URLs",        new JArray(License.URLs.
                                                              Select(url => url.ToString()).
                                                              Order(StringComparer.Ordinal)))
               ).ToString(Formatting.None);

        internal static Int32 LicenseHash(OpenSourceLicense License)
        {

            var description = LicenseDescription(License).ToString(Formatting.None);

            // URL sets are unordered; URL equality ignores scheme and host casing.
            var urlsHash = License.URLs.Aggregate(0, (hash, url) => hash ^ url.GetHashCode());

            return HashCode.Combine(
                       StringComparer.OrdinalIgnoreCase.GetHashCode(License.Id.ToString()),
                       StringComparer.Ordinal.GetHashCode(description),
                       urlsHash
                   );

        }

        private static JObject LicenseDescription(OpenSourceLicense License)

            => new (License.Description.ToJSON().
                            Properties().
                            OrderBy(property => property.Name, StringComparer.Ordinal).
                            Select(property => new JProperty(property.Name, property.Value.DeepClone())));

        #endregion

    }

}
