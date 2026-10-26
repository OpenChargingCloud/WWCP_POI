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

    /// <summary>
    /// An approval or compatibility document for a charging station model, in the hardware revisions and firmware versions
    /// it covers, and the transparency software releases it names, embedded in the conformity assessment body issuing it,
    /// e.g. under the legislation it is a notified body for.
    /// This is a domain document, not a cryptographic certificate.
    /// </summary>
    public sealed class TransparencySoftwareCertificate
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/transparencySoftwareCertificate/v1";

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
            public const String  Id                                  = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context                             = "@context";

            /// <summary>
            /// The property of the legislation it is issued under, e.g. MID.
            /// </summary>
            public const String  Legislation                         = "legislation";

            /// <summary>
            /// The property of its charging station model.
            /// </summary>
            public const String  ChargingStationModel                = "chargingStationModel";

            /// <summary>
            /// The property of the hardware revisions of its charging station model it covers.
            /// </summary>
            public const String  HardwareRevisions                   = "hardwareRevisions";

            /// <summary>
            /// The property of the firmware versions of its charging station model it covers.
            /// </summary>
            public const String  FirmwareVersions                    = "firmwareVersions";

            /// <summary>
            /// The property of its verified transparency software identifications.
            /// </summary>
            public const String  VerifiedTransparencySoftwareIds     = "verifiedTransparencySoftwareIds";

            /// <summary>
            /// The property of its compatible transparency software identifications.
            /// </summary>
            public const String  CompatibleTransparencySoftwareIds   = "compatibleTransparencySoftwareIds";

            /// <summary>
            /// The property of its charging station manufacturer identification.
            /// </summary>
            public const String  ChargingStationManufacturerId       = "chargingStationManufacturerId";

            /// <summary>
            /// The property of its document number.
            /// </summary>
            public const String  DocumentNumber                      = "documentNumber";

            /// <summary>
            /// The property of its document URL.
            /// </summary>
            public const String  DocumentURL                         = "documentURL";

            /// <summary>
            /// The property of its start of validity.
            /// </summary>
            public const String  NotBefore                           = "notBefore";

            /// <summary>
            /// The property of its end of validity.
            /// </summary>
            public const String  NotAfter                            = "notAfter";

        }

        #endregion

        /// <summary>
        /// The stable identity of this document.
        /// </summary>
        [Mandatory]
        public TransparencySoftwareCertificate_Id       Id                                   { get; }

        /// <summary>
        /// The optional legislation it is issued under, e.g. MID or CRA: one its conformity assessment body is notified for.
        /// </summary>
        [Optional]
        public String?                                  Legislation                          { get; }

        /// <summary>
        /// The manufacturer of the charging station model, as charging stations name it by their manufacturerId.
        /// </summary>
        [Mandatory]
        public ChargingStationManufacturer_Id           ChargingStationManufacturerId        { get; }

        /// <summary>
        /// The charging station model to which this document applies, one of its manufacturer,
        /// as charging stations name it by their modelId.
        /// </summary>
        [Mandatory]
        public ChargingStationModel_Id                  ChargingStationModel                 { get; }

        /// <summary>
        /// The hardware revisions of its charging station model to which this document applies, each one of its model,
        /// in their given order.
        /// </summary>
        [Mandatory]
        public ImmutableArray<HardwareRevision>         HardwareRevisions                    { get; }

        /// <summary>
        /// The firmware versions of its charging station model to which this document applies, each one of its model, in
        /// their given order.
        /// </summary>
        [Mandatory]
        public ImmutableArray<FirmwareVersion>          FirmwareVersions                     { get; }

        /// <summary>
        /// The transparency software releases officially checked with this model/version.
        /// </summary>
        [Mandatory]
        public ImmutableArray<TransparencySoftware_Id>  VerifiedTransparencySoftwareIds      { get; }

        /// <summary>
        /// Compatible additional releases, without a claim of official verification by this document.
        /// </summary>
        [Mandatory]
        public ImmutableArray<TransparencySoftware_Id>  CompatibleTransparencySoftwareIds    { get; }

        /// <summary>
        /// The optional document number of the issuer, if different from the identity.
        /// </summary>
        [Optional]
        public String?                                  DocumentNumber                       { get; }

        /// <summary>
        /// The optional location of the document.
        /// </summary>
        [Optional]
        public URL?                                     DocumentURL                          { get; }

        /// <summary>
        /// The optional start of the validity of this document.
        /// </summary>
        [Optional]
        public DateTimeOffset?                          NotBefore                            { get; }

        /// <summary>
        /// The optional end of the validity of this document.
        /// </summary>
        [Optional]
        public DateTimeOffset?                          NotAfter                             { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new approval or compatibility document for a charging station model, in the given hardware revisions
        /// and firmware versions.
        /// </summary>
        /// <param name="Id">The stable identity of this document.</param>
        /// <param name="ChargingStationManufacturerId">The manufacturer of the charging station model.</param>
        /// <param name="ChargingStationModel">The charging station model to which this document applies.</param>
        /// <param name="HardwareRevisions">The hardware revisions of the model to which this document applies.</param>
        /// <param name="FirmwareVersions">The firmware versions of the model to which this document applies.</param>
        /// <param name="VerifiedTransparencySoftwareIds">The transparency software releases officially checked with this model.</param>
        /// <param name="CompatibleTransparencySoftwareIds">Compatible additional releases, without a claim of official verification.</param>
        /// <param name="DocumentNumber">The optional document number of the issuer.</param>
        /// <param name="DocumentURL">The optional location of the document.</param>
        /// <param name="NotBefore">The optional start of the validity of this document.</param>
        /// <param name="NotAfter">The optional end of the validity of this document.</param>
        /// <param name="Legislation">The optional legislation it is issued under, e.g. MID.</param>
        public TransparencySoftwareCertificate(TransparencySoftwareCertificate_Id     Id,
                                               ChargingStationManufacturer_Id         ChargingStationManufacturerId,
                                               ChargingStationModel_Id                ChargingStationModel,
                                               IEnumerable<HardwareRevision>          HardwareRevisions,
                                               IEnumerable<FirmwareVersion>           FirmwareVersions,
                                               IEnumerable<TransparencySoftware_Id>?  VerifiedTransparencySoftwareIds     = null,
                                               IEnumerable<TransparencySoftware_Id>?  CompatibleTransparencySoftwareIds   = null,
                                               String?                                DocumentNumber                      = null,
                                               URL?                                   DocumentURL                         = null,
                                               DateTimeOffset?                        NotBefore                           = null,
                                               DateTimeOffset?                        NotAfter                            = null,
                                               String?                                Legislation                         = null)
        {

            if (Id.IsNullOrEmpty)
                throw new ArgumentException("A certificate document identifier is required.", nameof(Id));

            if (ChargingStationModel.IsNullOrEmpty)
                throw new ArgumentException("A charging station model is required.", nameof(ChargingStationModel));

            var hardwareRevisions  = ModelVersions.Of(HardwareRevisions, "hardware revision", $"the certificate '{Id}'");
            var firmwareVersions   = ModelVersions.Of(FirmwareVersions,  "firmware version",  $"the certificate '{Id}'");

            if (hardwareRevisions.IsEmpty)
                throw new ArgumentException($"The certificate '{Id}' must name at least one hardware revision of its model!", nameof(HardwareRevisions));

            if (firmwareVersions.IsEmpty)
                throw new ArgumentException($"The certificate '{Id}' must name at least one firmware version of its model!", nameof(FirmwareVersions));

            if (ChargingStationManufacturerId.IsNullOrEmpty)
                throw new ArgumentException("The manufacturer of the charging station model is required.", nameof(ChargingStationManufacturerId));

            if (DocumentURL is { } documentURL && !Uri.TryCreate(documentURL.ToString(), UriKind.Absolute, out _))
                throw new ArgumentException("documentURL must be absolute.", nameof(DocumentURL));

            if (Legislation is not null && String.IsNullOrWhiteSpace(Legislation))
                throw new ArgumentException("A legislation must not be empty.", nameof(Legislation));

            if (NotBefore > NotAfter)
                throw new ArgumentException("notBefore must not exceed notAfter.", nameof(NotBefore));

            var verifiedIds    = References(VerifiedTransparencySoftwareIds);
            var compatibleIds  = References(CompatibleTransparencySoftwareIds);

            if (verifiedIds.IsEmpty && compatibleIds.IsEmpty)
                throw new ArgumentException("A document must identify at least one software release.");

            if (verifiedIds.Intersect(compatibleIds).Any())
                throw new ArgumentException("A release cannot be both verified and merely compatible in the same document.");

            this.Id                                 = Id;
            this.Legislation                        = Legislation;
            this.ChargingStationModel               = ChargingStationModel;
            this.HardwareRevisions                  = hardwareRevisions;
            this.FirmwareVersions                   = firmwareVersions;
            this.VerifiedTransparencySoftwareIds    = verifiedIds;
            this.CompatibleTransparencySoftwareIds  = compatibleIds;
            this.ChargingStationManufacturerId      = ChargingStationManufacturerId;
            this.DocumentNumber                     = DocumentNumber;
            this.DocumentURL                        = DocumentURL;
            // To the millisecond, as their representations keep them.
            this.NotBefore                          = NotBefore.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(NotBefore.Value.ToUnixTimeMilliseconds()) : null;
            this.NotAfter                           = NotAfter. HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(NotAfter. Value.ToUnixTimeMilliseconds()) : null;

        }

        #endregion


        #region (static) Parse   (JSON)

        /// <summary>
        /// Parse the given JSON representation of an approval or compatibility document.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        public static TransparencySoftwareCertificate Parse(JObject JSON)
        {

            if (TryParse(JSON, out var certificate, out var errorResponse))
                return certificate;

            throw new ArgumentException(errorResponse, nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out Certificate, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an approval or compatibility document.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Certificate">The parsed document.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                                   JSON,
                                       [NotNullWhen(true)]  out TransparencySoftwareCertificate?  Certificate,
                                       [NotNullWhen(false)] out String?                           ErrorResponse)
        {

            Certificate    = null;
            ErrorResponse  = null;

            try
            {

                StrictJSON.OnlyProperties(JSON, PropertyKeys.Id, PropertyKeys.Context, PropertyKeys.Legislation, PropertyKeys.ChargingStationModel,
                                                PropertyKeys.HardwareRevisions, PropertyKeys.FirmwareVersions, PropertyKeys.ChargingStationManufacturerId,
                                                PropertyKeys.DocumentNumber, PropertyKeys.DocumentURL,
                                                PropertyKeys.VerifiedTransparencySoftwareIds, PropertyKeys.CompatibleTransparencySoftwareIds,
                                                PropertyKeys.NotBefore, PropertyKeys.NotAfter);

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    throw new ArgumentException($"The JSON-LD context of the transparency software certificate must be '{JSONLDContext}'!");
                }

                Certificate = new TransparencySoftwareCertificate(
                                  Id:                                 TransparencySoftwareCertificate_Id.Parse(StrictJSON.RequiredText(JSON, PropertyKeys.Id)),
                                  ChargingStationManufacturerId:      ChargingStationManufacturer_Id.Parse(StrictJSON.RequiredText(JSON, PropertyKeys.ChargingStationManufacturerId)),
                                  ChargingStationModel:               ChargingStationModel_Id.Parse(StrictJSON.RequiredText(JSON, PropertyKeys.ChargingStationModel)),
                                  HardwareRevisions:                  StrictJSON.Array(JSON, PropertyKeys.HardwareRevisions, token => HardwareRevision.Parse(StrictJSON.Identifier(token))),
                                  FirmwareVersions:                   StrictJSON.Array(JSON, PropertyKeys.FirmwareVersions,  token => FirmwareVersion. Parse(StrictJSON.Identifier(token))),
                                  VerifiedTransparencySoftwareIds:    StrictJSON.Array(JSON, PropertyKeys.VerifiedTransparencySoftwareIds,
                                                                                             token => TransparencySoftware_Id.Parse(StrictJSON.Identifier(token))),
                                  CompatibleTransparencySoftwareIds:  StrictJSON.Array(JSON, PropertyKeys.CompatibleTransparencySoftwareIds,
                                                                                             token => TransparencySoftware_Id.Parse(StrictJSON.Identifier(token))),
                                  DocumentNumber:                     StrictJSON.Text(JSON, PropertyKeys.DocumentNumber),
                                  DocumentURL:                        StrictJSON.Link(JSON, PropertyKeys.DocumentURL),
                                  NotBefore:                          StrictJSON.Date(JSON, PropertyKeys.NotBefore),
                                  NotAfter:                           StrictJSON.Date(JSON, PropertyKeys.NotAfter),
                                  Legislation:                        StrictJSON.Text(JSON, PropertyKeys.Legislation)
                              );

                return true;

            }
            catch (Exception exception)
            {
                Certificate    = null;
                ErrorResponse  = exception.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false, CustomTransparencySoftwareCertificateSerializer = null)

        /// <summary>
        /// Return a JSON representation of this document. Software releases are referenced
        /// by their identifications, in ordinal order.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomTransparencySoftwareCertificateSerializer">A delegate to serialize custom transparency software certificate JSON objects.</param>
        public JObject ToJSON(Boolean                                                            Embedded                                          = false,
                              CustomJObjectSerializerDelegate<TransparencySoftwareCertificate>?  CustomTransparencySoftwareCertificateSerializer   = null)
        {

            var json = JSONObject.Create(

                                 new JProperty(PropertyKeys.Id,                                  Id.ToString()),

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,                             JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.ChargingStationManufacturerId,       ChargingStationManufacturerId.ToString()),
                                 new JProperty(PropertyKeys.ChargingStationModel,                ChargingStationModel.ToString()),
                                 new JProperty(PropertyKeys.HardwareRevisions,                   new JArray(HardwareRevisions.Select(hardwareRevision => hardwareRevision.ToString()))),
                                 new JProperty(PropertyKeys.FirmwareVersions,                    new JArray(FirmwareVersions. Select(firmwareVersion  => firmwareVersion. ToString()))),
                                 new JProperty(PropertyKeys.VerifiedTransparencySoftwareIds,     new JArray(VerifiedTransparencySoftwareIds.  Select(id => id.ToString()).Order(StringComparer.Ordinal))),
                                 new JProperty(PropertyKeys.CompatibleTransparencySoftwareIds,   new JArray(CompatibleTransparencySoftwareIds.Select(id => id.ToString()).Order(StringComparer.Ordinal))),

                           DocumentNumber is not null
                               ? new JProperty(PropertyKeys.DocumentNumber,                      DocumentNumber)
                               : null,

                           DocumentURL.HasValue
                               ? new JProperty(PropertyKeys.DocumentURL,                         DocumentURL.Value.ToString())
                               : null,

                           NotBefore.HasValue
                               ? new JProperty(PropertyKeys.NotBefore,                           NotBefore.Value.ToISO8601())
                               : null,

                           NotAfter.HasValue
                               ? new JProperty(PropertyKeys.NotAfter,                            NotAfter. Value.ToISO8601())
                               : null,

                           Legislation is not null
                               ? new JProperty(PropertyKeys.Legislation,                         Legislation)
                               : null

                       );

            return CustomTransparencySoftwareCertificateSerializer is not null
                       ? CustomTransparencySoftwareCertificateSerializer(this, json)
                       : json;

        }

        #endregion


        #region Covers(TransparencySoftwareId)

        /// <summary>
        /// Whether this document names the given software release as verified or compatible.
        /// </summary>
        /// <param name="TransparencySoftwareId">A software release.</param>
        public Boolean Covers(TransparencySoftware_Id TransparencySoftwareId)

            => VerifiedTransparencySoftwareIds.  Contains(TransparencySoftwareId) ||
               CompatibleTransparencySoftwareIds.Contains(TransparencySoftwareId);

        #endregion

        #region (private static) References(Ids)

        private static ImmutableArray<TransparencySoftware_Id> References(IEnumerable<TransparencySoftware_Id>? Ids)
        {

            var references  = (Ids ?? []).ToImmutableArray();
            var seen        = new HashSet<String>(StringComparer.Ordinal);

            foreach (var id in references)
                if (id.IsNullOrEmpty || !seen.Add(id.ToString()))
                    throw new ArgumentException("Empty or duplicate TransparencySoftware reference.");

            return references;

        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Id}: {ChargingStationModel} {String.Join("/", HardwareRevisions)}";

        #endregion

    }

}
