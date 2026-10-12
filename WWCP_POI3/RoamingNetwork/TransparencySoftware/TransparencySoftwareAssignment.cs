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

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this assignment.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("transparencySoftwareId",  TransparencySoftwareId.ToString()),
                           new JProperty("legalStatus",             LegalStatus.           ToString())
                       );

            if (CertificateId.HasValue)
                json.Add("certificateId", CertificateId.Value.ToString());

            return json;

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

            if (!POIJSON.TryParseText(JSON, "transparencySoftwareId", "transparency software identification", TransparencySoftware_Id.           TryParse, out TransparencySoftware_Id?            transparencySoftwareId, out ErrorResponse) ||
                !POIJSON.TryParseText(JSON, "legalStatus",            "legal status",                         POI.LegalStatus.                   TryParse, out LegalStatus?                        legalStatus,            out ErrorResponse) ||
                !POIJSON.TryParseText(JSON, "certificateId",          "certificate identification",           TransparencySoftwareCertificate_Id.TryParse, out TransparencySoftwareCertificate_Id? certificateId,          out ErrorResponse))
            {
                return false;
            }

            if (!transparencySoftwareId.HasValue || !legalStatus.HasValue)
            {
                ErrorResponse = "A transparency software status needs its transparencySoftwareId and its legalStatus!";
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
