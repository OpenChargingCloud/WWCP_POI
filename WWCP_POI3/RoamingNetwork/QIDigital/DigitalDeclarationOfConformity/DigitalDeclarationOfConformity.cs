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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    #region DDoCNotifiedBody

    /// <summary>
    /// A notified body a declaration of conformity names (Decision 768/2008/EC, Annex III, point 7): the conformity
    /// assessment body, its number, the module of its intervention and the certificate it issued, e.g. the EU-type
    /// examination certificate of the model, or the approval of the quality system of its manufacturer.
    /// </summary>
    /// <param name="ConformityAssessmentBodyId">The conformity assessment body, one of the roaming network.</param>
    /// <param name="NotifiedBodyNumber">Its four digit number as a notified body.</param>
    /// <param name="Module">The module of its intervention, e.g. B.</param>
    /// <param name="CertificateId">The certificate of conformity it issued.</param>
    /// <param name="Intervention">What it did, multilingual.</param>
    public sealed record DDoCNotifiedBody(ConformityAssessmentBody_Id           ConformityAssessmentBodyId,
                                          String                                NotifiedBodyNumber,
                                          DCoCModule?                           Module          = null,
                                          DigitalCertificateOfConformity_Id?    CertificateId   = null,
                                          ImmutableI18NString?                  Intervention    = null)
    {

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of the conformity assessment body.
            /// </summary>
            public const String  ConformityAssessmentBodyId  = "conformityAssessmentBodyId";

            /// <summary>
            /// The property of its number as a notified body.
            /// </summary>
            public const String  NotifiedBodyNumber          = "notifiedBodyNumber";

            /// <summary>
            /// The property of the module of its intervention.
            /// </summary>
            public const String  Module                      = "module";

            /// <summary>
            /// The property of the certificate it issued.
            /// </summary>
            public const String  CertificateId               = "certificateId";

            /// <summary>
            /// The property of what it did.
            /// </summary>
            public const String  Intervention                = "intervention";

        }

        #endregion

        /// <summary>
        /// Its four digit number as a notified body, e.g. 0102.
        /// </summary>
        public String NotifiedBodyNumber { get; }

            = NotifiedBodyNumber is not null && NotifiedBodyNumber.Length == 4 && NotifiedBodyNumber.All(Char.IsAsciiDigit)
                  ? NotifiedBodyNumber
                  : throw new ArgumentException("The number of a notified body must be four digits!", nameof(NotifiedBodyNumber));


        #region (static) TryParse(JSON, out NotifiedBody, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a notified body of a declaration of conformity.
        /// </summary>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out DDoCNotifiedBody?  NotifiedBody,
                                       [NotNullWhen(false)] out String?            ErrorResponse)
        {

            NotifiedBody = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "a notified body", out ErrorResponse,
                                           PropertyKeys.ConformityAssessmentBodyId, PropertyKeys.NotifiedBodyNumber, PropertyKeys.Module,
                                           PropertyKeys.CertificateId, PropertyKeys.Intervention) ||
                    !QIDigitalJSON.TryParseRequiredText(JSON, PropertyKeys.ConformityAssessmentBodyId, "conformity assessment body identification", ConformityAssessmentBody_Id.TryParse, out ConformityAssessmentBody_Id bodyId, out ErrorResponse) ||
                    !POIJSON.TryParseEnum<DCoCModule>  (JSON, PropertyKeys.Module,                     "module",                                                                          out var module,                      out ErrorResponse) ||
                    !POIJSON.TryParseText              (JSON, PropertyKeys.CertificateId,              "certificate of conformity identification",  DigitalCertificateOfConformity_Id.TryParse, out DigitalCertificateOfConformity_Id? certificateId, out ErrorResponse) ||
                    !QIDigitalJSON.TryParseI18N        (JSON, PropertyKeys.Intervention,               "intervention",                                                                    out var intervention,                out ErrorResponse))
                {
                    return false;
                }

                if (QIDigitalJSON.Text(JSON, PropertyKeys.NotifiedBodyNumber) is not String number)
                {
                    ErrorResponse = "A notified body requires its number!";
                    return false;
                }

                NotifiedBody   = new DDoCNotifiedBody(bodyId, number, module, certificateId, intervention);
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                NotifiedBody   = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this notified body.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.ConformityAssessmentBodyId,  ConformityAssessmentBodyId.ToString()),
                           new JProperty(PropertyKeys.NotifiedBodyNumber,          NotifiedBodyNumber)
                       );

            if (Module.HasValue)
                json.Add(PropertyKeys.Module,         Module.Value.ToString());

            if (CertificateId.HasValue)
                json.Add(PropertyKeys.CertificateId,  CertificateId.Value.ToString());

            if (Intervention.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Intervention,   Intervention.ToJSON());

            return json;

        }

        #endregion

        /// <inheritdoc/>
        public override String ToString()
            => $"{NotifiedBodyNumber} ({ConformityAssessmentBodyId}){(Module.HasValue ? $", module {Module}" : "")}{(CertificateId.HasValue ? $", certificate {CertificateId}" : "")}";

    }

    #endregion


    /// <summary>
    /// A digital EU declaration of conformity (DDoC) of a charging station model or an energy meter model, in the hardware
    /// revisions and firmware versions it covers, issued under the sole responsibility of its manufacturer and signed by
    /// it with one of its signing keys that may sign its models.
    /// </summary>
    /// <remarks>
    /// Its structure follows the model declaration of Decision 768/2008/EC, Annex III, as e.g. the MID (Annex XIII) and
    /// the CRA (Annex V) ask for it: its number (1), its manufacturer (2) as the manufacturer of its model, the object of
    /// the declaration (4) as its model, the Union harmonisation legislations (5), the standards and other technical
    /// specifications applied (6), the notified bodies, their intervention and their certificates (7), additional
    /// information (8), and the place, the date, the name and the function of who signs it for its manufacturer. The
    /// sentence of point 3 is implied by its context. Its date of issue is when it was signed.
    /// </remarks>
    public sealed class DigitalDeclarationOfConformity : AQIDigitalDocument<DigitalDeclarationOfConformity>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public static readonly String JSONLDContext = QIDigitalJSON.Context("digitalDeclarationOfConformity");

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
            /// The property of its model.
            /// </summary>
            public const String  Model                  = "model";

            /// <summary>
            /// The property of its legislations.
            /// </summary>
            public const String  Legislations           = "legislations";

            /// <summary>
            /// The property of its standards.
            /// </summary>
            public const String  Standards              = "standards";

            /// <summary>
            /// The property of its notified bodies.
            /// </summary>
            public const String  NotifiedBodies         = "notifiedBodies";

            /// <summary>
            /// The property of its additional information.
            /// </summary>
            public const String  AdditionalInformation  = "additionalInformation";

            /// <summary>
            /// The property of its place of issue.
            /// </summary>
            public const String  PlaceOfIssue           = "placeOfIssue";

            /// <summary>
            /// The property of who signs it for its manufacturer.
            /// </summary>
            public const String  Signatory              = "signatory";

        }

        #endregion


        /// <summary>
        /// Its number, assigned by its manufacturer (point 1).
        /// </summary>
        public DigitalDeclarationOfConformity_Id         Id                       { get; }

        /// <summary>
        /// The object of the declaration (point 4): a model of its manufacturer, its issuer (point 2), in the hardware
        /// revisions and firmware versions it covers.
        /// </summary>
        public QIDigitalModel                            Model                    { get; }

        /// <summary>
        /// The Union harmonisation legislations its model conforms to, at least one, each once (point 5).
        /// </summary>
        public ImmutableArray<QIDigitalLegislation>      Legislations             { get; }

        /// <summary>
        /// The harmonised standards and other technical specifications applied (point 6).
        /// </summary>
        public ImmutableArray<QIDigitalStandard>         Standards                { get; }

        /// <summary>
        /// The notified bodies, what they did and the certificates they issued (point 7).
        /// </summary>
        public ImmutableArray<DDoCNotifiedBody>          NotifiedBodies           { get; }

        /// <summary>
        /// Additional information (point 8).
        /// </summary>
        public ImmutableI18NString?                      AdditionalInformation    { get; }

        /// <summary>
        /// Its place of issue.
        /// </summary>
        public String                                    PlaceOfIssue             { get; }

        /// <summary>
        /// Who signs it for and on behalf of its manufacturer, with the function as its role.
        /// </summary>
        public QIDigitalPerson                           Signatory                { get; }


        /// <inheritdoc/>
        public override String Context
            => JSONLDContext;

        /// <inheritdoc/>
        public override String Usage
            => POIKeyUsages.Models;

        /// <inheritdoc/>
        public override String IssuerText
            => $"the {(Model.Type == QIDigitalModel.ChargingStationModelType ? "charging station" : "energy meter")} manufacturer '{Model.ManufacturerId}'";

        /// <inheritdoc/>
        public override String DocumentText
            => $"the digital declaration of conformity '{Id}'";

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new digital declaration of conformity; Sign(…) signs it.
        /// </summary>
        /// <param name="Id">Its number, assigned by its manufacturer.</param>
        /// <param name="Model">The object of the declaration, a model of its manufacturer.</param>
        /// <param name="Legislations">The Union harmonisation legislations its model conforms to.</param>
        /// <param name="PlaceOfIssue">Its place of issue.</param>
        /// <param name="Signatory">Who signs it for its manufacturer.</param>
        /// <param name="Standards">The standards and other technical specifications applied.</param>
        /// <param name="NotifiedBodies">The notified bodies, what they did and the certificates they issued.</param>
        /// <param name="AdditionalInformation">Additional information.</param>
        /// <param name="Signature">Its signature, if it is signed.</param>
        public DigitalDeclarationOfConformity(DigitalDeclarationOfConformity_Id    Id,
                                              QIDigitalModel                       Model,
                                              IEnumerable<QIDigitalLegislation>    Legislations,
                                              String                               PlaceOfIssue,
                                              QIDigitalPerson                      Signatory,
                                              IEnumerable<QIDigitalStandard>?      Standards               = null,
                                              IEnumerable<DDoCNotifiedBody>?       NotifiedBodies          = null,
                                              ImmutableI18NString?                 AdditionalInformation   = null,
                                              QIDigitalSignature?                  Signature               = null)

            : base(Signature)

        {

            ArgumentNullException.ThrowIfNull(Model);
            ArgumentNullException.ThrowIfNull(Signatory);

            if (Id.IsNullOrEmpty)
                throw new ArgumentException("A digital declaration of conformity requires its number!", nameof(Id));

            if (String.IsNullOrWhiteSpace(PlaceOfIssue))
                throw new ArgumentException($"The digital declaration of conformity '{Id}' requires its place of issue!", nameof(PlaceOfIssue));

            var legislations = QIDigitalJSON.Distinct(Legislations, legislation => legislation.Name, "legislation of a declaration of conformity");

            if (legislations.IsEmpty)
                throw new ArgumentException($"The digital declaration of conformity '{Id}' names at least one Union harmonisation legislation!", nameof(Legislations));

            this.Id                     = Id;
            this.Model                  = Model;
            this.Legislations           = legislations;
            this.PlaceOfIssue           = PlaceOfIssue;
            this.Signatory              = Signatory;
            this.Standards              = QIDigitalJSON.Distinct(Standards,      standard     => standard.Reference,                                                "standard of a declaration of conformity");
            this.NotifiedBodies         = QIDigitalJSON.Distinct(NotifiedBodies, notifiedBody => $"{notifiedBody.NotifiedBodyNumber}/{notifiedBody.Module}/{notifiedBody.CertificateId}", "notified body of a declaration of conformity");
            this.AdditionalInformation  = AdditionalInformation;

        }

        #endregion


        #region TryVerify(RoamingNetwork, out ErrorResponse, At = null)

        /// <summary>
        /// Verify this declaration against the given roaming network: signed by a signing key of the manufacturer of its
        /// model, a manufacturer of the network, that may sign its models; its model one of its manufacturer, in hardware
        /// revisions and firmware versions of it; and each notified body it names a conformity assessment body of the
        /// network with that number.
        /// </summary>
        /// <param name="RoamingNetwork">The roaming network.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="At">When the key must be valid as well, if anything.</param>
        public Boolean TryVerify(RoamingNetwork                    RoamingNetwork,
                                 [NotNullWhen(false)] out String?  ErrorResponse,
                                 DateTimeOffset?                   At   = null)
        {

            if (!Model.TryVerify(RoamingNetwork, out ErrorResponse))
                return false;

            var signingKeys = Model.ChargingStationManufacturerId is ChargingStationManufacturer_Id chargingStationManufacturerId
                                  ? RoamingNetwork.ChargingStationManufacturers.Index[chargingStationManufacturerId].SigningKeys
                                  : RoamingNetwork.EnergyMeterManufacturers.    Index[Model.EnergyMeterManufacturerId!.Value].SigningKeys;

            if (!TryVerify(signingKeys, out ErrorResponse, At))
                return false;

            foreach (var notifiedBody in NotifiedBodies)
            {

                if (!RoamingNetwork.TryGetConformityAssessmentBodyById(notifiedBody.ConformityAssessmentBodyId, out var body))
                {
                    ErrorResponse = $"The notified body '{notifiedBody.ConformityAssessmentBodyId}' of {DocumentText} is no conformity assessment body of the roaming network '{RoamingNetwork.Id}'!";
                    return false;
                }

                if (body.NotifiedBodyNumber != notifiedBody.NotifiedBodyNumber)
                {
                    ErrorResponse = $"The conformity assessment body '{body.Id}' is the notified body '{body.NotifiedBodyNumber ?? "none"}', not '{notifiedBody.NotifiedBodyNumber}' as {DocumentText} says!";
                    return false;
                }

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region TryVerify(Certificate, out ErrorResponse)

        /// <summary>
        /// Whether the given certificate of conformity is one this declaration names (point 7): issued by the notified body
        /// it names with it, of the module it names, under one of its legislations; for its model and in all its hardware
        /// revisions and firmware versions, or, of a module of quality assurance, approving the quality system of the
        /// manufacturer of its model, of its kind and with its kind in the scope, as far as it can be told. Whether the
        /// certificate itself is valid, its own TryVerify(…) tells.
        /// </summary>
        /// <param name="Certificate">A certificate of conformity.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean TryVerify(DigitalCertificateOfConformity    Certificate,
                                 [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (NotifiedBodies.FirstOrDefault(notifiedBody => notifiedBody.CertificateId == Certificate.Id) is not DDoCNotifiedBody notifiedBody)
                ErrorResponse = $"{Capitalized(DocumentText)} names no certificate '{Certificate.Id}'!";

            else if (notifiedBody.ConformityAssessmentBodyId != Certificate.ConformityAssessmentBodyId ||
                     notifiedBody.NotifiedBodyNumber         != Certificate.NotifiedBodyNumber)
                ErrorResponse = $"The certificate '{Certificate.Id}' is one of '{Certificate.ConformityAssessmentBodyId}', not of the notified body {notifiedBody.NotifiedBodyNumber} '{notifiedBody.ConformityAssessmentBodyId}' {DocumentText} names!";

            else if (notifiedBody.Module.HasValue && notifiedBody.Module != Certificate.Module)
                ErrorResponse = $"The certificate '{Certificate.Id}' is one of the module {Certificate.Module}, not {notifiedBody.Module}!";

            else if (!Legislations.Any(legislation => legislation.Name == Certificate.Legislation.Name))
                ErrorResponse = $"The certificate '{Certificate.Id}' is issued under the {Certificate.Legislation.Name}, none of the legislations of {DocumentText}!";

            // The approval of a quality system belongs to the manufacturer: it names no model, revisions or versions.
            else if (Certificate.Manufacturer is QIDigitalManufacturer manufacturer)
            {

                if (!manufacturer.Makes(Model))
                    ErrorResponse = $"The certificate '{Certificate.Id}' approves the quality system of {manufacturer.Text}, not of the {(Model.Type == QIDigitalModel.ChargingStationModelType ? "charging station" : "energy meter")} manufacturer '{Model.ManufacturerId}' of {DocumentText}!";

                else if (!manufacturer.InScope(Model.Type == QIDigitalModel.ChargingStationModelType ? QIDigitalItem.ChargingStationType : QIDigitalItem.EnergyMeterType))
                    ErrorResponse = $"The certificate '{Certificate.Id}' approves a quality system for {String.Join(", ", manufacturer.Scope)}, not for the model '{Model.ModelId}' of {DocumentText}!";

                else
                    ErrorResponse = null;

            }

            else if (Certificate.Model!.Type != Model.Type ||
                     !QIDigitalModel.SameId(Certificate.Model.ManufacturerId, Model.ManufacturerId) ||
                     !QIDigitalModel.SameId(Certificate.Model.ModelId,        Model.ModelId))
                ErrorResponse = $"The certificate '{Certificate.Id}' is one of the model '{Certificate.Model.ModelId}' of '{Certificate.Model.ManufacturerId}', not of '{Model.ModelId}' of '{Model.ManufacturerId}'!";

            else if (Model.HardwareRevisions.FirstOrDefault(hardwareRevision => !Certificate.Model.HardwareRevisions.Contains(hardwareRevision)) is HardwareRevision uncoveredHardwareRevision &&
                     uncoveredHardwareRevision.IsNotNullOrEmpty)
                ErrorResponse = $"The certificate '{Certificate.Id}' does not cover the hardware revision '{uncoveredHardwareRevision}' of {DocumentText}!";

            else if (Model.FirmwareVersions.FirstOrDefault(firmwareVersion => !Certificate.Model.FirmwareVersions.Contains(firmwareVersion)) is FirmwareVersion uncoveredFirmwareVersion &&
                     uncoveredFirmwareVersion.IsNotNullOrEmpty)
                ErrorResponse = $"The certificate '{Certificate.Id}' does not cover the firmware version '{uncoveredFirmwareVersion}' of {DocumentText}!";

            else
                ErrorResponse = null;

            return ErrorResponse is null;

        }

        #endregion


        #region (static) TryParse(Text, out Declaration, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON text of a digital declaration of conformity.
        /// </summary>
        public static Boolean TryParse(String                                                    Text,
                                       [NotNullWhen(true)]  out DigitalDeclarationOfConformity?  Declaration,
                                       [NotNullWhen(false)] out String?                          ErrorResponse)
        {

            Declaration = null;

            try
            {

                if (POIJSON.Read(Text) is not JObject json)
                {
                    ErrorResponse = "The given JSON text must be a JSON object!";
                    return false;
                }

                return TryParse(json, out Declaration, out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(JSON, out Declaration, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a digital declaration of conformity. Its signature is not verified here.
        /// </summary>
        public static Boolean TryParse(JObject                                                   JSON,
                                       [NotNullWhen(true)]  out DigitalDeclarationOfConformity?  Declaration,
                                       [NotNullWhen(false)] out String?                          ErrorResponse)
        {

            Declaration = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "a digital declaration of conformity", out ErrorResponse,
                                           QIDigitalSignature.PropertyKeys.Context, PropertyKeys.Id, PropertyKeys.Model,
                                           PropertyKeys.Legislations, PropertyKeys.Standards, PropertyKeys.NotifiedBodies,
                                           PropertyKeys.AdditionalInformation, PropertyKeys.PlaceOfIssue, PropertyKeys.Signatory,
                                           QIDigitalSignature.PropertyKeys.Signature) ||
                    !QIDigitalJSON.TryContext(JSON, JSONLDContext, "a digital declaration of conformity", out ErrorResponse))
                {
                    return false;
                }

                if (!QIDigitalJSON.TryParseRequiredText(JSON, PropertyKeys.Id,                    "declaration of conformity identification",  DigitalDeclarationOfConformity_Id.TryParse, out DigitalDeclarationOfConformity_Id id, out ErrorResponse) ||
                    !POIJSON.TryParseObject<QIDigitalModel>           (JSON, PropertyKeys.Model,                 "model",                     QIDigitalModel.      TryParse, out var model,                  out ErrorResponse) ||
                    !POIJSON.TryParseObjects<QIDigitalLegislation>    (JSON, PropertyKeys.Legislations,          "legislation",               QIDigitalLegislation.TryParse, out var legislations,           out ErrorResponse) ||
                    !POIJSON.TryParseObjects<QIDigitalStandard>       (JSON, PropertyKeys.Standards,             "standard",                  QIDigitalStandard.   TryParse, out var standards,              out ErrorResponse) ||
                    !POIJSON.TryParseObjects<DDoCNotifiedBody>        (JSON, PropertyKeys.NotifiedBodies,        "notified body",             DDoCNotifiedBody.    TryParse, out var notifiedBodies,         out ErrorResponse) ||
                    !POIJSON.TryParseObject<QIDigitalPerson>          (JSON, PropertyKeys.Signatory,             "signatory",                 QIDigitalPerson.     TryParse, out var signatory,              out ErrorResponse) ||
                    !QIDigitalJSON.TryParseI18N                       (JSON, PropertyKeys.AdditionalInformation, "additional information",                                   out var additionalInformation,  out ErrorResponse) ||
                    !POIJSON.TryParseString                           (JSON, PropertyKeys.PlaceOfIssue,          "place of issue",                                           out var placeOfIssue,           out ErrorResponse) ||
                    !TryParseSignature                                (JSON,                                                                                                 out var signature,              out ErrorResponse))
                {
                    return false;
                }

                if (model is null || signatory is null || placeOfIssue is null)
                {
                    ErrorResponse = $"The digital declaration of conformity '{id}' requires its model, its place of issue and its signatory!";
                    return false;
                }

                Declaration    = new DigitalDeclarationOfConformity(id,
                                                                    model,
                                                                    legislations,
                                                                    placeOfIssue,
                                                                    signatory,
                                                                    standards,
                                                                    notifiedBodies,
                                                                    additionalInformation,
                                                                    signature);

                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                Declaration    = null;
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
                           new JProperty(PropertyKeys.Id,                        Id.ToString()),
                           new JProperty(PropertyKeys.Model,                     Model.ToJSON()),
                           new JProperty(PropertyKeys.Legislations,              new JArray(Legislations.Select(legislation => legislation.ToJSON())))
                       );

            if (Standards.Length > 0)
                json.Add(PropertyKeys.Standards,                 new JArray(Standards.     Select(standard     => standard.    ToJSON())));

            if (NotifiedBodies.Length > 0)
                json.Add(PropertyKeys.NotifiedBodies,            new JArray(NotifiedBodies.Select(notifiedBody => notifiedBody.ToJSON())));

            if (AdditionalInformation.IsNotNullOrEmpty())
                json.Add(PropertyKeys.AdditionalInformation,     AdditionalInformation.ToJSON());

            json.Add(PropertyKeys.PlaceOfIssue,                  PlaceOfIssue);
            json.Add(PropertyKeys.Signatory,                     Signatory.ToJSON());

            return json;

        }

        #endregion

        #region (protected) WithSignature(Signature)

        /// <inheritdoc/>
        protected override DigitalDeclarationOfConformity WithSignature(QIDigitalSignature Signature)

            => new (Id,
                    Model,
                    Legislations,
                    PlaceOfIssue,
                    Signatory,
                    Standards,
                    NotifiedBodies,
                    AdditionalInformation,
                    Signature);

        #endregion

    }

}
