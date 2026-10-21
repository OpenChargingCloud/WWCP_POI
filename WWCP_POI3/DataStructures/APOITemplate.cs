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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A template giving the members naming it in their templateIds properties, which they take at runtime: of the
    /// templates of a member, the first one giving a value decides.
    /// </summary>
    public interface IPOITemplate
    {

        /// <summary>
        /// The identification of the template, as a text.
        /// </summary>
        String          TemplateId      { get; }

        /// <summary>
        /// The timestamp of its last change.
        /// </summary>
        DateTimeOffset  LastChangeDate  { get; }

        /// <summary>
        /// Its JSON representation, writing the properties it gives its members as they write them.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        JObject         ToJSON(Boolean Embedded = false);

    }


    /// <summary>
    /// The frame of every template: an identification, a name, a description, a data source, timestamps and custom data,
    /// content as every entity has it, but no runtime status: a template is no part of the infrastructure, only what
    /// members naming it share.
    /// </summary>
    /// <typeparam name="TId">The type of its identification.</typeparam>
    public abstract class APOITemplate<TId> : AImmutableInternalData,
                                              IPOITemplate

        where TId : IId

    {

        #region Properties

        #region (static) class BasePropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization every template shares.
        /// </summary>
        public static class BasePropertyKeys
        {

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name          = "name";

            /// <summary>
            /// The property of its multilingual description.
            /// </summary>
            public const String  Description   = "description";

            /// <summary>
            /// The property of its data source.
            /// </summary>
            public const String  DataSource    = "dataSource";

        }

        #endregion


        /// <summary>
        /// The identification of the template.
        /// </summary>
        public TId                  Id           { get; }

        /// <summary>
        /// The multi-language name of the template.
        /// </summary>
        public ImmutableI18NString  Name         { get; }

        /// <summary>
        /// The multi-language description of the template.
        /// </summary>
        public ImmutableI18NString  Description  { get; }

        /// <summary>
        /// The source of this information, e.g. the WWCP importer used.
        /// </summary>
        public String?              DataSource   { get; }

        /// <inheritdoc/>
        String IPOITemplate.TemplateId
            => Id.ToString() ?? "";

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new template.
        /// </summary>
        protected APOITemplate(TId                     Id,
                               ImmutableI18NString?    Name,
                               ImmutableI18NString?    Description,
                               String?                 DataSource,
                               DateTimeOffset?         Created,
                               DateTimeOffset?         LastChange,
                               CustomDataNew?          CustomData,
                               UserDefinedDictionary?  InternalData)

            : base(CustomData,
                   InternalData,
                   Created,
                   LastChange)

        {

            if (Id.IsNullOrEmpty)
                throw new ArgumentNullException(nameof(Id), "The given identification must not be null or empty!");

            this.Id           = Id;
            this.Name         = Name        ?? ImmutableI18NString.Empty;
            this.Description  = Description ?? ImmutableI18NString.Empty;
            this.DataSource   = DataSource;

        }

        #endregion


        #region ToJSON(Embedded = false)

        /// <inheritdoc/>
        public abstract JObject ToJSON(Boolean Embedded = false);

        #endregion

        #region (protected) AddFrameTo(JSON)

        /// <summary>
        /// Write the name, description and data source of this template into the given JSON object.
        /// </summary>
        protected void AddFrameTo(JObject JSON)
        {

            if (Name.IsNotNullOrEmpty())
                JSON.Add(BasePropertyKeys.Name,         Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                JSON.Add(BasePropertyKeys.Description,  Description.ToJSON());

            if (DataSource is not null)
                JSON.Add(BasePropertyKeys.DataSource,   DataSource);

        }

        #endregion

        #region (override) ToString()

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString() ?? "";

        #endregion

    }

}
