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

    /// <summary>
    /// A digital calibration certificate (DCC) of a single charging station or energy meter, issued and signed by the
    /// calibration laboratory, a conformity assessment body of the roaming network, e.g. a DAkkS accredited laboratory or
    /// a state-approved testing station verifying meters under the German calibration law.
    /// </summary>
    /// <remarks>
    /// Its structure follows the PTB DCC schema (3.x): its administrative data (dcc:administrativeData) are its
    /// identification (dcc:uniqueIdentifier), its country, its calibration laboratory with its code, its customer with
    /// the order number, the dates and the location of the calibration (dcc:coreData), its item (dcc:items), its
    /// responsible persons and its statements; its measurement results (dcc:measurementResults) follow. Its date of
    /// issue is when it was signed. The DCC signs with XAdES; this one with a signing key of its laboratory that may sign
    /// certificates.
    /// </remarks>
    public sealed class DigitalCalibrationCertificate : AQIDigitalDocument<DigitalCalibrationCertificate>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public static readonly String JSONLDContext = QIDigitalJSON.Context("digitalCalibrationCertificate");

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
            public const String  Id                         = "@id";

            /// <summary>
            /// The property of its country, ISO 3166-1 alpha-2.
            /// </summary>
            public const String  CountryCode                = "countryCode";

            /// <summary>
            /// The property of its calibration laboratory.
            /// </summary>
            public const String  CalibrationLaboratoryId    = "calibrationLaboratoryId";

            /// <summary>
            /// The property of the code of its calibration laboratory, e.g. its accreditation.
            /// </summary>
            public const String  CalibrationLaboratoryCode  = "calibrationLaboratoryCode";

            /// <summary>
            /// The property of its customer.
            /// </summary>
            public const String  Customer                   = "customer";

            /// <summary>
            /// The property of the order number of its customer.
            /// </summary>
            public const String  OrderNumber                = "orderNumber";

            /// <summary>
            /// The property of its item.
            /// </summary>
            public const String  Item                       = "item";

            /// <summary>
            /// The property of the beginning of the calibration.
            /// </summary>
            public const String  BeginPerformanceDate       = "beginPerformanceDate";

            /// <summary>
            /// The property of the end of the calibration.
            /// </summary>
            public const String  EndPerformanceDate         = "endPerformanceDate";

            /// <summary>
            /// The property of where the calibration was performed.
            /// </summary>
            public const String  PerformanceLocation        = "performanceLocation";

            /// <summary>
            /// The property of its responsible persons.
            /// </summary>
            public const String  RespPersons                = "respPersons";

            /// <summary>
            /// The property of its statements.
            /// </summary>
            public const String  Statements                 = "statements";

            /// <summary>
            /// The property of its measurement results.
            /// </summary>
            public const String  MeasurementResults         = "measurementResults";

            /// <summary>
            /// The property of when its item is to be calibrated again.
            /// </summary>
            public const String  RecalibrationDue           = "recalibrationDue";

            /// <summary>
            /// The property of its comment.
            /// </summary>
            public const String  Comment                    = "comment";

        }

        #endregion


        /// <summary>
        /// Its identification, assigned by its calibration laboratory (dcc:uniqueIdentifier).
        /// </summary>
        public DigitalCalibrationCertificate_Id           Id                          { get; }

        /// <summary>
        /// The country of its calibration laboratory, ISO 3166-1 alpha-2, e.g. DE (dcc:countryCodeISO3166_1).
        /// </summary>
        public String                                     CountryCode                 { get; }

        /// <summary>
        /// Its calibration laboratory, a conformity assessment body of the roaming network: its issuer.
        /// </summary>
        public ConformityAssessmentBody_Id                CalibrationLaboratoryId     { get; }

        /// <summary>
        /// The code of its calibration laboratory, e.g. its DAkkS accreditation "D-K-15071-01-00" (dcc:calibrationLaboratoryCode).
        /// </summary>
        public String?                                    CalibrationLaboratoryCode   { get; }

        /// <summary>
        /// Its customer, e.g. the charging station operator who ordered it (dcc:customer).
        /// </summary>
        public String?                                    Customer                    { get; }

        /// <summary>
        /// The order number of its customer (dcc:identification of refType basic_orderNo).
        /// </summary>
        public String?                                    OrderNumber                 { get; }

        /// <summary>
        /// The charging station or energy meter it calibrated (dcc:item).
        /// </summary>
        public QIDigitalItem                              Item                        { get; }

        /// <summary>
        /// When the calibration began (dcc:beginPerformanceDate).
        /// </summary>
        public DateTimeOffset                             BeginPerformanceDate        { get; }

        /// <summary>
        /// When the calibration ended (dcc:endPerformanceDate).
        /// </summary>
        public DateTimeOffset                             EndPerformanceDate          { get; }

        /// <summary>
        /// Where the calibration was performed (dcc:performanceLocation).
        /// </summary>
        public DCCPerformanceLocation                     PerformanceLocation         { get; }

        /// <summary>
        /// Its responsible persons, at least one, at most one of them its main signer (dcc:respPersons).
        /// </summary>
        public ImmutableArray<QIDigitalPerson>            RespPersons                 { get; }

        /// <summary>
        /// Its statements (dcc:statements).
        /// </summary>
        public ImmutableArray<DCCStatement>               Statements                  { get; }

        /// <summary>
        /// Its measurement results, at least one (dcc:measurementResults).
        /// </summary>
        public ImmutableArray<DCCMeasurementResult>       MeasurementResults          { get; }

        /// <summary>
        /// When its item is to be calibrated again, e.g. the end of its verification period under the German calibration law.
        /// </summary>
        public DateTimeOffset?                            RecalibrationDue            { get; }

        /// <summary>
        /// Its comment (dcc:comment).
        /// </summary>
        public ImmutableI18NString?                       Comment                     { get; }


        /// <inheritdoc/>
        public override String Context
            => JSONLDContext;

        /// <inheritdoc/>
        public override String Usage
            => POIKeyUsages.Certificates;

        /// <inheritdoc/>
        public override String IssuerText
            => $"the calibration laboratory '{CalibrationLaboratoryId}'";

        /// <inheritdoc/>
        public override String DocumentText
            => $"the digital calibration certificate '{Id}'";

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new digital calibration certificate; Sign(…) signs it.
        /// </summary>
        /// <param name="Id">Its identification, assigned by its calibration laboratory.</param>
        /// <param name="CalibrationLaboratoryId">Its calibration laboratory, a conformity assessment body.</param>
        /// <param name="Item">The charging station or energy meter it calibrated.</param>
        /// <param name="BeginPerformanceDate">When the calibration began.</param>
        /// <param name="EndPerformanceDate">When the calibration ended.</param>
        /// <param name="RespPersons">Its responsible persons, at least one.</param>
        /// <param name="MeasurementResults">Its measurement results, at least one.</param>
        /// <param name="CountryCode">The country of its calibration laboratory, ISO 3166-1 alpha-2.</param>
        /// <param name="PerformanceLocation">Where the calibration was performed.</param>
        /// <param name="CalibrationLaboratoryCode">The code of its calibration laboratory, e.g. its accreditation.</param>
        /// <param name="Customer">Its customer.</param>
        /// <param name="OrderNumber">The order number of its customer.</param>
        /// <param name="Statements">Its statements.</param>
        /// <param name="RecalibrationDue">When its item is to be calibrated again.</param>
        /// <param name="Comment">Its comment.</param>
        /// <param name="Signature">Its signature, if it is signed.</param>
        public DigitalCalibrationCertificate(DigitalCalibrationCertificate_Id      Id,
                                             ConformityAssessmentBody_Id           CalibrationLaboratoryId,
                                             QIDigitalItem                         Item,
                                             DateTimeOffset                        BeginPerformanceDate,
                                             DateTimeOffset                        EndPerformanceDate,
                                             IEnumerable<QIDigitalPerson>          RespPersons,
                                             IEnumerable<DCCMeasurementResult>     MeasurementResults,
                                             String                                CountryCode                 = "DE",
                                             DCCPerformanceLocation                PerformanceLocation         = DCCPerformanceLocation.Laboratory,
                                             String?                               CalibrationLaboratoryCode   = null,
                                             String?                               Customer                    = null,
                                             String?                               OrderNumber                 = null,
                                             IEnumerable<DCCStatement>?            Statements                  = null,
                                             DateTimeOffset?                       RecalibrationDue            = null,
                                             ImmutableI18NString?                  Comment                     = null,
                                             QIDigitalSignature?                   Signature                   = null)

            : base(Signature)

        {

            ArgumentNullException.ThrowIfNull(Item);

            if (Id.IsNullOrEmpty)
                throw new ArgumentException("A digital calibration certificate requires an identification!", nameof(Id));

            if (CountryCode is null || CountryCode.Length != 2 || !CountryCode.All(Char.IsAsciiLetterUpper))
                throw new ArgumentException($"The country of the digital calibration certificate '{Id}' must be two capital letters, ISO 3166-1 alpha-2!", nameof(CountryCode));

            if (EndPerformanceDate < BeginPerformanceDate)
                throw new ArgumentException($"The calibration of the digital calibration certificate '{Id}' must not end before it began!", nameof(EndPerformanceDate));

            if (RecalibrationDue.HasValue && RecalibrationDue.Value <= EndPerformanceDate)
                throw new ArgumentException($"The recalibration of the digital calibration certificate '{Id}' must be due after its calibration!", nameof(RecalibrationDue));

            var respPersons         = (RespPersons        ?? []).ToImmutableArray();
            var measurementResults  = (MeasurementResults ?? []).ToImmutableArray();

            if (respPersons.IsEmpty)
                throw new ArgumentException($"The digital calibration certificate '{Id}' requires at least one responsible person!", nameof(RespPersons));

            if (respPersons.Count(person => person.MainSigner) > 1)
                throw new ArgumentException($"The digital calibration certificate '{Id}' has at most one main signer!", nameof(RespPersons));

            if (measurementResults.IsEmpty)
                throw new ArgumentException($"The digital calibration certificate '{Id}' requires at least one measurement result!", nameof(MeasurementResults));

            this.Id                         = Id;
            this.CalibrationLaboratoryId    = CalibrationLaboratoryId;
            this.Item                       = Item;
            this.BeginPerformanceDate       = QIDigitalJSON.Timestamp(BeginPerformanceDate);
            this.EndPerformanceDate         = QIDigitalJSON.Timestamp(EndPerformanceDate);
            this.RespPersons                = respPersons;
            this.MeasurementResults         = measurementResults;
            this.CountryCode                = CountryCode;
            this.PerformanceLocation        = PerformanceLocation;
            this.CalibrationLaboratoryCode  = CalibrationLaboratoryCode;
            this.Customer                   = Customer;
            this.OrderNumber                = OrderNumber;
            this.Statements                 = (Statements ?? []).ToImmutableArray();
            this.RecalibrationDue           = QIDigitalJSON.Timestamp(RecalibrationDue);
            this.Comment                    = Comment;

        }

        #endregion


        #region MainSigner

        /// <summary>
        /// The responsible person signing it as its main signer, if it names one.
        /// </summary>
        public QIDigitalPerson? MainSigner
            => RespPersons.FirstOrDefault(person => person.MainSigner);

        #endregion

        #region Passes

        /// <summary>
        /// Whether every measured quantity stating its conformity passes, and at least one states it: whether its item
        /// is within its tolerance limits, e.g. the maximum permissible errors of its accuracy class.
        /// </summary>
        public Boolean Passes
        {
            get
            {

                var conformities = MeasurementResults.SelectMany(result => result.Quantities).
                                                      Where     (quantity => quantity.Conformity.HasValue).
                                                      Select    (quantity => quantity.Conformity!.Value).
                                                      ToArray();

                return conformities.Length > 0 &&
                       conformities.All(conformity => conformity == QIDigitalConformity.Pass);

            }
        }

        #endregion

        #region IsDueAt(Timestamp)

        /// <summary>
        /// Whether its item is due to be calibrated again at the given time.
        /// </summary>
        public Boolean IsDueAt(DateTimeOffset Timestamp)

            => RecalibrationDue.HasValue &&
               Timestamp >= RecalibrationDue.Value;

        #endregion

        #region TryVerify(RoamingNetwork, out ErrorResponse, At = null)

        /// <summary>
        /// Verify this certificate against the given roaming network: signed by a signing key of its calibration
        /// laboratory, a conformity assessment body of the network, that may sign certificates; and its item a charging
        /// station or an energy meter of the network as it calibrated it.
        /// </summary>
        /// <param name="RoamingNetwork">The roaming network.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="At">When the key must be valid as well, if anything.</param>
        public Boolean TryVerify(RoamingNetwork                    RoamingNetwork,
                                 [NotNullWhen(false)] out String?  ErrorResponse,
                                 DateTimeOffset?                   At   = null)
        {

            if (!RoamingNetwork.TryGetConformityAssessmentBodyById(CalibrationLaboratoryId, out var calibrationLaboratory))
            {
                ErrorResponse = $"The calibration laboratory '{CalibrationLaboratoryId}' of {DocumentText} is no conformity assessment body of the roaming network '{RoamingNetwork.Id}'!";
                return false;
            }

            return TryVerify(calibrationLaboratory.SigningKeys, out ErrorResponse, At) &&
                   Item.TryVerify(RoamingNetwork, out ErrorResponse);

        }

        #endregion


        #region (static) TryParse(Text, out Certificate, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON text of a digital calibration certificate, numbers with a fraction as decimals.
        /// </summary>
        public static Boolean TryParse(String                                                   Text,
                                       [NotNullWhen(true)]  out DigitalCalibrationCertificate?  Certificate,
                                       [NotNullWhen(false)] out String?                         ErrorResponse)
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
        /// Try to parse the given JSON representation of a digital calibration certificate. Its signature is not verified here.
        /// </summary>
        public static Boolean TryParse(JObject                                                  JSON,
                                       [NotNullWhen(true)]  out DigitalCalibrationCertificate?  Certificate,
                                       [NotNullWhen(false)] out String?                         ErrorResponse)
        {

            Certificate = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "a digital calibration certificate", out ErrorResponse,
                                           QIDigitalSignature.PropertyKeys.Context, PropertyKeys.Id, PropertyKeys.CountryCode,
                                           PropertyKeys.CalibrationLaboratoryId, PropertyKeys.CalibrationLaboratoryCode,
                                           PropertyKeys.Customer, PropertyKeys.OrderNumber, PropertyKeys.Item,
                                           PropertyKeys.BeginPerformanceDate, PropertyKeys.EndPerformanceDate, PropertyKeys.PerformanceLocation,
                                           PropertyKeys.RespPersons, PropertyKeys.Statements, PropertyKeys.MeasurementResults,
                                           PropertyKeys.RecalibrationDue, PropertyKeys.Comment, QIDigitalSignature.PropertyKeys.Signature) ||
                    !QIDigitalJSON.TryContext(JSON, JSONLDContext, "a digital calibration certificate", out ErrorResponse))
                {
                    return false;
                }

                if (!QIDigitalJSON.TryParseRequiredText(JSON, PropertyKeys.Id,                      "digital calibration certificate identification", DigitalCalibrationCertificate_Id.TryParse, out DigitalCalibrationCertificate_Id id,                      out ErrorResponse) ||
                    !QIDigitalJSON.TryParseRequiredText(JSON, PropertyKeys.CalibrationLaboratoryId, "calibration laboratory identification",          ConformityAssessmentBody_Id.     TryParse, out ConformityAssessmentBody_Id      calibrationLaboratoryId, out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseObject<QIDigitalItem>                (JSON, PropertyKeys.Item,                      "item",                         QIDigitalItem.       TryParse, out var item,                out ErrorResponse) ||
                    !POIJSON.TryParseObjects<QIDigitalPerson>             (JSON, PropertyKeys.RespPersons,               "responsible person",           QIDigitalPerson.     TryParse, out var respPersons,         out ErrorResponse) ||
                    !POIJSON.TryParseObjects<DCCStatement>                (JSON, PropertyKeys.Statements,                "statement",                    DCCStatement.        TryParse, out var statements,          out ErrorResponse) ||
                    !POIJSON.TryParseObjects<DCCMeasurementResult>        (JSON, PropertyKeys.MeasurementResults,        "measurement result",           DCCMeasurementResult.TryParse, out var measurementResults,  out ErrorResponse) ||
                    !POIJSON.TryParseEnum<DCCPerformanceLocation>         (JSON, PropertyKeys.PerformanceLocation,       "performance location",                                        out var performanceLocation, out ErrorResponse) ||
                    !POIJSON.TryParseString                               (JSON, PropertyKeys.CountryCode,               "country code",                                                out var countryCode,         out ErrorResponse) ||
                    !POIJSON.TryParseString                               (JSON, PropertyKeys.CalibrationLaboratoryCode, "calibration laboratory code",                                 out var laboratoryCode,      out ErrorResponse) ||
                    !POIJSON.TryParseString                               (JSON, PropertyKeys.Customer,                  "customer",                                                    out var customer,            out ErrorResponse) ||
                    !POIJSON.TryParseString                               (JSON, PropertyKeys.OrderNumber,               "order number",                                                out var orderNumber,         out ErrorResponse) ||
                    !QIDigitalJSON.TryParseI18N                           (JSON, PropertyKeys.Comment,                   "comment",                                                     out var comment,             out ErrorResponse) ||
                    !QIDigitalJSON.TryParseTimestamp                      (JSON, PropertyKeys.BeginPerformanceDate,      "beginning of the calibration",                                out var begin,               out ErrorResponse) ||
                    !QIDigitalJSON.TryParseTimestamp                      (JSON, PropertyKeys.EndPerformanceDate,        "end of the calibration",                                      out var end,                 out ErrorResponse) ||
                    !QIDigitalJSON.TryParseTimestamp                      (JSON, PropertyKeys.RecalibrationDue,          "recalibration due",                                           out var recalibrationDue,    out ErrorResponse) ||
                    !TryParseSignature                                    (JSON,                                                                                                        out var signature,           out ErrorResponse))
                {
                    return false;
                }

                if (item is null || !begin.HasValue || !end.HasValue || countryCode is null || !performanceLocation.HasValue)
                {
                    ErrorResponse = $"The digital calibration certificate '{id}' requires its item, its country, the beginning, the end and the location of its calibration!";
                    return false;
                }

                Certificate    = new DigitalCalibrationCertificate(id,
                                                                   calibrationLaboratoryId,
                                                                   item,
                                                                   begin.Value,
                                                                   end.Value,
                                                                   respPersons,
                                                                   measurementResults,
                                                                   countryCode,
                                                                   performanceLocation.Value,
                                                                   laboratoryCode,
                                                                   customer,
                                                                   orderNumber,
                                                                   statements,
                                                                   recalibrationDue,
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
                           new JProperty(PropertyKeys.Id,                         Id.ToString()),
                           new JProperty(PropertyKeys.CountryCode,                CountryCode),
                           new JProperty(PropertyKeys.CalibrationLaboratoryId,    CalibrationLaboratoryId.ToString())
                       );

            if (CalibrationLaboratoryCode is not null)
                json.Add(PropertyKeys.CalibrationLaboratoryCode,  CalibrationLaboratoryCode);

            if (Customer is not null)
                json.Add(PropertyKeys.Customer,                   Customer);

            if (OrderNumber is not null)
                json.Add(PropertyKeys.OrderNumber,                OrderNumber);

            json.Add(PropertyKeys.Item,                           Item.ToJSON());
            json.Add(PropertyKeys.BeginPerformanceDate,           BeginPerformanceDate.ToISO8601());
            json.Add(PropertyKeys.EndPerformanceDate,             EndPerformanceDate.  ToISO8601());
            json.Add(PropertyKeys.PerformanceLocation,            PerformanceLocation. ToString());
            json.Add(PropertyKeys.RespPersons,                    new JArray(RespPersons.Select(person => person.ToJSON())));

            if (Statements.Length > 0)
                json.Add(PropertyKeys.Statements,                 new JArray(Statements.Select(statement => statement.ToJSON())));

            json.Add(PropertyKeys.MeasurementResults,             new JArray(MeasurementResults.Select(result => result.ToJSON())));

            if (RecalibrationDue.HasValue)
                json.Add(PropertyKeys.RecalibrationDue,           RecalibrationDue.Value.ToISO8601());

            if (Comment.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Comment,                    Comment.ToJSON());

            return json;

        }

        #endregion

        #region (protected) WithSignature(Signature)

        /// <inheritdoc/>
        protected override DigitalCalibrationCertificate WithSignature(QIDigitalSignature Signature)

            => new (Id,
                    CalibrationLaboratoryId,
                    Item,
                    BeginPerformanceDate,
                    EndPerformanceDate,
                    RespPersons,
                    MeasurementResults,
                    CountryCode,
                    PerformanceLocation,
                    CalibrationLaboratoryCode,
                    Customer,
                    OrderNumber,
                    Statements,
                    RecalibrationDue,
                    Comment,
                    Signature);

        #endregion

    }

}
