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

    public partial class TransparencySoftwareStatus
    {

        #region (static) Parse   (JSON, TransparencySoftwareLookup, CertificateLookup, CustomTransparencySoftwareStatusParser = null)

        /// <summary>
        /// Parse the given JSON representation of a legal status of a transparency software release.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="TransparencySoftwareLookup">Looks up the referenced software release.</param>
        /// <param name="CertificateLookup">Looks up the referenced document.</param>
        /// <param name="CustomTransparencySoftwareStatusParser">An optional delegate to parse custom transparency software status JSON objects.</param>
        public static TransparencySoftwareStatus Parse(JObject                                                   JSON,
                                                       TransparencySoftwareLookupDelegate                        TransparencySoftwareLookup,
                                                       TransparencySoftwareCertificateLookupDelegate             CertificateLookup,
                                                       CustomJObjectParserDelegate<TransparencySoftwareStatus>?  CustomTransparencySoftwareStatusParser   = null)
        {

            if (TryParse(JSON,
                         TransparencySoftwareLookup,
                         CertificateLookup,
                         out var transparencySoftwareStatus,
                         out var errorResponse,
                         CustomTransparencySoftwareStatusParser))
            {
                return transparencySoftwareStatus;
            }

            throw new ArgumentException(errorResponse, nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, TransparencySoftwareLookup, CertificateLookup, out TransparencySoftwareStatus, out ErrorResponse, ...)

        /// <summary>
        /// Try to parse the given JSON representation of a legal status of a transparency software release.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="TransparencySoftwareLookup">Looks up the referenced software release.</param>
        /// <param name="CertificateLookup">Looks up the referenced document.</param>
        /// <param name="TransparencySoftwareStatus">The parsed legal status.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomTransparencySoftwareStatusParser">An optional delegate to parse custom transparency software status JSON objects.</param>
        public static Boolean TryParse(JObject                                                   JSON,
                                       TransparencySoftwareLookupDelegate                        TransparencySoftwareLookup,
                                       TransparencySoftwareCertificateLookupDelegate             CertificateLookup,
                                       [NotNullWhen(true)]  out TransparencySoftwareStatus?      TransparencySoftwareStatus,
                                       [NotNullWhen(false)] out String?                          ErrorResponse,
                                       CustomJObjectParserDelegate<TransparencySoftwareStatus>?  CustomTransparencySoftwareStatusParser   = null)
        {

            TransparencySoftwareStatus  = null;
            ErrorResponse               = null;

            try
            {

                ArgumentNullException.ThrowIfNull(JSON);
                ArgumentNullException.ThrowIfNull(TransparencySoftwareLookup);
                ArgumentNullException.ThrowIfNull(CertificateLookup);

                TransparencyJSON.OnlyProperties(JSON, "transparencySoftwareId", "legalStatus", "certificateId");

                #region Parse TransparencySoftwareId    [mandatory]

                var transparencySoftwareId  = TransparencySoftware_Id.Parse(TransparencyJSON.RequiredText(JSON, "transparencySoftwareId"));
                var transparencySoftware    = TransparencySoftwareLookup(transparencySoftwareId) ??
                                                  throw new ArgumentException($"transparencySoftwareId: unresolved software '{transparencySoftwareId}'.");

                #endregion

                #region Parse LegalStatus               [mandatory]

                if (!LegalStatus.TryParse(TransparencyJSON.RequiredText(JSON, "legalStatus"), out var legalStatus))
                    throw new ArgumentException("legalStatus: invalid legal status.");

                #endregion

                #region Parse CertificateId             [optional]

                var certificate = TransparencyJSON.Text(JSON, "certificateId") is { } certificateId
                                      ? CertificateLookup(TransparencySoftwareCertificate_Id.Parse(certificateId)) ??
                                            throw new ArgumentException($"certificateId: unresolved document '{certificateId}'.")
                                      : null;

                #endregion


                var transparencySoftwareStatus = new TransparencySoftwareStatus(
                                                     transparencySoftware,
                                                     legalStatus,
                                                     certificate
                                                 );

                TransparencySoftwareStatus = CustomTransparencySoftwareStatusParser is null
                                                 ? transparencySoftwareStatus
                                                 : CustomTransparencySoftwareStatusParser(JSON, transparencySoftwareStatus) ??
                                                   throw new ArgumentException("The custom software status parser returned null.");

                return true;

            }
            catch (Exception exception)
            {
                TransparencySoftwareStatus  = null;
                ErrorResponse               = $"TransparencySoftwareStatus: {exception.Message}";
                return false;
            }

        }

        #endregion

    }

}
