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
    /// The legal status of a transparency software release for an energy meter, optionally proven by a certificate:
    /// the JSON of a TransparencySoftwareStatus, naming the release and the certificate by their identifications.
    /// Both are embedded in the roaming network; the certificate must cover the release.
    /// </summary>
    /// <param name="TransparencySoftwareId">The transparency software release.</param>
    /// <param name="LegalStatus">Its legal status, e.g. GermanCalibrationLaw.</param>
    /// <param name="CertificateId">The certificate proving it.</param>
    public sealed record TransparencySoftwareAssignment(TransparencySoftware_Id              TransparencySoftwareId,
                                                        LegalStatus                          LegalStatus,
                                                        TransparencySoftwareCertificate_Id?  CertificateId   = null)
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/transparencySoftwareAssignment";

        #endregion

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context                  = "@context";

            /// <summary>
            /// The property of its transparency software release.
            /// </summary>
            public const String  TransparencySoftwareId   = "transparencySoftwareId";

            /// <summary>
            /// The property of its legal status.
            /// </summary>
            public const String  LegalStatus              = "legalStatus";

            /// <summary>
            /// The property of its certificate.
            /// </summary>
            public const String  CertificateId            = "certificateId";

        }

        #endregion


        #region ToJSON(Embedded = false, CustomTransparencySoftwareAssignmentSerializer = null)

        /// <summary>
        /// Return the JSON representation of this assignment.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomTransparencySoftwareAssignmentSerializer">A delegate to serialize custom assignment JSON objects.</param>
        public JObject ToJSON(Boolean                                                           Embedded                                         = false,
                              CustomJObjectSerializerDelegate<TransparencySoftwareAssignment>?  CustomTransparencySoftwareAssignmentSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,                 JSONLDContext);

            json.Add(PropertyKeys.TransparencySoftwareId,      TransparencySoftwareId.ToString());
            json.Add(PropertyKeys.LegalStatus,                 LegalStatus.           ToString());

            if (CertificateId.HasValue)
                json.Add(PropertyKeys.CertificateId,           CertificateId.Value.   ToString());

            return CustomTransparencySoftwareAssignmentSerializer is not null
                       ? CustomTransparencySoftwareAssignmentSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out TransparencySoftwareAssignment, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an assignment.
        /// </summary>
        public static Boolean TryParse(JObject                                                   JSON,
                                       [NotNullWhen(true)]  out TransparencySoftwareAssignment?  TransparencySoftwareAssignment,
                                       [NotNullWhen(false)] out String?                          ErrorResponse)
        {

            TransparencySoftwareAssignment = null;

            if (JSON[PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of the transparency software status must be '{JSONLDContext}'!";
                return false;
            }

            if (!POIJSON.TryParseText(JSON, PropertyKeys.TransparencySoftwareId, "transparency software identification", TransparencySoftware_Id.           TryParse, out TransparencySoftware_Id?            transparencySoftwareId, out ErrorResponse) ||
                !POIJSON.TryParseText(JSON, PropertyKeys.LegalStatus,            "legal status",                         POI.LegalStatus.                   TryParse, out LegalStatus?                        legalStatus,            out ErrorResponse) ||
                !POIJSON.TryParseText(JSON, PropertyKeys.CertificateId,          "certificate identification",           TransparencySoftwareCertificate_Id.TryParse, out TransparencySoftwareCertificate_Id? certificateId,          out ErrorResponse))
            {
                return false;
            }

            if (!transparencySoftwareId.HasValue || !legalStatus.HasValue)
            {
                ErrorResponse = $"A transparency software status needs its {PropertyKeys.TransparencySoftwareId} and its {PropertyKeys.LegalStatus}!";
                return false;
            }

            TransparencySoftwareAssignment  = new TransparencySoftwareAssignment(transparencySoftwareId.Value, legalStatus.Value, certificateId);
            ErrorResponse                   = null;
            return true;

        }

        #endregion

        #region (static) Checked(Assignments, Owner)

        /// <summary>
        /// The given assignments of one energy meter in the order of their release identifications (ordinal),
        /// at most one per release.
        /// </summary>
        internal static ImmutableArray<TransparencySoftwareAssignment> Checked(IEnumerable<TransparencySoftwareAssignment>?  Assignments,
                                                                                String                                        Owner)
        {

            var assignments = (Assignments ?? []).ToArray();

            if (assignments.Any(assignment => assignment is null))
                throw new ArgumentException($"The transparency software status of {Owner} must not contain null entries!");

            if (assignments.Select(assignment => assignment.TransparencySoftwareId).Distinct().Count() != assignments.Length)
                throw new ArgumentException($"A transparency software release of {Owner} must not be given twice!");

            return [.. assignments.OrderBy(assignment => assignment.TransparencySoftwareId.ToString(), StringComparer.Ordinal)];

        }

        #endregion

    }

}
