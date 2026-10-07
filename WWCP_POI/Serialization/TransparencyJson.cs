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

            if (JSON[Field]?.Type == JTokenType.Null) return null;
            return InfrastructureJson.Date(JSON, Field)?.ToUniversalTime();

        }

        #endregion

        #region Parse licenses

        internal static OpenSourceLicense License(JObject JSON)
        {

            var document = InfrastructureJson.Object(JSON, "openSourceLicense") ??
                           throw new ArgumentException("openSourceLicense: missing license object.");
            return ParseLicenseObject(document);

        }

        private static OpenSourceLicense ParseLicenseObject(JObject JSON)
        {

            InfrastructureJson.Validate(JSON, OpenSourceLicense.JSONLDContext);

            InfrastructureJson.ValidateFields(JSON, "@id", "@context", "description", "URLs");
            var text = InfrastructureJson.Text(JSON, "@id");

            if (text is null || !OpenSourceLicense_Id.TryParse(text, out var id))
                throw new ArgumentException("openSourceLicense.@id: invalid or missing license identifier.");

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
