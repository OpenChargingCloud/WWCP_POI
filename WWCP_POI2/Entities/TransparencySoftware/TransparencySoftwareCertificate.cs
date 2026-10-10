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
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// An approval or compatibility document for a charging station model/version
    /// and the transparency software releases it names.
    /// This is a domain document, not a cryptographic certificate.
    /// </summary>
    public sealed class TransparencySoftwareCertificate
    {

        #region Properties

        /// <summary>
        /// The stable identity of this document.
        /// </summary>
        [Mandatory]
        public TransparencySoftwareCertificate_Id       Id                                   { get; }

        /// <summary>
        /// The issuing organization.
        /// </summary>
        [Mandatory]
        public String                                   Issuer                               { get; }

        /// <summary>
        /// The charging station model to which this document applies.
        /// </summary>
        [Mandatory]
        public String                                   ChargingStationModel                 { get; }

        /// <summary>
        /// The charging station model version to which this document applies.
        /// </summary>
        [Mandatory]
        public String                                   ChargingStationModelVersion          { get; }

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
        /// The optional manufacturer of the charging station model.
        /// </summary>
        [Optional]
        public ChargingStationManufacturer_Id?          ChargingStationManufacturerId        { get; }

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
        /// Create a new approval or compatibility document for a charging station model/version.
        /// </summary>
        /// <param name="Id">The stable identity of this document.</param>
        /// <param name="Issuer">The issuing organization.</param>
        /// <param name="ChargingStationModel">The charging station model to which this document applies.</param>
        /// <param name="ChargingStationModelVersion">The charging station model version to which this document applies.</param>
        /// <param name="VerifiedTransparencySoftwareIds">The transparency software releases officially checked with this model/version.</param>
        /// <param name="CompatibleTransparencySoftwareIds">Compatible additional releases, without a claim of official verification.</param>
        /// <param name="ChargingStationManufacturerId">The optional manufacturer of the charging station model.</param>
        /// <param name="DocumentNumber">The optional document number of the issuer.</param>
        /// <param name="DocumentURL">The optional location of the document.</param>
        /// <param name="NotBefore">The optional start of the validity of this document.</param>
        /// <param name="NotAfter">The optional end of the validity of this document.</param>
        public TransparencySoftwareCertificate(TransparencySoftwareCertificate_Id     Id,
                                               String                                 Issuer,
                                               String                                 ChargingStationModel,
                                               String                                 ChargingStationModelVersion,
                                               IEnumerable<TransparencySoftware_Id>?  VerifiedTransparencySoftwareIds     = null,
                                               IEnumerable<TransparencySoftware_Id>?  CompatibleTransparencySoftwareIds   = null,
                                               ChargingStationManufacturer_Id?        ChargingStationManufacturerId       = null,
                                               String?                                DocumentNumber                      = null,
                                               URL?                                   DocumentURL                         = null,
                                               DateTimeOffset?                        NotBefore                           = null,
                                               DateTimeOffset?                        NotAfter                            = null)
        {

            if (Id.IsNullOrEmpty)
                throw new ArgumentException("A certificate document identifier is required.", nameof(Id));

            ArgumentException.ThrowIfNullOrWhiteSpace(Issuer);
            ArgumentException.ThrowIfNullOrWhiteSpace(ChargingStationModel);
            ArgumentException.ThrowIfNullOrWhiteSpace(ChargingStationModelVersion);

            if (ChargingStationManufacturerId is { } manufacturerId && manufacturerId.IsNullOrEmpty)
                throw new ArgumentException("Invalid manufacturer identifier.", nameof(ChargingStationManufacturerId));

            if (DocumentURL is { } documentURL && !Uri.TryCreate(documentURL.ToString(), UriKind.Absolute, out _))
                throw new ArgumentException("documentURL must be absolute.", nameof(DocumentURL));

            if (NotBefore > NotAfter)
                throw new ArgumentException("notBefore must not exceed notAfter.", nameof(NotBefore));

            var verifiedIds    = References(VerifiedTransparencySoftwareIds);
            var compatibleIds  = References(CompatibleTransparencySoftwareIds);

            if (verifiedIds.IsEmpty && compatibleIds.IsEmpty)
                throw new ArgumentException("A document must identify at least one software release.");

            if (verifiedIds.Intersect(compatibleIds).Any())
                throw new ArgumentException("A release cannot be both verified and merely compatible in the same document.");

            this.Id                                 = Id;
            this.Issuer                             = Issuer;
            this.ChargingStationModel               = ChargingStationModel;
            this.ChargingStationModelVersion        = ChargingStationModelVersion;
            this.VerifiedTransparencySoftwareIds    = verifiedIds;
            this.CompatibleTransparencySoftwareIds  = compatibleIds;
            this.ChargingStationManufacturerId      = ChargingStationManufacturerId;
            this.DocumentNumber                     = DocumentNumber;
            this.DocumentURL                        = DocumentURL;
            this.NotBefore                          = NotBefore?.ToUniversalTime();
            this.NotAfter                           = NotAfter?. ToUniversalTime();

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

                TransparencyJSON.OnlyProperties(JSON, "@id", "issuer", "chargingStationModel", "chargingStationModelVersion",
                                                      "chargingStationManufacturerId", "documentNumber", "documentURL",
                                                      "verifiedTransparencySoftwareIds", "compatibleTransparencySoftwareIds",
                                                      "notBefore", "notAfter");

                Certificate = new TransparencySoftwareCertificate(
                                  Id:                                 TransparencySoftwareCertificate_Id.Parse(TransparencyJSON.RequiredText(JSON, "@id")),
                                  Issuer:                             TransparencyJSON.RequiredText(JSON, "issuer"),
                                  ChargingStationModel:               TransparencyJSON.RequiredText(JSON, "chargingStationModel"),
                                  ChargingStationModelVersion:        TransparencyJSON.RequiredText(JSON, "chargingStationModelVersion"),
                                  VerifiedTransparencySoftwareIds:    TransparencyJSON.Array(JSON, "verifiedTransparencySoftwareIds",
                                                                                             token => TransparencySoftware_Id.Parse(TransparencyJSON.Identifier(token))),
                                  CompatibleTransparencySoftwareIds:  TransparencyJSON.Array(JSON, "compatibleTransparencySoftwareIds",
                                                                                             token => TransparencySoftware_Id.Parse(TransparencyJSON.Identifier(token))),
                                  ChargingStationManufacturerId:      TransparencyJSON.Text(JSON, "chargingStationManufacturerId") is { } manufacturerId
                                                                          ? ChargingStationManufacturer_Id.Parse(manufacturerId)
                                                                          : null,
                                  DocumentNumber:                     TransparencyJSON.Text(JSON, "documentNumber"),
                                  DocumentURL:                        TransparencyJSON.Link(JSON, "documentURL"),
                                  NotBefore:                          TransparencyJSON.Date(JSON, "notBefore"),
                                  NotAfter:                           TransparencyJSON.Date(JSON, "notAfter")
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

        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of this document. Software releases are referenced
        /// by their identifications, in ordinal order.
        /// </summary>
        public JObject ToJSON()
        {

            var json = JSONObject.Create(

                                 new JProperty("@id",                                Id.ToString()),
                                 new JProperty("issuer",                             Issuer),
                                 new JProperty("chargingStationModel",               ChargingStationModel),
                                 new JProperty("chargingStationModelVersion",        ChargingStationModelVersion),
                                 new JProperty("verifiedTransparencySoftwareIds",    new JArray(VerifiedTransparencySoftwareIds.  Select(id => id.ToString()).Order(StringComparer.Ordinal))),
                                 new JProperty("compatibleTransparencySoftwareIds",  new JArray(CompatibleTransparencySoftwareIds.Select(id => id.ToString()).Order(StringComparer.Ordinal))),

                           ChargingStationManufacturerId.HasValue
                               ? new JProperty("chargingStationManufacturerId",      ChargingStationManufacturerId.Value.ToString())
                               : null,

                           DocumentNumber is not null
                               ? new JProperty("documentNumber",                     DocumentNumber)
                               : null,

                           DocumentURL.HasValue
                               ? new JProperty("documentURL",                        DocumentURL.Value.ToString())
                               : null,

                           NotBefore.HasValue
                               ? new JProperty("notBefore",                          NotBefore.Value.ToString("O", CultureInfo.InvariantCulture))
                               : null,

                           NotAfter.HasValue
                               ? new JProperty("notAfter",                           NotAfter. Value.ToString("O", CultureInfo.InvariantCulture))
                               : null

                       );

            return json;

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

            => $"{Id}: {Issuer}, {ChargingStationModel} {ChargingStationModelVersion}";

        #endregion

    }

}
