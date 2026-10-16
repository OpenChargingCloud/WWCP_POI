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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A brand, embedded completely in what it is a brand of, and meaning something of its own on every level:
    /// a charging station operator is a white-label solution for it, a charging pool, charging station or EVSE
    /// also appears under it, an RFID authentication mode takes its cards. The levels are not mixed: a brand of an
    /// operator is no brand of its pools, nor the other way round. Without runtime status and timestamps.
    /// </summary>
    public sealed class Brand : IEquatable<Brand>,
                                IComparable<Brand>
    {

        #region Properties

        /// <summary>
        /// The identification of the brand.
        /// </summary>
        public Brand_Id              Id             { get; }

        /// <summary>
        /// The multi-language name; at least one.
        /// </summary>
        public ImmutableI18NString   Name           { get; }

        /// <summary>
        /// The multi-language description.
        /// </summary>
        public ImmutableI18NString?  Description    { get; }

        /// <summary>
        /// The logo.
        /// </summary>
        public URL?                  Logo           { get; }

        /// <summary>
        /// The homepage.
        /// </summary>
        public URL?                  Homepage       { get; }

        /// <summary>
        /// Custom data.
        /// </summary>
        public CustomDataNew?        CustomData     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new brand.
        /// </summary>
        public Brand(Brand_Id              Id,
                     ImmutableI18NString   Name,
                     ImmutableI18NString?  Description   = null,
                     URL?                  Logo          = null,
                     URL?                  Homepage      = null,
                     CustomDataNew?        CustomData    = null)
        {

            if (Name is null || Name.IsNullOrEmpty())
                throw new ArgumentException($"The brand '{Id}' needs its name!", nameof(Name));

            this.Id           = Id;
            this.Name         = Name;
            this.Description  = Description;
            this.Logo         = Logo;
            this.Homepage     = Homepage;
            this.CustomData   = CustomData;

        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this brand, as its roaming network embeds it.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id",   Id.  ToString()),
                           new JProperty("name",  Name.ToJSON())
                       );

            if (Description.IsNotNullOrEmpty())
                json.Add("description",  Description.ToJSON());

            if (Logo.HasValue)
                json.Add("logo",         Logo.Value.ToString());

            if (Homepage.HasValue)
                json.Add("homepage",     Homepage.Value.ToString());

            if (CustomData is not null && CustomData.HasValues)
                json.Add("customData",   CustomData.ToJObject());

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out Brand, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a brand.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Brand">The parsed brand.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                           JSON,
                                       [NotNullWhen(true)]  out Brand?   Brand,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Brand = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "brand identification",
                                         Brand_Id.TryParse,
                                         out Brand_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N(JSON, "name",         "brand name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N(JSON, "description",  "brand description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseText(JSON, "logo",         "logo",               URL.TryParse, out URL? logo,      out ErrorResponse) ||
                    !POIJSON.TryParseText(JSON, "homepage",     "homepage",           URL.TryParse, out URL? homepage,  out ErrorResponse))
                {
                    return false;
                }

                if (name is null || name.IsNullOrEmpty())
                {
                    ErrorResponse = $"The brand '{id}' needs its name!";
                    return false;
                }

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                Brand          = new Brand(id, name, description, logo, homepage, customData);
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                Brand          = null;
                ErrorResponse  = "The given JSON representation of a brand is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two brands for equality by their identifications.
        /// </summary>
        public static Boolean operator == (Brand? Brand1, Brand? Brand2)

            => ReferenceEquals(Brand1, Brand2) ||
               (Brand1 is not null && Brand1.Equals(Brand2));

        /// <summary>
        /// Compares two brands for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (Brand? Brand1, Brand? Brand2)

            => !(Brand1 == Brand2);

        /// <inheritdoc/>
        public Int32 CompareTo(Brand? Brand)

            => Brand is null
                   ? throw new ArgumentNullException(nameof(Brand), "The given brand must not be null!")
                   : Id.CompareTo(Brand.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is Brand brand &&
               Equals(brand);

        /// <inheritdoc/>
        public Boolean Equals(Brand? Brand)

            => Brand is not null &&
               Id.Equals(Brand.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
