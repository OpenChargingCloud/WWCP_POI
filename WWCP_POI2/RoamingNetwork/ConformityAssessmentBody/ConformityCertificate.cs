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
    /// A certificate of conformity for a model of charging stations or a model of energy meters, in the hardware revisions
    /// and firmware versions it covers, embedded in the conformity assessment body issuing it, e.g. under the legislation
    /// it is a notified body for. It names the components it was issued with: the transparency software releases it
    /// verifies or finds compatible, products of their own manufacturers, and, for a charging station model, the energy
    /// meters it mounts. A charging station names the certificates of its model, an energy meter those of its model.
    /// This is a domain document, not a cryptographic certificate.
    /// </summary>
    public sealed class ConformityCertificate
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/certificate/v1";

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
            /// The property of its charging station manufacturer identification.
            /// </summary>
            public const String  ChargingStationManufacturerId       = "chargingStationManufacturerId";

            /// <summary>
            /// The property of its charging station model.
            /// </summary>
            public const String  ChargingStationModel                = "chargingStationModel";

            /// <summary>
            /// The property of its energy meter manufacturer identification.
            /// </summary>
            public const String  EnergyMeterManufacturerId           = "energyMeterManufacturerId";

            /// <summary>
            /// The property of its energy meter model.
            /// </summary>
            public const String  EnergyMeterModel                    = "energyMeterModel";

            /// <summary>
            /// The property of the hardware revisions of its model it covers.
            /// </summary>
            public const String  HardwareRevisions                   = "hardwareRevisions";

            /// <summary>
            /// The property of the firmware versions of its model it covers.
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
            /// The property of the energy meters of a charging station model it was issued with.
            /// </summary>
            public const String  EnergyMeters                        = "energyMeters";

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
        public ConformityCertificate_Id                 Id                                   { get; }

        /// <summary>
        /// The optional legislation it is issued under, e.g. MID or CRA: one its conformity assessment body is notified for.
        /// </summary>
        [Optional]
        public String?                                  Legislation                          { get; }

        /// <summary>
        /// The manufacturer of the charging station model it is for, as charging stations name it by their
        /// manufacturerId; none, when it is for an energy meter model.
        /// </summary>
        [Optional]
        public ChargingStationManufacturer_Id?          ChargingStationManufacturerId        { get; }

        /// <summary>
        /// The charging station model it is for, one of its manufacturer, as charging stations name it by their modelId;
        /// none, when it is for an energy meter model.
        /// </summary>
        [Optional]
        public ChargingStationModel_Id?                 ChargingStationModel                 { get; }

        /// <summary>
        /// The manufacturer of the energy meter model it is for, as energy meters name it by their manufacturerId; none,
        /// when it is for a charging station model.
        /// </summary>
        [Optional]
        public EnergyMeterManufacturer_Id?              EnergyMeterManufacturerId            { get; }

        /// <summary>
        /// The energy meter model it is for, one of its manufacturer, as energy meters name it by their modelId; none,
        /// when it is for a charging station model.
        /// </summary>
        [Optional]
        public EnergyMeterModel_Id?                     EnergyMeterModel                     { get; }

        /// <summary>
        /// The hardware revisions of its model to which this document applies, each one of its model, in their given
        /// order.
        /// </summary>
        [Mandatory]
        public ImmutableArray<String>                   HardwareRevisions                    { get; }

        /// <summary>
        /// The firmware versions of its model to which this document applies, each one of its model, in their given
        /// order.
        /// </summary>
        [Mandatory]
        public ImmutableArray<String>                   FirmwareVersions                     { get; }

        /// <summary>
        /// The transparency software releases officially checked with this model, components it was issued with.
        /// </summary>
        [Mandatory]
        public ImmutableArray<TransparencySoftware_Id>  VerifiedTransparencySoftwareIds      { get; }

        /// <summary>
        /// Compatible additional releases, without a claim of official verification by this document.
        /// </summary>
        [Mandatory]
        public ImmutableArray<TransparencySoftware_Id>  CompatibleTransparencySoftwareIds    { get; }

        /// <summary>
        /// The energy meters of a charging station model it was issued with, components of the station, in their given
        /// order; none for an energy meter model.
        /// </summary>
        [Optional]
        public ImmutableArray<CertifiedEnergyMeter>     EnergyMeters                         { get; }

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


        /// <summary>
        /// Whether it is for a charging station model, else for an energy meter model.
        /// </summary>
        public Boolean                                  IsForChargingStationModel
            => ChargingStationModel.HasValue;

        /// <summary>
        /// The model it is for, in words, e.g. "the charging station model 'GD-DC300' of 'GraphDefined'".
        /// </summary>
        public String                                   Subject
            => IsForChargingStationModel
                   ? $"the charging station model '{ChargingStationModel}' of '{ChargingStationManufacturerId}'"
                   : $"the energy meter model '{EnergyMeterModel}' of '{EnergyMeterManufacturerId}'";

        #endregion

        #region Constructor(s)

        #region ConformityCertificate(Id, ChargingStationManufacturerId, ChargingStationModel, HardwareRevisions, FirmwareVersions, ...)

        /// <summary>
        /// Create a new certificate of conformity for a charging station model, in the given hardware revisions and firmware
        /// versions, issued with the given transparency software releases and energy meters.
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
        /// <param name="EnergyMeters">The energy meters of the model it was issued with.</param>
        public ConformityCertificate(ConformityCertificate_Id               Id,
                                     ChargingStationManufacturer_Id         ChargingStationManufacturerId,
                                     ChargingStationModel_Id                ChargingStationModel,
                                     IEnumerable<String>                    HardwareRevisions,
                                     IEnumerable<String>                    FirmwareVersions,
                                     IEnumerable<TransparencySoftware_Id>?  VerifiedTransparencySoftwareIds     = null,
                                     IEnumerable<TransparencySoftware_Id>?  CompatibleTransparencySoftwareIds   = null,
                                     String?                                DocumentNumber                      = null,
                                     URL?                                   DocumentURL                         = null,
                                     DateTimeOffset?                        NotBefore                           = null,
                                     DateTimeOffset?                        NotAfter                            = null,
                                     String?                                Legislation                         = null,
                                     IEnumerable<CertifiedEnergyMeter>?     EnergyMeters                        = null)

            : this(Id,
                   ChargingStationManufacturerId,
                   ChargingStationModel,
                   null,
                   null,
                   HardwareRevisions,
                   FirmwareVersions,
                   VerifiedTransparencySoftwareIds,
                   CompatibleTransparencySoftwareIds,
                   EnergyMeters,
                   DocumentNumber,
                   DocumentURL,
                   NotBefore,
                   NotAfter,
                   Legislation)

        { }

        #endregion

        #region ConformityCertificate(Id, EnergyMeterManufacturerId, EnergyMeterModel, HardwareRevisions, FirmwareVersions, ...)

        /// <summary>
        /// Create a new certificate of conformity for an energy meter model, in the given hardware revisions and firmware
        /// versions, issued with the given transparency software releases.
        /// </summary>
        /// <param name="Id">The stable identity of this document.</param>
        /// <param name="EnergyMeterManufacturerId">The manufacturer of the energy meter model.</param>
        /// <param name="EnergyMeterModel">The energy meter model to which this document applies.</param>
        /// <param name="HardwareRevisions">The hardware revisions of the model to which this document applies.</param>
        /// <param name="FirmwareVersions">The firmware versions of the model to which this document applies.</param>
        /// <param name="VerifiedTransparencySoftwareIds">The transparency software releases officially checked with this model.</param>
        /// <param name="CompatibleTransparencySoftwareIds">Compatible additional releases, without a claim of official verification.</param>
        /// <param name="DocumentNumber">The optional document number of the issuer.</param>
        /// <param name="DocumentURL">The optional location of the document.</param>
        /// <param name="NotBefore">The optional start of the validity of this document.</param>
        /// <param name="NotAfter">The optional end of the validity of this document.</param>
        /// <param name="Legislation">The optional legislation it is issued under, e.g. MID.</param>
        public ConformityCertificate(ConformityCertificate_Id               Id,
                                     EnergyMeterManufacturer_Id             EnergyMeterManufacturerId,
                                     EnergyMeterModel_Id                    EnergyMeterModel,
                                     IEnumerable<String>                    HardwareRevisions,
                                     IEnumerable<String>                    FirmwareVersions,
                                     IEnumerable<TransparencySoftware_Id>?  VerifiedTransparencySoftwareIds     = null,
                                     IEnumerable<TransparencySoftware_Id>?  CompatibleTransparencySoftwareIds   = null,
                                     String?                                DocumentNumber                      = null,
                                     URL?                                   DocumentURL                         = null,
                                     DateTimeOffset?                        NotBefore                           = null,
                                     DateTimeOffset?                        NotAfter                            = null,
                                     String?                                Legislation                         = null)

            : this(Id,
                   null,
                   null,
                   EnergyMeterManufacturerId,
                   EnergyMeterModel,
                   HardwareRevisions,
                   FirmwareVersions,
                   VerifiedTransparencySoftwareIds,
                   CompatibleTransparencySoftwareIds,
                   null,
                   DocumentNumber,
                   DocumentURL,
                   NotBefore,
                   NotAfter,
                   Legislation)

        { }

        #endregion

        #region (private) ConformityCertificate(Id, ...)

        private ConformityCertificate(ConformityCertificate_Id               Id,
                                      ChargingStationManufacturer_Id?        ChargingStationManufacturerId,
                                      ChargingStationModel_Id?               ChargingStationModel,
                                      EnergyMeterManufacturer_Id?            EnergyMeterManufacturerId,
                                      EnergyMeterModel_Id?                   EnergyMeterModel,
                                      IEnumerable<String>                    HardwareRevisions,
                                      IEnumerable<String>                    FirmwareVersions,
                                      IEnumerable<TransparencySoftware_Id>?  VerifiedTransparencySoftwareIds,
                                      IEnumerable<TransparencySoftware_Id>?  CompatibleTransparencySoftwareIds,
                                      IEnumerable<CertifiedEnergyMeter>?     EnergyMeters,
                                      String?                                DocumentNumber,
                                      URL?                                   DocumentURL,
                                      DateTimeOffset?                        NotBefore,
                                      DateTimeOffset?                        NotAfter,
                                      String?                                Legislation)
        {

            if (Id.IsNullOrEmpty)
                throw new ArgumentException("A certificate document identifier is required.", nameof(Id));

            if (ChargingStationManufacturerId?.IsNullOrEmpty == true || ChargingStationModel?.IsNullOrEmpty == true ||
                EnergyMeterManufacturerId?.    IsNullOrEmpty == true || EnergyMeterModel?.    IsNullOrEmpty == true)
            {
                throw new ArgumentException($"The certificate '{Id}' must name its model and its manufacturer!");
            }

            // For exactly one model: of charging stations or of energy meters, each with its manufacturer.
            var forStation  = ChargingStationManufacturerId.HasValue || ChargingStationModel.HasValue;
            var forMeter    = EnergyMeterManufacturerId.    HasValue || EnergyMeterModel.    HasValue;

            if (forStation == forMeter)
                throw new ArgumentException($"The certificate '{Id}' must be for either a charging station model or an energy meter model!");

            if (forStation && !(ChargingStationManufacturerId.HasValue && ChargingStationModel.HasValue))
                throw new ArgumentException($"The certificate '{Id}' must name the charging station model it is for with its manufacturer!");

            if (forMeter   && !(EnergyMeterManufacturerId.    HasValue && EnergyMeterModel.    HasValue))
                throw new ArgumentException($"The certificate '{Id}' must name the energy meter model it is for with its manufacturer!");

            var hardwareRevisions  = ModelVersions.Of(HardwareRevisions, "hardware revision", $"the certificate '{Id}'");
            var firmwareVersions   = ModelVersions.Of(FirmwareVersions,  "firmware version",  $"the certificate '{Id}'");

            if (hardwareRevisions.IsEmpty)
                throw new ArgumentException($"The certificate '{Id}' must name at least one hardware revision of its model!", nameof(HardwareRevisions));

            if (firmwareVersions.IsEmpty)
                throw new ArgumentException($"The certificate '{Id}' must name at least one firmware version of its model!", nameof(FirmwareVersions));

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

            var energyMeters   = (EnergyMeters ?? []).ToImmutableArray();

            if (energyMeters.Any(energyMeter => energyMeter is null))
                throw new ArgumentException($"The energy meters of the certificate '{Id}' must not contain null entries!", nameof(EnergyMeters));

            if (forMeter && !energyMeters.IsEmpty)
                throw new ArgumentException($"The certificate '{Id}' of an energy meter model names no energy meters!", nameof(EnergyMeters));

            if (energyMeters.GroupBy(energyMeter => (energyMeter.ManufacturerId, energyMeter.ModelId)).FirstOrDefault(group => group.Count() > 1) is { } twice)
                throw new ArgumentException($"The energy meter model '{twice.Key.ModelId}' of '{twice.Key.ManufacturerId}' must not be given twice in the certificate '{Id}'!", nameof(EnergyMeters));

            this.Id                                 = Id;
            this.Legislation                        = Legislation;
            this.ChargingStationManufacturerId      = ChargingStationManufacturerId;
            this.ChargingStationModel               = ChargingStationModel;
            this.EnergyMeterManufacturerId          = EnergyMeterManufacturerId;
            this.EnergyMeterModel                   = EnergyMeterModel;
            this.HardwareRevisions                  = hardwareRevisions;
            this.FirmwareVersions                   = firmwareVersions;
            this.VerifiedTransparencySoftwareIds    = verifiedIds;
            this.CompatibleTransparencySoftwareIds  = compatibleIds;
            this.EnergyMeters                       = energyMeters;
            this.DocumentNumber                     = DocumentNumber;
            this.DocumentURL                        = DocumentURL;
            // To the millisecond, as their representations keep them.
            this.NotBefore                          = NotBefore.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(NotBefore.Value.ToUnixTimeMilliseconds()) : null;
            this.NotAfter                           = NotAfter. HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(NotAfter. Value.ToUnixTimeMilliseconds()) : null;

        }

        #endregion

        #endregion


        #region (static) Parse   (JSON)

        /// <summary>
        /// Parse the given JSON representation of a certificate of conformity.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        public static ConformityCertificate Parse(JObject JSON)
        {

            if (TryParse(JSON, out var certificate, out var errorResponse))
                return certificate;

            throw new ArgumentException(errorResponse, nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out Certificate, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a certificate of conformity, strictly.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Certificate">The parsed document.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out ConformityCertificate?  Certificate,
                                       [NotNullWhen(false)] out String?                 ErrorResponse)
        {

            Certificate    = null;
            ErrorResponse  = null;

            try
            {

                StrictJSON.OnlyProperties(JSON, PropertyKeys.Id, PropertyKeys.Context, PropertyKeys.Legislation,
                                                PropertyKeys.ChargingStationManufacturerId, PropertyKeys.ChargingStationModel,
                                                PropertyKeys.EnergyMeterManufacturerId, PropertyKeys.EnergyMeterModel,
                                                PropertyKeys.HardwareRevisions, PropertyKeys.FirmwareVersions,
                                                PropertyKeys.VerifiedTransparencySoftwareIds, PropertyKeys.CompatibleTransparencySoftwareIds,
                                                PropertyKeys.EnergyMeters, PropertyKeys.DocumentNumber, PropertyKeys.DocumentURL,
                                                PropertyKeys.NotBefore, PropertyKeys.NotAfter);

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    throw new ArgumentException($"The JSON-LD context of the certificate must be '{JSONLDContext}'!");
                }

                Certificate = new ConformityCertificate(
                                  Id:                                 ConformityCertificate_Id.Parse(StrictJSON.RequiredText(JSON, PropertyKeys.Id)),
                                  ChargingStationManufacturerId:      StrictJSON.Text(JSON, PropertyKeys.ChargingStationManufacturerId) is String stationMaker ? ChargingStationManufacturer_Id.Parse(stationMaker) : null,
                                  ChargingStationModel:               StrictJSON.Text(JSON, PropertyKeys.ChargingStationModel)          is String stationModel ? ChargingStationModel_Id.       Parse(stationModel) : null,
                                  EnergyMeterManufacturerId:          StrictJSON.Text(JSON, PropertyKeys.EnergyMeterManufacturerId)     is String meterMaker   ? EnergyMeterManufacturer_Id.    Parse(meterMaker)   : null,
                                  EnergyMeterModel:                   StrictJSON.Text(JSON, PropertyKeys.EnergyMeterModel)              is String meterModel   ? EnergyMeterModel_Id.           Parse(meterModel)   : null,
                                  HardwareRevisions:                  StrictJSON.Array(JSON, PropertyKeys.HardwareRevisions, StrictJSON.Identifier),
                                  FirmwareVersions:                   StrictJSON.Array(JSON, PropertyKeys.FirmwareVersions,  StrictJSON.Identifier),
                                  VerifiedTransparencySoftwareIds:    StrictJSON.Array(JSON, PropertyKeys.VerifiedTransparencySoftwareIds,
                                                                                             token => TransparencySoftware_Id.Parse(StrictJSON.Identifier(token))),
                                  CompatibleTransparencySoftwareIds:  StrictJSON.Array(JSON, PropertyKeys.CompatibleTransparencySoftwareIds,
                                                                                             token => TransparencySoftware_Id.Parse(StrictJSON.Identifier(token))),
                                  EnergyMeters:                       StrictJSON.Array(JSON, PropertyKeys.EnergyMeters, CertifiedEnergyMeter.Parse),
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

        #region ToJSON(Embedded = false, CustomConformityCertificateSerializer = null)

        /// <summary>
        /// Return a JSON representation of this document. Software releases are referenced
        /// by their identifications, in ordinal order.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomConformityCertificateSerializer">A delegate to serialize custom certificate JSON objects.</param>
        public JObject ToJSON(Boolean                                                  Embedded                                = false,
                              CustomJObjectSerializerDelegate<ConformityCertificate>?  CustomConformityCertificateSerializer   = null)
        {

            var json = JSONObject.Create(

                                 new JProperty(PropertyKeys.Id,                                  Id.ToString()),

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,                             JSONLDContext)
                               : null,

                           ChargingStationManufacturerId.HasValue
                               ? new JProperty(PropertyKeys.ChargingStationManufacturerId,       ChargingStationManufacturerId.Value.ToString())
                               : null,

                           ChargingStationModel.HasValue
                               ? new JProperty(PropertyKeys.ChargingStationModel,                ChargingStationModel.Value.ToString())
                               : null,

                           EnergyMeterManufacturerId.HasValue
                               ? new JProperty(PropertyKeys.EnergyMeterManufacturerId,           EnergyMeterManufacturerId.Value.ToString())
                               : null,

                           EnergyMeterModel.HasValue
                               ? new JProperty(PropertyKeys.EnergyMeterModel,                    EnergyMeterModel.Value.ToString())
                               : null,

                                 new JProperty(PropertyKeys.HardwareRevisions,                   new JArray(HardwareRevisions)),
                                 new JProperty(PropertyKeys.FirmwareVersions,                    new JArray(FirmwareVersions)),
                                 new JProperty(PropertyKeys.VerifiedTransparencySoftwareIds,     new JArray(VerifiedTransparencySoftwareIds.  Select(id => id.ToString()).Order(StringComparer.Ordinal))),
                                 new JProperty(PropertyKeys.CompatibleTransparencySoftwareIds,   new JArray(CompatibleTransparencySoftwareIds.Select(id => id.ToString()).Order(StringComparer.Ordinal))),

                           EnergyMeters.Length > 0
                               ? new JProperty(PropertyKeys.EnergyMeters,                        new JArray(EnergyMeters.Select(energyMeter => energyMeter.ToJSON())))
                               : null,

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

            return CustomConformityCertificateSerializer is not null
                       ? CustomConformityCertificateSerializer(this, json)
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

        #region Covers(HardwareRevision, FirmwareVersion)

        /// <summary>
        /// Whether this document covers the given hardware revision and firmware version of its model.
        /// </summary>
        public Boolean Covers(String?  HardwareRevision,
                              String?  FirmwareVersion)

            => HardwareRevision is not null && HardwareRevisions.Contains(HardwareRevision, StringComparer.Ordinal) &&
               FirmwareVersion  is not null && FirmwareVersions. Contains(FirmwareVersion,  StringComparer.Ordinal);

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

            => $"{Id}: {Subject}, hardware {String.Join("/", HardwareRevisions)}, firmware {String.Join("/", FirmwareVersions)}";

        #endregion

    }

}
