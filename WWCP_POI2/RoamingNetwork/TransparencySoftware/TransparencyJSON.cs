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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Strict JSON reading and stable ordering of the open source licenses of transparency software.
    /// </summary>
    internal static class TransparencyJSON
    {

        #region Open source licenses

        /// <summary>
        /// The mandatory, nonempty array of complete open source license objects.
        /// </summary>
        internal static List<OpenSourceLicense> Licenses(JObject  JSON,
                                                         String   Property)
        {

            if (JSON[Property] is not JArray array || array.Count == 0)
                throw new ArgumentException($"{Property}: expected a nonempty array of license objects.");

            return StrictJSON.Array(JSON, Property, token => License(new JObject(new JProperty("license", token.DeepClone())), "license"));

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

            StrictJSON.OnlyProperties(license, "@id", "@context", "description", "URLs");

            var text = StrictJSON.Text(license, "@id");
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

            var urls = StrictJSON.Array(license, "URLs", token => StrictJSON.Link(new JObject(new JProperty("url", token.DeepClone())), "url") ??
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
