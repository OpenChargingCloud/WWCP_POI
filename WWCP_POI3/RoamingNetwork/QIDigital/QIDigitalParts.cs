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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    #region QIDigitalStandard

    /// <summary>
    /// A standard or another technical specification a QI-Digital document names: one applied for a declaration or a
    /// certificate of conformity (Decision 768/2008/EC, Annex III, point 6), or the method a calibration used (the
    /// dcc:usedMethod of a PTB DCC with its dcc:norm).
    /// </summary>
    /// <param name="Reference">Its reference, with its date or version, e.g. "EN 50470-3:2022" or "PTB-A 50.7".</param>
    /// <param name="Title">Its title or the name of the method.</param>
    public sealed record QIDigitalStandard(String                Reference,
                                           ImmutableI18NString?  Title   = null)
    {

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its reference.
            /// </summary>
            public const String  Reference  = "reference";

            /// <summary>
            /// The property of its title.
            /// </summary>
            public const String  Title      = "title";

        }

        #endregion

        /// <summary>
        /// Its reference, e.g. "EN 50470-3:2022".
        /// </summary>
        public String Reference { get; }

            = !String.IsNullOrWhiteSpace(Reference) && Reference == Reference.Trim()
                  ? Reference
                  : throw new ArgumentException("The reference of a standard must neither be empty nor begin or end with whitespace!", nameof(Reference));


        #region (static) TryParse(JSON, out Standard, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a standard.
        /// </summary>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out QIDigitalStandard?  Standard,
                                       [NotNullWhen(false)] out String?             ErrorResponse)
        {

            Standard = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "a standard", out ErrorResponse, PropertyKeys.Reference, PropertyKeys.Title) ||
                    !POIJSON.TryParseI18N(JSON, PropertyKeys.Title, "title of a standard", out var title, out ErrorResponse))
                {
                    return false;
                }

                if (QIDigitalJSON.Text(JSON, PropertyKeys.Reference) is not String reference)
                {
                    ErrorResponse = "A standard requires its reference!";
                    return false;
                }

                Standard       = new QIDigitalStandard(reference, title);
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                Standard       = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this standard.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(new JProperty(PropertyKeys.Reference, Reference));

            if (Title.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Title, Title.ToJSON());

            return json;

        }

        #endregion

        /// <inheritdoc/>
        public override String ToString()
            => Reference;

    }

    #endregion

    #region QIDigitalLegislation

    /// <summary>
    /// A legislation of the European Union a declaration or a certificate of conformity is issued under: by the name the
    /// roaming network knows it by, as conformity assessment bodies are notified for it, and its act.
    /// </summary>
    /// <param name="Name">Its name, e.g. MID, as ConformityAssessmentBody.Legislations names it.</param>
    /// <param name="Act">Its act, e.g. "Directive 2014/32/EU".</param>
    public sealed record QIDigitalLegislation(String   Name,
                                              String?  Act   = null)
    {

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its name.
            /// </summary>
            public const String  Name  = "name";

            /// <summary>
            /// The property of its act.
            /// </summary>
            public const String  Act   = "act";

        }

        #endregion

        #region Well-known legislations

        /// <summary>
        /// The Measuring Instruments Directive.
        /// </summary>
        public static readonly QIDigitalLegislation MID   = new (ConformityAssessmentBody.Legislations.MID, "Directive 2014/32/EU");

        /// <summary>
        /// The Low Voltage Directive.
        /// </summary>
        public static readonly QIDigitalLegislation LVD   = new ("LVD",  "Directive 2014/35/EU");

        /// <summary>
        /// The Electromagnetic Compatibility Directive.
        /// </summary>
        public static readonly QIDigitalLegislation EMC   = new ("EMC",  "Directive 2014/30/EU");

        /// <summary>
        /// The Radio Equipment Directive.
        /// </summary>
        public static readonly QIDigitalLegislation RED   = new ("RED",  "Directive 2014/53/EU");

        /// <summary>
        /// The Restriction of Hazardous Substances Directive.
        /// </summary>
        public static readonly QIDigitalLegislation RoHS  = new ("RoHS", "Directive 2011/65/EU");

        /// <summary>
        /// The Cyber Resilience Act.
        /// </summary>
        public static readonly QIDigitalLegislation CRA   = new (ConformityAssessmentBody.Legislations.CRA, "Regulation (EU) 2024/2847");

        #endregion

        /// <summary>
        /// Its name, e.g. MID.
        /// </summary>
        public String Name { get; }

            = !String.IsNullOrWhiteSpace(Name) && Name == Name.Trim()
                  ? Name
                  : throw new ArgumentException("The name of a legislation must neither be empty nor begin or end with whitespace!", nameof(Name));


        #region (static) TryParse(JSON, out Legislation, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a legislation.
        /// </summary>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out QIDigitalLegislation?  Legislation,
                                       [NotNullWhen(false)] out String?                ErrorResponse)
        {

            Legislation = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "a legislation", out ErrorResponse, PropertyKeys.Name, PropertyKeys.Act) ||
                    !POIJSON.TryParseString(JSON, PropertyKeys.Act, "act of a legislation", out var act, out ErrorResponse))
                {
                    return false;
                }

                if (QIDigitalJSON.Text(JSON, PropertyKeys.Name) is not String name)
                {
                    ErrorResponse = "A legislation requires its name!";
                    return false;
                }

                Legislation    = new QIDigitalLegislation(name, act);
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                Legislation    = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this legislation.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(new JProperty(PropertyKeys.Name, Name));

            if (Act is not null)
                json.Add(PropertyKeys.Act, Act);

            return json;

        }

        #endregion

        /// <inheritdoc/>
        public override String ToString()
            => Act is not null ? $"{Name} ({Act})" : Name;

    }

    #endregion

    #region QIDigitalPerson

    /// <summary>
    /// A person responsible for a QI-Digital document: one of the responsible persons of a PTB DCC (dcc:respPerson), the
    /// one of them signing it as its main signer, or the person signing a declaration of conformity for and on behalf of
    /// its manufacturer (Decision 768/2008/EC, Annex III), with the function as its role.
    /// </summary>
    /// <param name="Name">The name of the person.</param>
    /// <param name="Role">The role or function of the person, e.g. "Head of laboratory".</param>
    /// <param name="MainSigner">Whether the person is the main signer.</param>
    public sealed record QIDigitalPerson(String   Name,
                                         String?  Role         = null,
                                         Boolean  MainSigner   = false)
    {

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of the name of the person.
            /// </summary>
            public const String  Name        = "name";

            /// <summary>
            /// The property of the role of the person.
            /// </summary>
            public const String  Role        = "role";

            /// <summary>
            /// The property of whether the person is the main signer.
            /// </summary>
            public const String  MainSigner  = "mainSigner";

        }

        #endregion

        /// <summary>
        /// The name of the person.
        /// </summary>
        public String Name { get; }

            = !String.IsNullOrWhiteSpace(Name) && Name == Name.Trim()
                  ? Name
                  : throw new ArgumentException("The name of a person must neither be empty nor begin or end with whitespace!", nameof(Name));


        #region (static) TryParse(JSON, out Person, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a person.
        /// </summary>
        public static Boolean TryParse(JObject                                    JSON,
                                       [NotNullWhen(true)]  out QIDigitalPerson?  Person,
                                       [NotNullWhen(false)] out String?           ErrorResponse)
        {

            Person = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "a person", out ErrorResponse, PropertyKeys.Name, PropertyKeys.Role, PropertyKeys.MainSigner) ||
                    !POIJSON.TryParseString(JSON, PropertyKeys.Role, "role of a person", out var role, out ErrorResponse))
                {
                    return false;
                }

                if (QIDigitalJSON.Text(JSON, PropertyKeys.Name) is not String name)
                {
                    ErrorResponse = "A person requires a name!";
                    return false;
                }

                if (JSON[PropertyKeys.MainSigner] is JToken mainSigner && mainSigner.Type != JTokenType.Boolean)
                {
                    ErrorResponse = "Whether a person is the main signer must be true or false!";
                    return false;
                }

                Person         = new QIDigitalPerson(name, role, JSON[PropertyKeys.MainSigner]?.Value<Boolean>() ?? false);
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                Person         = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this person.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(new JProperty(PropertyKeys.Name, Name));

            if (Role is not null)
                json.Add(PropertyKeys.Role,        Role);

            if (MainSigner)
                json.Add(PropertyKeys.MainSigner,  true);

            return json;

        }

        #endregion

        /// <inheritdoc/>
        public override String ToString()
            => Role is not null ? $"{Name}, {Role}" : Name;

    }

    #endregion

}
