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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A release of charging transparency software, e.g. for the German calibration law, embedded in its
    /// manufacturer: one per version of a software. Besides what it is, it names what the EU Cyber Resilience Act
    /// asks its manufacturer to tell its users about it (Regulation (EU) 2024/2847, Annex II): the end of its
    /// support period, its EU declaration of conformity and its software bill of materials.
    /// </summary>
    public sealed partial class TransparencySoftware : IEquatable<TransparencySoftware>,
                                                       IComparable<TransparencySoftware>,
                                                       IComparable
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/transparencySoftware/v1";

        /// <summary>
        /// The licenses in their canonical order; licenses are mutable, so they are copied on access.
        /// </summary>
        private readonly ImmutableArray<OpenSourceLicense> openSourceLicenses;

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its identification.
            /// </summary>
            public const String  Id                     = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context                = "@context";

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name                   = "name";

            /// <summary>
            /// The property of its version.
            /// </summary>
            public const String  Version                = "version";

            /// <summary>
            /// The property of its Open Source licenses.
            /// </summary>
            public const String  OpenSourceLicenses     = "openSourceLicenses";

            /// <summary>
            /// The property of its logo URL.
            /// </summary>
            public const String  Logo                   = "logo";

            /// <summary>
            /// The property of its how-to-use URL.
            /// </summary>
            public const String  HowToUse               = "howToUse";

            /// <summary>
            /// The property of its more-information URL.
            /// </summary>
            public const String  MoreInformation        = "moreInformation";

            /// <summary>
            /// The property of its source code repository URL.
            /// </summary>
            public const String  SourceCodeRepository   = "sourceCodeRepository";

            /// <summary>
            /// The property of the end of its support period (CRA, Article 13(8) and Annex II, point 7).
            /// </summary>
            public const String  SupportUntil              = "supportUntil";

            /// <summary>
            /// The property of the URL of its EU declaration of conformity (CRA, Article 28 and Annex II, point 6).
            /// </summary>
            public const String  DeclarationOfConformity   = "declarationOfConformity";

            /// <summary>
            /// The property of the URL of its software bill of materials, when its manufacturer makes it available
            /// to its users (CRA, Annex I, Part II, point 1, and Annex II, point 9).
            /// </summary>
            public const String  SoftwareBillOfMaterials   = "softwareBillOfMaterials";

        }

        #endregion

        /// <summary>
        /// The stable identity of this software release, shared by all referring meters.
        /// </summary>
        [Mandatory]
        public TransparencySoftware_Id  Id                      { get; }

        /// <summary>
        /// The multi-language name of the transparency software.
        /// </summary>
        [Mandatory]
        public ImmutableI18NString             Name                    { get; }

        /// <summary>
        /// The version of the transparency software.
        /// </summary>
        [Mandatory]
        public TransparencySoftwareVersion     Version                 { get; }

        /// <summary>
        /// The Open Source licenses of the transparency software, at least one.
        /// </summary>
        [Mandatory]
        public IEnumerable<OpenSourceLicense>  OpenSourceLicenses
            => openSourceLicenses.Select(openSourceLicense => openSourceLicense.Clone());

        /// <summary>
        /// The optional URL where to find a small logo of the transparency software.
        /// </summary>
        [Optional]
        public URL?                            Logo                    { get; }

        /// <summary>
        /// The optional URL where to find a manual how to use the transparency software.
        /// </summary>
        [Optional]
        public URL?                            HowToUse                { get; }

        /// <summary>
        /// The optional URL where to find more information about the transparency software.
        /// </summary>
        [Optional]
        public URL?                            MoreInformation         { get; }

        /// <summary>
        /// The optional URL where to find the source code of the transparency software.
        /// </summary>
        [Optional]
        public URL?                            SourceCodeRepository    { get; }

        /// <summary>
        /// The optional end of the support period of this release, in UTC to the millisecond: until when its
        /// manufacturer handles its vulnerabilities and provides security updates for it (CRA, Article 13(8)).
        /// </summary>
        [Optional]
        public DateTimeOffset?                 SupportUntil            { get; }

        /// <summary>
        /// The optional URL of the EU declaration of conformity of this release (CRA, Article 28).
        /// </summary>
        [Optional]
        public URL?                            DeclarationOfConformity { get; }

        /// <summary>
        /// The optional URL of the software bill of materials of this release.
        /// </summary>
        [Optional]
        public URL?                            SoftwareBillOfMaterials { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create new charging transparency software.
        /// </summary>
        /// <param name="Id">The stable identity of this software release.</param>
        /// <param name="Name">The multi-language name of the transparency software.</param>
        /// <param name="Version">The version of the transparency software.</param>
        /// <param name="OpenSourceLicenses">The Open Source licenses of the transparency software, at least one.</param>
        ///
        /// <param name="Logo">An optional URL where to find a small logo of the transparency software.</param>
        /// <param name="HowToUse">An optional URL where to find a manual how to use the transparency software.</param>
        /// <param name="MoreInformation">An optional URL where to find more information about the transparency software.</param>
        /// <param name="SourceCodeRepository">An optional URL where to find the source code of the transparency software.</param>
        /// <param name="SupportUntil">The optional end of the support period of this release.</param>
        /// <param name="DeclarationOfConformity">The optional URL of the EU declaration of conformity of this release.</param>
        /// <param name="SoftwareBillOfMaterials">The optional URL of the software bill of materials of this release.</param>
        public TransparencySoftware(TransparencySoftware_Id         Id,
                                    ImmutableI18NString             Name,
                                    TransparencySoftwareVersion     Version,
                                    IEnumerable<OpenSourceLicense>  OpenSourceLicenses,

                                    URL?                            Logo                   = null,
                                    URL?                            HowToUse               = null,
                                    URL?                            MoreInformation        = null,
                                    URL?                            SourceCodeRepository      = null,
                                    DateTimeOffset?                 SupportUntil              = null,
                                    URL?                            DeclarationOfConformity   = null,
                                    URL?                            SoftwareBillOfMaterials   = null)
        {

            if (Id.IsNullOrEmpty)
                throw new ArgumentException("A software identifier is required.", nameof(Id));

            ArgumentNullException.ThrowIfNull(Name);

            if (Name.IsNullOrEmpty() || Name.Any(text => String.IsNullOrWhiteSpace(text.Text)))
                throw new ArgumentException("A name with a nonempty text in each language is required.", nameof(Name));

            if (Version.IsNullOrEmpty)
                throw new ArgumentException("A software version is required.", nameof(Version));

            ArgumentNullException.ThrowIfNull(OpenSourceLicenses);

            var licenses = OpenSourceLicenses.ToArray();

            if (licenses.Length == 0)
                throw new ArgumentException("At least one license is required.", nameof(OpenSourceLicenses));

            if (licenses.Any(license => license is null || license.Id.IsNullOrEmpty))
                throw new ArgumentException("The license identifier must not be empty.", nameof(OpenSourceLicenses));

            if (licenses.Select(license => license.Id.ToString()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != licenses.Length)
                throw new ArgumentException("A license must not be given twice.", nameof(OpenSourceLicenses));

            foreach (var link in new[] { Logo, HowToUse, MoreInformation, SourceCodeRepository, DeclarationOfConformity, SoftwareBillOfMaterials })
                if (link is { } url && !Uri.TryCreate(url.ToString(), UriKind.Absolute, out _))
                    throw new ArgumentException("Software links must be absolute URLs.");

            this.Id                    = Id;
            this.Name                  = Name;
            this.Version               = Version;
            this.openSourceLicenses    = [.. licenses.Select (license => license.Clone()).
                                                      OrderBy(TransparencyJSON.LicenseOrder, StringComparer.Ordinal)];

            this.Logo                  = Logo;
            this.HowToUse              = HowToUse;
            this.MoreInformation       = MoreInformation;
            this.SourceCodeRepository  = SourceCodeRepository;
            this.SupportUntil          = SupportUntil.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(SupportUntil.Value.ToUnixTimeMilliseconds()) : null;
            this.DeclarationOfConformity  = DeclarationOfConformity;
            this.SoftwareBillOfMaterials  = SoftwareBillOfMaterials;

        }

        #endregion


        #region (static) Parse   (JSON, CustomTransparencySoftwareParser = null)

        /// <summary>
        /// Parse the given JSON representation of a transparency software.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="CustomTransparencySoftwareParser">An optional delegate to parse custom transparency software JSON objects.</param>
        public static TransparencySoftware Parse(JObject                                             JSON,
                                                 CustomJObjectParserDelegate<TransparencySoftware>?  CustomTransparencySoftwareParser = null)
        {

            if (TryParse(JSON,
                         out var transparencySoftware,
                         out var errorResponse,
                         CustomTransparencySoftwareParser))
            {
                return transparencySoftware!;
            }

            throw new ArgumentException("The given JSON representation of a transparency software is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out TransparencySoftware, out ErrorResponse, CustomTransparencySoftwareParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a transparency software.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="TransparencySoftware">The parsed transparency software.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                        JSON,
                                       [NotNullWhen(true)] out TransparencySoftware?  TransparencySoftware,
                                       [NotNullWhen(false)] out String?               ErrorResponse)

            => TryParse(JSON,
                        out TransparencySoftware,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a transparency software.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="TransparencySoftware">The parsed transparency software.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomTransparencySoftwareParser">An optional delegate to parse custom transparency software JSON objects.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       [NotNullWhen(true)] out TransparencySoftware?       TransparencySoftware,
                                       [NotNullWhen(false)] out String?                    ErrorResponse,
                                       CustomJObjectParserDelegate<TransparencySoftware>?  CustomTransparencySoftwareParser = null)
            => TryParseDocument(JSON, out TransparencySoftware, out ErrorResponse, CustomTransparencySoftwareParser);

        #endregion

        #region ToJSON(Embedded = false, CustomTransparencySoftwareSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomTransparencySoftwareSerializer">A delegate to serialize custom transparency software JSON objects.</param>
        public JObject ToJSON(Boolean                                                 Embedded                               = false,
                              CustomJObjectSerializerDelegate<TransparencySoftware>?  CustomTransparencySoftwareSerializer   = null)
        {

            var JSON = JSONObject.Create(

                                 new JProperty(PropertyKeys.Id,                     Id.     ToString()),

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,               JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.Name,                   Name.    ToJSON()),
                                 new JProperty(PropertyKeys.Version,                Version. ToString()),
                                 new JProperty(PropertyKeys.OpenSourceLicenses,     new JArray(openSourceLicenses.Select(license => license.ToJSON(Embedded: true)))),

                           Logo.                HasValue
                               ? new JProperty(PropertyKeys.Logo,                   Logo.                ToString())
                               : null,

                           HowToUse.            HasValue
                               ? new JProperty(PropertyKeys.HowToUse,               HowToUse.            ToString())
                               : null,

                           MoreInformation.     HasValue
                               ? new JProperty(PropertyKeys.MoreInformation,        MoreInformation.     ToString())
                               : null,

                           SourceCodeRepository.HasValue
                               ? new JProperty(PropertyKeys.SourceCodeRepository,   SourceCodeRepository.ToString())
                               : null,

                           SupportUntil.        HasValue
                               ? new JProperty(PropertyKeys.SupportUntil,           SupportUntil.Value.ToISO8601())
                               : null,

                           DeclarationOfConformity.HasValue
                               ? new JProperty(PropertyKeys.DeclarationOfConformity, DeclarationOfConformity.ToString())
                               : null,

                           SoftwareBillOfMaterials.HasValue
                               ? new JProperty(PropertyKeys.SoftwareBillOfMaterials, SoftwareBillOfMaterials.ToString())
                               : null

                       );

            return CustomTransparencySoftwareSerializer is not null
                       ? CustomTransparencySoftwareSerializer(this, JSON)
                       : JSON;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this transparency software.
        /// </summary>
        public TransparencySoftware Clone()

            => new (
                   Id,
                   Name,
                   Version,
                   openSourceLicenses,
                   Logo?.                   Clone(),
                   HowToUse?.               Clone(),
                   MoreInformation?.        Clone(),
                   SourceCodeRepository?.   Clone(),
                   SupportUntil,
                   DeclarationOfConformity?.Clone(),
                   SoftwareBillOfMaterials?.Clone()
               );

        #endregion


        #region Operator overloading

        #region Operator == (TransparencySoftware1, TransparencySoftware2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftware1">A transparency software.</param>
        /// <param name="TransparencySoftware2">Another transparency software.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (TransparencySoftware TransparencySoftware1,
                                           TransparencySoftware TransparencySoftware2)
        {

            if (Object.ReferenceEquals(TransparencySoftware1, TransparencySoftware2))
                return true;

            if (TransparencySoftware1 is null || TransparencySoftware2 is null)
                return false;

            return TransparencySoftware1.Equals(TransparencySoftware2);

        }

        #endregion

        #region Operator != (TransparencySoftware1, TransparencySoftware2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftware1">A transparency software.</param>
        /// <param name="TransparencySoftware2">Another transparency software.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (TransparencySoftware TransparencySoftware1,
                                           TransparencySoftware TransparencySoftware2)

            => !(TransparencySoftware1 == TransparencySoftware2);

        #endregion

        #region Operator <  (TransparencySoftware1, TransparencySoftware2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftware1">A transparency software.</param>
        /// <param name="TransparencySoftware2">Another transparency software.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (TransparencySoftware TransparencySoftware1,
                                          TransparencySoftware TransparencySoftware2)

            => TransparencySoftware1 is null
                   ? throw new ArgumentNullException(nameof(TransparencySoftware1), "The give transparency software must not be null!")
                   : TransparencySoftware1.CompareTo(TransparencySoftware2) < 0;

        #endregion

        #region Operator <= (TransparencySoftware1, TransparencySoftware2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftware1">A transparency software.</param>
        /// <param name="TransparencySoftware2">Another transparency software.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (TransparencySoftware TransparencySoftware1,
                                           TransparencySoftware TransparencySoftware2)

            => !(TransparencySoftware1 > TransparencySoftware2);

        #endregion

        #region Operator >  (TransparencySoftware1, TransparencySoftware2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftware1">A transparency software.</param>
        /// <param name="TransparencySoftware2">Another transparency software.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (TransparencySoftware TransparencySoftware1,
                                          TransparencySoftware TransparencySoftware2)

            => TransparencySoftware1 is null
                   ? throw new ArgumentNullException(nameof(TransparencySoftware1), "The give transparency software must not be null!")
                   : TransparencySoftware1.CompareTo(TransparencySoftware2) > 0;

        #endregion

        #region Operator >= (TransparencySoftware1, TransparencySoftware2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftware1">A transparency software.</param>
        /// <param name="TransparencySoftware2">Another transparency software.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (TransparencySoftware TransparencySoftware1,
                                           TransparencySoftware TransparencySoftware2)

            => !(TransparencySoftware1 < TransparencySoftware2);

        #endregion

        #endregion

        #region IComparable<TransparencySoftware> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two transparency software.
        /// </summary>
        /// <param name="Object">A transparency software to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is TransparencySoftware transparencySoftware
                   ? CompareTo(transparencySoftware)
                   : throw new ArgumentException("The given object is not a transparency software!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(TransparencySoftware)

        /// <summary>
        /// Compares two transparency software.
        /// </summary>
        /// <param name="TransparencySoftware">A transparency software to compare with.</param>
        public Int32 CompareTo(TransparencySoftware? TransparencySoftware)
        {

            if (TransparencySoftware is null)
                throw new ArgumentNullException(nameof(TransparencySoftware), "The give transparency software must not be null!");

            var c = Id.CompareTo(TransparencySoftware.Id);

            if (c == 0)
                c = Name.Equals(TransparencySoftware.Name) ? 0 :
                    StringComparer.Ordinal.Compare(StrictJSON.NameOrder(Name), StrictJSON.NameOrder(TransparencySoftware.Name));

            if (c == 0)
                c = Version.CompareTo(TransparencySoftware.Version);

            if (c == 0)
                c = LicensesEqual(TransparencySoftware) ? 0 :
                    StringComparer.Ordinal.Compare(String.Join("|", openSourceLicenses.                     Select(TransparencyJSON.LicenseOrder)),
                                                   String.Join("|", TransparencySoftware.openSourceLicenses.Select(TransparencyJSON.LicenseOrder)));

            if (c == 0)
                c = Nullable.Compare(Logo, TransparencySoftware.Logo);

            if (c == 0)
                c = Nullable.Compare(HowToUse, TransparencySoftware.HowToUse);

            if (c == 0)
                c = Nullable.Compare(MoreInformation, TransparencySoftware.MoreInformation);

            if (c == 0)
                c = Nullable.Compare(SourceCodeRepository, TransparencySoftware.SourceCodeRepository);

            if (c == 0)
                c = Nullable.Compare(SupportUntil, TransparencySoftware.SupportUntil);

            if (c == 0)
                c = Nullable.Compare(DeclarationOfConformity, TransparencySoftware.DeclarationOfConformity);

            if (c == 0)
                c = Nullable.Compare(SoftwareBillOfMaterials, TransparencySoftware.SoftwareBillOfMaterials);

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<TransparencySoftware> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two transparency software for equality.
        /// </summary>
        /// <param name="Object">A transparency software to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is TransparencySoftware transparencySoftware &&
                   Equals(transparencySoftware);

        #endregion

        #region Equals(TransparencySoftware)

        /// <summary>
        /// Compares two transparency software for equality.
        /// </summary>
        /// <param name="TransparencySoftware">A transparency software to compare with.</param>
        public Boolean Equals(TransparencySoftware? TransparencySoftware)

            => TransparencySoftware is not null &&

               Id.               Equals(TransparencySoftware.Id)                 &&
               Name.             Equals(TransparencySoftware.Name)               &&
               Version.          Equals(TransparencySoftware.Version)            &&
               LicensesEqual            (TransparencySoftware)                   &&
               Nullable.Equals(SupportUntil,            TransparencySoftware.SupportUntil)            &&
               Nullable.Equals(DeclarationOfConformity, TransparencySoftware.DeclarationOfConformity) &&
               Nullable.Equals(SoftwareBillOfMaterials, TransparencySoftware.SoftwareBillOfMaterials) &&

            ((!Logo.                HasValue && !TransparencySoftware.Logo.                HasValue) ||
              (Logo.                HasValue &&  TransparencySoftware.Logo.                HasValue && Logo.                Value.Equals(TransparencySoftware.Logo.                Value))) &&

            ((!HowToUse.            HasValue && !TransparencySoftware.HowToUse.            HasValue) ||
              (HowToUse.            HasValue &&  TransparencySoftware.HowToUse.            HasValue && HowToUse.            Value.Equals(TransparencySoftware.HowToUse.            Value))) &&

            ((!MoreInformation.     HasValue && !TransparencySoftware.MoreInformation.     HasValue) ||
              (MoreInformation.     HasValue &&  TransparencySoftware.MoreInformation.     HasValue && MoreInformation.     Value.Equals(TransparencySoftware.MoreInformation.     Value))) &&

            ((!SourceCodeRepository.HasValue && !TransparencySoftware.SourceCodeRepository.HasValue) ||
              (SourceCodeRepository.HasValue &&  TransparencySoftware.SourceCodeRepository.HasValue && SourceCodeRepository.Value.Equals(TransparencySoftware.SourceCodeRepository.Value)));

        #endregion

        #region (private) LicensesEqual(TransparencySoftware)

        private Boolean LicensesEqual(TransparencySoftware TransparencySoftware)

            => openSourceLicenses.Length == TransparencySoftware.openSourceLicenses.Length &&
               openSourceLicenses.Zip(TransparencySoftware.openSourceLicenses).All(pair => pair.First.Equals(pair.Second));

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return Id.                   GetHashCode()       * 29 ^
                       Name.                 GetHashCode()       * 23 ^
                       Version.              GetHashCode()       * 19 ^
                       openSourceLicenses.Aggregate(0, (hash, license) => hash ^ TransparencyJSON.LicenseHash(license)) * 13 ^
                      (SupportUntil?.        GetHashCode() ?? 0) * 11 ^
                      (Logo?.                GetHashCode() ?? 0) * 7 ^
                      (HowToUse?.            GetHashCode() ?? 0) * 5 ^
                      (MoreInformation?.     GetHashCode() ?? 0) * 3 ^
                      (SourceCodeRepository?.GetHashCode() ?? 0);

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   Name.FirstText(), ", ",
                   Version,          ", ",
                   String.Join(" / ", openSourceLicenses.Select(license => license.Id))

               );

        #endregion

    }

}
