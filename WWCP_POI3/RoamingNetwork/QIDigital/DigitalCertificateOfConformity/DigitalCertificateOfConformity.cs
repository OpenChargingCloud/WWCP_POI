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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    #region DCoCModule

    /// <summary>
    /// The conformity assessment module of a certificate of conformity (Decision 768/2008/EC, Annex II), as the NoBoMet
    /// D-CoC documents distinguish them: type examination (D-CoC MB), quality assurance (D-CoC QA), product verification
    /// (D-CoC MF). A certificate of type examination or of product verification is for a model, one of quality assurance
    /// approves the quality system of its manufacturer.
    /// </summary>
    public enum DCoCModule
    {

        /// <summary>
        /// EU-type examination: an EU-type examination certificate of the model.
        /// </summary>
        B,

        /// <summary>
        /// Conformity to type based on quality assurance of the production process: the approval of the quality system of
        /// the manufacturer for production, final product inspection and testing.
        /// </summary>
        D,

        /// <summary>
        /// Quality assurance of the production process: the approval of the quality system of the manufacturer.
        /// </summary>
        D1,

        /// <summary>
        /// Conformity to type based on product quality assurance: the approval of the quality system of the manufacturer
        /// for final product inspection and testing.
        /// </summary>
        E,

        /// <summary>
        /// Quality assurance of final product inspection and testing: the approval of the quality system of the manufacturer.
        /// </summary>
        E1,

        /// <summary>
        /// Conformity to type based on product verification: a certificate of the products verified, by their model.
        /// </summary>
        F,

        /// <summary>
        /// Conformity based on product verification: a certificate of the products verified, by their model.
        /// </summary>
        F1,

        /// <summary>
        /// Conformity based on unit verification: a certificate of the unit verified, by its model.
        /// </summary>
        G,

        /// <summary>
        /// Conformity based on full quality assurance: the approval of the quality system of the manufacturer for design,
        /// manufacture, final product inspection and testing.
        /// </summary>
        H,

        /// <summary>
        /// Conformity based on full quality assurance plus design examination: the approval of the quality system of the
        /// manufacturer, as for H.
        /// </summary>
        H1

    }


    /// <summary>
    /// What a conformity assessment module certifies: a model, or the quality system of a manufacturer.
    /// </summary>
    public static class DCoCModuleExtensions
    {

        /// <summary>
        /// Whether a certificate of the given module approves the quality system of a manufacturer (D, D1, E, E1, H, H1)
        /// and names its manufacturer, not a model as one of type examination or product verification (B, F, F1, G).
        /// </summary>
        /// <param name="Module">A conformity assessment module.</param>
        public static Boolean ApprovesQualitySystem(this DCoCModule Module)

            => Module is DCoCModule.D  or DCoCModule.D1 or
                         DCoCModule.E  or DCoCModule.E1 or
                         DCoCModule.H  or DCoCModule.H1;

    }

    #endregion


    /// <summary>
    /// A digital certificate of conformity (D-CoC) of a charging station model or an energy meter model, in the hardware
    /// revisions and firmware versions it covers, or of the quality system of a charging station or an energy meter
    /// manufacturer, in the scope it covers, issued and signed by a conformity assessment body of the roaming network as a
    /// notified body, e.g. the EU-type examination certificate (Module B) of a meter model or the approval of the quality
    /// system of its manufacturer (Module D) under the MID, with one of its signing keys that may sign certificates.
    /// </summary>
    /// <remarks>
    /// Its structure follows the D-CoC of PTB and NoBoMet (NoBoMet Document 1:2023, D-CoC MB and D-CoC QA): its
    /// administrative data, i.e. its identification and revision, its module and legislation, its issuing body with its
    /// number, the certificate holder as the manufacturer of its model, the approved model and its validity, or, of a
    /// module of quality assurance, the manufacturer as the certificate holder with the scope of its quality system
    /// instead of a model; its certification results, i.e. the normative documents applied, the characteristics
    /// confirmed, its conclusion and the conditions of its validity; and its comment. Its date of issue is when it was
    /// signed.
    /// </remarks>
    public sealed class DigitalCertificateOfConformity : AQIDigitalDocument<DigitalCertificateOfConformity>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public static readonly String JSONLDContext = QIDigitalJSON.Context("digitalCertificateOfConformity");

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
            public const String  Id                          = "@id";

            /// <summary>
            /// The property of its revision.
            /// </summary>
            public const String  Revision                    = "revision";

            /// <summary>
            /// The property of its module.
            /// </summary>
            public const String  Module                      = "module";

            /// <summary>
            /// The property of its legislation.
            /// </summary>
            public const String  Legislation                 = "legislation";

            /// <summary>
            /// The property of its conformity assessment body.
            /// </summary>
            public const String  ConformityAssessmentBodyId  = "conformityAssessmentBodyId";

            /// <summary>
            /// The property of the number of its body as a notified body.
            /// </summary>
            public const String  NotifiedBodyNumber          = "notifiedBodyNumber";

            /// <summary>
            /// The property of its model.
            /// </summary>
            public const String  Model                       = "model";

            /// <summary>
            /// The property of its manufacturer, with the scope of its quality system.
            /// </summary>
            public const String  Manufacturer                = "manufacturer";

            /// <summary>
            /// The property of the beginning of its validity.
            /// </summary>
            public const String  ValidFrom                   = "validFrom";

            /// <summary>
            /// The property of the end of its validity.
            /// </summary>
            public const String  ValidUntil                  = "validUntil";

            /// <summary>
            /// The property of its normative documents.
            /// </summary>
            public const String  Standards                   = "standards";

            /// <summary>
            /// The property of its characteristics.
            /// </summary>
            public const String  Characteristics             = "characteristics";

            /// <summary>
            /// The property of its conclusion.
            /// </summary>
            public const String  Conclusion                  = "conclusion";

            /// <summary>
            /// The property of its conditions.
            /// </summary>
            public const String  Conditions                  = "conditions";

            /// <summary>
            /// The property of the URL of its human readable document.
            /// </summary>
            public const String  DocumentURL                 = "documentURL";

            /// <summary>
            /// The property of its comment.
            /// </summary>
            public const String  Comment                     = "comment";

        }

        #endregion


        /// <summary>
        /// Its identification, assigned by its conformity assessment body, e.g. "DE-26-M-PTB-0042".
        /// </summary>
        public DigitalCertificateOfConformity_Id        Id                            { get; }

        /// <summary>
        /// Its revision: 0 for the first one, one more for every revision of it, e.g. for a new firmware version.
        /// </summary>
        public UInt32                                   Revision                      { get; }

        /// <summary>
        /// Its conformity assessment module, e.g. B.
        /// </summary>
        public DCoCModule                               Module                        { get; }

        /// <summary>
        /// The legislation it is issued under, e.g. the MID.
        /// </summary>
        public QIDigitalLegislation                     Legislation                   { get; }

        /// <summary>
        /// Its conformity assessment body, one of the roaming network: its issuer.
        /// </summary>
        public ConformityAssessmentBody_Id              ConformityAssessmentBodyId    { get; }

        /// <summary>
        /// The four digit number of its body as a notified body.
        /// </summary>
        public String                                   NotifiedBodyNumber            { get; }

        /// <summary>
        /// The approved model, its manufacturer the certificate holder, in the hardware revisions and firmware versions it
        /// covers: of a module for a model (B, F, F1, G), null otherwise.
        /// </summary>
        public QIDigitalModel?                          Model                         { get; }

        /// <summary>
        /// The manufacturer whose quality system it approves, the certificate holder, with the scope of its quality system:
        /// of a module of quality assurance (D, D1, E, E1, H, H1), null otherwise. The approval belongs to the
        /// manufacturer, not to one of its models.
        /// </summary>
        public QIDigitalManufacturer?                   Manufacturer                  { get; }

        /// <summary>
        /// Since when it is valid.
        /// </summary>
        public DateTimeOffset                           ValidFrom                     { get; }

        /// <summary>
        /// Until when it is valid, e.g. ten years after it was issued for an EU-type examination certificate under the MID.
        /// </summary>
        public DateTimeOffset?                          ValidUntil                    { get; }

        /// <summary>
        /// The normative documents applied, e.g. EN 50470-3 and PTB-A 50.7.
        /// </summary>
        public ImmutableArray<QIDigitalStandard>        Standards                     { get; }

        /// <summary>
        /// The characteristics of the model it confirms, e.g. its rated currents and voltages.
        /// </summary>
        public ImmutableArray<QIDigitalQuantity>        Characteristics               { get; }

        /// <summary>
        /// Its conclusion, multilingual.
        /// </summary>
        public ImmutableI18NString?                     Conclusion                    { get; }

        /// <summary>
        /// The conditions of its validity, each multilingual.
        /// </summary>
        public ImmutableArray<ImmutableI18NString>      Conditions                    { get; }

        /// <summary>
        /// The URL of its human readable document.
        /// </summary>
        public URL?                                     DocumentURL                   { get; }

        /// <summary>
        /// Its comment.
        /// </summary>
        public ImmutableI18NString?                     Comment                       { get; }


        /// <inheritdoc/>
        public override String Context
            => JSONLDContext;

        /// <inheritdoc/>
        public override String Usage
            => POIKeyUsages.Certificates;

        /// <inheritdoc/>
        public override String IssuerText
            => $"the conformity assessment body '{ConformityAssessmentBodyId}'";

        /// <inheritdoc/>
        public override String DocumentText
            => $"the digital certificate of conformity '{Id}'{(Revision > 0 ? $" revision {Revision}" : "")}";

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new digital certificate of conformity of a model, of a module for a model (B, F, F1, G); Sign(…) signs
        /// it.
        /// </summary>
        /// <param name="Id">Its identification, assigned by its conformity assessment body.</param>
        /// <param name="Module">Its conformity assessment module, one for a model.</param>
        /// <param name="Legislation">The legislation it is issued under.</param>
        /// <param name="ConformityAssessmentBodyId">Its conformity assessment body.</param>
        /// <param name="NotifiedBodyNumber">The four digit number of its body as a notified body.</param>
        /// <param name="Model">The approved model, in the hardware revisions and firmware versions it covers.</param>
        /// <param name="ValidFrom">Since when it is valid.</param>
        /// <param name="ValidUntil">Until when it is valid.</param>
        /// <param name="Revision">Its revision.</param>
        /// <param name="Standards">The normative documents applied.</param>
        /// <param name="Characteristics">The characteristics of the model it confirms.</param>
        /// <param name="Conclusion">Its conclusion.</param>
        /// <param name="Conditions">The conditions of its validity.</param>
        /// <param name="DocumentURL">The URL of its human readable document.</param>
        /// <param name="Comment">Its comment.</param>
        /// <param name="Signature">Its signature, if it is signed.</param>
        public DigitalCertificateOfConformity(DigitalCertificateOfConformity_Id     Id,
                                              DCoCModule                            Module,
                                              QIDigitalLegislation                  Legislation,
                                              ConformityAssessmentBody_Id           ConformityAssessmentBodyId,
                                              String                                NotifiedBodyNumber,
                                              QIDigitalModel                        Model,
                                              DateTimeOffset                        ValidFrom,
                                              DateTimeOffset?                       ValidUntil        = null,
                                              UInt32                                Revision          = 0,
                                              IEnumerable<QIDigitalStandard>?       Standards         = null,
                                              IEnumerable<QIDigitalQuantity>?       Characteristics   = null,
                                              ImmutableI18NString?                  Conclusion        = null,
                                              IEnumerable<ImmutableI18NString>?     Conditions        = null,
                                              URL?                                  DocumentURL       = null,
                                              ImmutableI18NString?                  Comment           = null,
                                              QIDigitalSignature?                   Signature         = null)

            : this(Id,
                   Module,
                   Legislation,
                   ConformityAssessmentBodyId,
                   NotifiedBodyNumber,
                   Model ?? throw new ArgumentNullException(nameof(Model)),
                   null,
                   ValidFrom,
                   ValidUntil,
                   Revision,
                   Standards,
                   Characteristics,
                   Conclusion,
                   Conditions,
                   DocumentURL,
                   Comment,
                   Signature)

        { }


        /// <summary>
        /// Create a new digital certificate of conformity of the quality system of a manufacturer, of a module of quality
        /// assurance (D, D1, E, E1, H, H1); Sign(…) signs it.
        /// </summary>
        /// <param name="Id">Its identification, assigned by its conformity assessment body.</param>
        /// <param name="Module">Its conformity assessment module, one of quality assurance.</param>
        /// <param name="Legislation">The legislation it is issued under.</param>
        /// <param name="ConformityAssessmentBodyId">Its conformity assessment body.</param>
        /// <param name="NotifiedBodyNumber">The four digit number of its body as a notified body.</param>
        /// <param name="Manufacturer">The manufacturer whose quality system it approves, with the scope of its quality system.</param>
        /// <param name="ValidFrom">Since when it is valid.</param>
        /// <param name="ValidUntil">Until when it is valid.</param>
        /// <param name="Revision">Its revision.</param>
        /// <param name="Standards">The normative documents applied.</param>
        /// <param name="Characteristics">The characteristics it confirms.</param>
        /// <param name="Conclusion">Its conclusion.</param>
        /// <param name="Conditions">The conditions of its validity.</param>
        /// <param name="DocumentURL">The URL of its human readable document.</param>
        /// <param name="Comment">Its comment.</param>
        /// <param name="Signature">Its signature, if it is signed.</param>
        public DigitalCertificateOfConformity(DigitalCertificateOfConformity_Id     Id,
                                              DCoCModule                            Module,
                                              QIDigitalLegislation                  Legislation,
                                              ConformityAssessmentBody_Id           ConformityAssessmentBodyId,
                                              String                                NotifiedBodyNumber,
                                              QIDigitalManufacturer                 Manufacturer,
                                              DateTimeOffset                        ValidFrom,
                                              DateTimeOffset?                       ValidUntil        = null,
                                              UInt32                                Revision          = 0,
                                              IEnumerable<QIDigitalStandard>?       Standards         = null,
                                              IEnumerable<QIDigitalQuantity>?       Characteristics   = null,
                                              ImmutableI18NString?                  Conclusion        = null,
                                              IEnumerable<ImmutableI18NString>?     Conditions        = null,
                                              URL?                                  DocumentURL       = null,
                                              ImmutableI18NString?                  Comment           = null,
                                              QIDigitalSignature?                   Signature         = null)

            : this(Id,
                   Module,
                   Legislation,
                   ConformityAssessmentBodyId,
                   NotifiedBodyNumber,
                   null,
                   Manufacturer ?? throw new ArgumentNullException(nameof(Manufacturer)),
                   ValidFrom,
                   ValidUntil,
                   Revision,
                   Standards,
                   Characteristics,
                   Conclusion,
                   Conditions,
                   DocumentURL,
                   Comment,
                   Signature)

        { }


        private DigitalCertificateOfConformity(DigitalCertificateOfConformity_Id     Id,
                                               DCoCModule                            Module,
                                               QIDigitalLegislation                  Legislation,
                                               ConformityAssessmentBody_Id           ConformityAssessmentBodyId,
                                               String                                NotifiedBodyNumber,
                                               QIDigitalModel?                       Model,
                                               QIDigitalManufacturer?                Manufacturer,
                                               DateTimeOffset                        ValidFrom,
                                               DateTimeOffset?                       ValidUntil,
                                               UInt32                                Revision,
                                               IEnumerable<QIDigitalStandard>?       Standards,
                                               IEnumerable<QIDigitalQuantity>?       Characteristics,
                                               ImmutableI18NString?                  Conclusion,
                                               IEnumerable<ImmutableI18NString>?     Conditions,
                                               URL?                                  DocumentURL,
                                               ImmutableI18NString?                  Comment,
                                               QIDigitalSignature?                   Signature)

            : base(Signature)

        {

            ArgumentNullException.ThrowIfNull(Legislation);

            if (Id.IsNullOrEmpty)
                throw new ArgumentException("A digital certificate of conformity requires an identification!", nameof(Id));

            if (HolderMismatch(Id, Module, Model, Manufacturer) is String holderMismatch)
                throw new ArgumentException(holderMismatch, Model is null ? nameof(Manufacturer) : nameof(Model));

            if (NotifiedBodyNumber is null || NotifiedBodyNumber.Length != 4 || !NotifiedBodyNumber.All(Char.IsAsciiDigit))
                throw new ArgumentException($"The number of the notified body of the digital certificate of conformity '{Id}' must be four digits!", nameof(NotifiedBodyNumber));

            if (ValidUntil.HasValue && ValidUntil.Value <= ValidFrom)
                throw new ArgumentException($"The digital certificate of conformity '{Id}' must be valid until after it is valid from!", nameof(ValidUntil));

            var conditions = (Conditions ?? []).ToImmutableArray();

            if (conditions.Any(condition => condition.IsNullOrEmpty()))
                throw new ArgumentException($"A condition of the digital certificate of conformity '{Id}' must not be empty!", nameof(Conditions));

            this.Id                          = Id;
            this.Module                      = Module;
            this.Legislation                 = Legislation;
            this.ConformityAssessmentBodyId  = ConformityAssessmentBodyId;
            this.NotifiedBodyNumber          = NotifiedBodyNumber;
            this.Model                       = Model;
            this.Manufacturer                = Manufacturer;
            this.ValidFrom                   = QIDigitalJSON.Timestamp(ValidFrom);
            this.ValidUntil                  = QIDigitalJSON.Timestamp(ValidUntil);
            this.Revision                    = Revision;
            this.Standards                   = QIDigitalJSON.Distinct(Standards, standard => standard.Reference, "standard of a certificate of conformity");
            this.Characteristics             = (Characteristics ?? []).ToImmutableArray();
            this.Conclusion                  = Conclusion;
            this.Conditions                  = conditions;
            this.DocumentURL                 = DocumentURL;
            this.Comment                     = Comment;

        }

        #endregion


        #region IsValidAt(Timestamp)

        /// <summary>
        /// Whether it is valid at the given time: not before its validFrom, and not after its validUntil, when it has one.
        /// </summary>
        public Boolean IsValidAt(DateTimeOffset Timestamp)

            => Timestamp >= ValidFrom &&
               (!ValidUntil.HasValue || Timestamp <= ValidUntil.Value);

        #endregion

        #region TryVerify(RoamingNetwork, out ErrorResponse, At = null)

        /// <summary>
        /// Verify this certificate against the given roaming network: signed by a signing key of its conformity assessment
        /// body, one of the network with its number as a notified body and notified for its legislation, that may sign
        /// certificates; its model one of its manufacturer, in hardware revisions and firmware versions of it, or its
        /// manufacturer one of the network of the kind it names; and valid at the given time, if one is given.
        /// </summary>
        /// <param name="RoamingNetwork">The roaming network.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="At">When it and its key must be valid as well, if anything.</param>
        public Boolean TryVerify(RoamingNetwork                    RoamingNetwork,
                                 [NotNullWhen(false)] out String?  ErrorResponse,
                                 DateTimeOffset?                   At   = null)
        {

            if (!RoamingNetwork.TryGetConformityAssessmentBodyById(ConformityAssessmentBodyId, out var body))
            {
                ErrorResponse = $"The conformity assessment body '{ConformityAssessmentBodyId}' of {DocumentText} is none of the roaming network '{RoamingNetwork.Id}'!";
                return false;
            }

            if (body.NotifiedBodyNumber != NotifiedBodyNumber)
            {
                ErrorResponse = $"The conformity assessment body '{body.Id}' is the notified body '{body.NotifiedBodyNumber ?? "none"}', not '{NotifiedBodyNumber}' as {DocumentText} says!";
                return false;
            }

            if (!body.NotifiedFor.Contains(Legislation.Name))
            {
                ErrorResponse = $"The conformity assessment body '{body.Id}' is not notified for the {Legislation.Name}, {DocumentText} is issued under!";
                return false;
            }

            if (At is DateTimeOffset at && !IsValidAt(at))
            {
                ErrorResponse = $"{Capitalized(DocumentText)} is not valid at {at.ToISO8601()}!";
                return false;
            }

            return (Model is not null
                        ? Model.        TryVerify(RoamingNetwork, out ErrorResponse)
                        : Manufacturer!.TryVerify(RoamingNetwork, out ErrorResponse)) &&
                   TryVerify(body.SigningKeys, out ErrorResponse, At);

        }

        #endregion

        #region TryCover(Item, out ErrorResponse)

        /// <summary>
        /// Whether it covers the given charging station or energy meter, e.g. the item of a digital calibration certificate:
        /// one of its model, in one of its hardware revisions and one of its firmware versions; or, of a module of quality
        /// assurance, one of its manufacturer, of the kind it is a manufacturer of and of a kind in the scope of its
        /// quality system, as far as it can be told, e.g. an energy meter for MI-003.
        /// </summary>
        public Boolean TryCover(QIDigitalItem                     Item,
                                [NotNullWhen(false)] out String?  ErrorResponse)

            => Model is not null
                   ? Model.        TryCover(Item, out ErrorResponse)
                   : Manufacturer!.TryCover(Item, out ErrorResponse);

        #endregion


        #region (static) TryParse(Text, out Certificate, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON text of a digital certificate of conformity, numbers with a fraction as decimals.
        /// </summary>
        public static Boolean TryParse(String                                                    Text,
                                       [NotNullWhen(true)]  out DigitalCertificateOfConformity?  Certificate,
                                       [NotNullWhen(false)] out String?                          ErrorResponse)
        {

            Certificate = null;

            try
            {

                if (POIJSON.Read(Text) is not JObject json)
                {
                    ErrorResponse = "The given JSON text must be a JSON object!";
                    return false;
                }

                return TryParse(json, out Certificate, out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(JSON, out Certificate, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a digital certificate of conformity. Its signature is not verified here.
        /// </summary>
        public static Boolean TryParse(JObject                                                   JSON,
                                       [NotNullWhen(true)]  out DigitalCertificateOfConformity?  Certificate,
                                       [NotNullWhen(false)] out String?                          ErrorResponse)
        {

            Certificate = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "a digital certificate of conformity", out ErrorResponse,
                                           QIDigitalSignature.PropertyKeys.Context, PropertyKeys.Id, PropertyKeys.Revision, PropertyKeys.Module,
                                           PropertyKeys.Legislation, PropertyKeys.ConformityAssessmentBodyId, PropertyKeys.NotifiedBodyNumber,
                                           PropertyKeys.Model, PropertyKeys.Manufacturer, PropertyKeys.ValidFrom, PropertyKeys.ValidUntil, PropertyKeys.Standards,
                                           PropertyKeys.Characteristics, PropertyKeys.Conclusion, PropertyKeys.Conditions,
                                           PropertyKeys.DocumentURL, PropertyKeys.Comment, QIDigitalSignature.PropertyKeys.Signature) ||
                    !QIDigitalJSON.TryContext(JSON, JSONLDContext, "a digital certificate of conformity", out ErrorResponse))
                {
                    return false;
                }

                if (!QIDigitalJSON.TryParseRequiredText(JSON, PropertyKeys.Id,                         "certificate of conformity identification",  DigitalCertificateOfConformity_Id.TryParse, out DigitalCertificateOfConformity_Id id,     out ErrorResponse) ||
                    !QIDigitalJSON.TryParseRequiredText(JSON, PropertyKeys.ConformityAssessmentBodyId, "conformity assessment body identification",  ConformityAssessmentBody_Id.      TryParse, out ConformityAssessmentBody_Id       bodyId, out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseEnum<DCoCModule>                (JSON, PropertyKeys.Module,             "module",                                                                    out var module,           out ErrorResponse) ||
                    !POIJSON.TryParseObject<QIDigitalLegislation>    (JSON, PropertyKeys.Legislation,        "legislation",           QIDigitalLegislation. TryParse,                     out var legislation,      out ErrorResponse) ||
                    !POIJSON.TryParseObject<QIDigitalModel>          (JSON, PropertyKeys.Model,              "model",                 QIDigitalModel.       TryParse,                     out var model,            out ErrorResponse) ||
                    !POIJSON.TryParseObject<QIDigitalManufacturer>   (JSON, PropertyKeys.Manufacturer,       "manufacturer",          QIDigitalManufacturer.TryParse,                     out var manufacturer,     out ErrorResponse) ||
                    !POIJSON.TryParseObjects<QIDigitalStandard>      (JSON, PropertyKeys.Standards,          "standard",              QIDigitalStandard.    TryParse,                     out var standards,        out ErrorResponse) ||
                    !POIJSON.TryParseObjects<QIDigitalQuantity>      (JSON, PropertyKeys.Characteristics,    "characteristic",        QIDigitalQuantity.    TryParse,                     out var characteristics,  out ErrorResponse) ||
                    !POIJSON.TryParseString                          (JSON, PropertyKeys.NotifiedBodyNumber, "notified body number",                                                      out var number,           out ErrorResponse) ||
                    !POIJSON.TryParseI18N                            (JSON, PropertyKeys.Conclusion,         "conclusion",                                                                out var conclusion,       out ErrorResponse) ||
                    !POIJSON.TryParseI18N                            (JSON, PropertyKeys.Comment,            "comment",                                                                   out var comment,          out ErrorResponse) ||
                    !POIJSON.TryParseText                            (JSON, PropertyKeys.DocumentURL,        "document URL",          URL.TryParse,                                       out URL? documentURL,     out ErrorResponse) ||
                    !QIDigitalJSON.TryParseTimestamp                 (JSON, PropertyKeys.ValidFrom,          "beginning of its validity",                                                 out var validFrom,        out ErrorResponse) ||
                    !QIDigitalJSON.TryParseTimestamp                 (JSON, PropertyKeys.ValidUntil,         "end of its validity",                                                       out var validUntil,       out ErrorResponse) ||
                    !TryParseSignature                               (JSON,                                                                                                               out var signature,        out ErrorResponse))
                {
                    return false;
                }

                if (!module.HasValue || legislation is null || number is null || !validFrom.HasValue)
                {
                    ErrorResponse = $"The digital certificate of conformity '{id}' requires its module, its legislation, the number of its notified body and the beginning of its validity!";
                    return false;
                }

                if (HolderMismatch(id, module.Value, model, manufacturer) is String holderMismatch)
                {
                    ErrorResponse = holderMismatch;
                    return false;
                }

                var revision = 0U;

                if (JSON[PropertyKeys.Revision] is JToken revisionJSON)
                {

                    if (revisionJSON.Type != JTokenType.Integer || revisionJSON.Value<Int64>() < 0 || revisionJSON.Value<Int64>() > UInt32.MaxValue)
                    {
                        ErrorResponse = "The revision of a digital certificate of conformity must be an integer from 0!";
                        return false;
                    }

                    revision = revisionJSON.Value<UInt32>();

                }

                var conditions = new List<ImmutableI18NString>();

                if (JSON[PropertyKeys.Conditions] is JToken conditionsJSON)
                {

                    if (conditionsJSON is not JArray conditionsArray)
                    {
                        ErrorResponse = "The conditions of a digital certificate of conformity must be a JSON array!";
                        return false;
                    }

                    foreach (var conditionJSON in conditionsArray)
                    {

                        if (conditionJSON is not JObject conditionObject || !ImmutableI18NString.TryParse(conditionObject, out var condition, out var conditionError))
                        {
                            ErrorResponse = $"Invalid condition of a digital certificate of conformity '{conditionJSON}'!";
                            return false;
                        }

                        conditions.Add(condition);

                    }

                }

                Certificate    = new DigitalCertificateOfConformity(id,
                                                                    module.Value,
                                                                    legislation,
                                                                    bodyId,
                                                                    number,
                                                                    model,
                                                                    manufacturer,
                                                                    validFrom.Value,
                                                                    validUntil,
                                                                    revision,
                                                                    standards,
                                                                    characteristics,
                                                                    conclusion,
                                                                    conditions,
                                                                    documentURL,
                                                                    comment,
                                                                    signature);

                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                Certificate    = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion


        #region (protected) Content()

        /// <inheritdoc/>
        protected override JObject Content()
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Id,                          Id.ToString()),
                           new JProperty(PropertyKeys.Revision,                    Revision),
                           new JProperty(PropertyKeys.Module,                      Module.ToString()),
                           new JProperty(PropertyKeys.Legislation,                 Legislation.ToJSON()),
                           new JProperty(PropertyKeys.ConformityAssessmentBodyId,  ConformityAssessmentBodyId.ToString()),
                           new JProperty(PropertyKeys.NotifiedBodyNumber,          NotifiedBodyNumber),
                           Model is not null
                               ? new JProperty(PropertyKeys.Model,                 Model.        ToJSON())
                               : new JProperty(PropertyKeys.Manufacturer,          Manufacturer!.ToJSON()),
                           new JProperty(PropertyKeys.ValidFrom,                   ValidFrom.ToISO8601())
                       );

            if (ValidUntil.HasValue)
                json.Add(PropertyKeys.ValidUntil,       ValidUntil.Value.ToISO8601());

            if (Standards.Length > 0)
                json.Add(PropertyKeys.Standards,        new JArray(Standards.      Select(standard       => standard.      ToJSON())));

            if (Characteristics.Length > 0)
                json.Add(PropertyKeys.Characteristics,  new JArray(Characteristics.Select(characteristic => characteristic.ToJSON())));

            if (Conclusion.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Conclusion,       Conclusion.ToJSON());

            if (Conditions.Length > 0)
                json.Add(PropertyKeys.Conditions,       new JArray(Conditions.     Select(condition      => condition.     ToJSON())));

            if (DocumentURL.HasValue)
                json.Add(PropertyKeys.DocumentURL,      DocumentURL.Value.ToString());

            if (Comment.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Comment,          Comment.ToJSON());

            return json;

        }

        #endregion

        #region (protected) WithSignature(Signature)

        /// <inheritdoc/>
        protected override DigitalCertificateOfConformity WithSignature(QIDigitalSignature Signature)

            => new (Id,
                    Module,
                    Legislation,
                    ConformityAssessmentBodyId,
                    NotifiedBodyNumber,
                    Model,
                    Manufacturer,
                    ValidFrom,
                    ValidUntil,
                    Revision,
                    Standards,
                    Characteristics,
                    Conclusion,
                    Conditions,
                    DocumentURL,
                    Comment,
                    Signature);

        #endregion


        #region (private static) HolderMismatch(Id, Module, Model, Manufacturer)

        /// <summary>
        /// Why the given model and manufacturer do not fit the given module, or null when they do: exactly one of them, a
        /// model for a module for a model, a manufacturer for one of quality assurance.
        /// </summary>
        private static String? HolderMismatch(DigitalCertificateOfConformity_Id  Id,
                                              DCoCModule                         Module,
                                              QIDigitalModel?                    Model,
                                              QIDigitalManufacturer?             Manufacturer)

            => Module.ApprovesQualitySystem()

                   ? Model is not null
                         ? $"The digital certificate of conformity '{Id}' of the module {Module} approves the quality system of a manufacturer and names it, not a model!"
                         : Manufacturer is null
                               ? $"The digital certificate of conformity '{Id}' of the module {Module} requires the manufacturer whose quality system it approves!"
                               : null

                   : Manufacturer is not null
                         ? $"The digital certificate of conformity '{Id}' of the module {Module} is for a model and names it, not a manufacturer!"
                         : Model is null
                               ? $"The digital certificate of conformity '{Id}' of the module {Module} requires its model!"
                               : null;

        #endregion

    }

}
