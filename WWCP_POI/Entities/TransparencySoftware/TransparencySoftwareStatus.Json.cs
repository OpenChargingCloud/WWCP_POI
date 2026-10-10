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

        /// <summary>
        /// Parse software/document references and the legal status using this network version.
        /// </summary>
        private static Boolean TryParseDocument(JObject                                                   JSON,
                                                [NotNullWhen(true)]  out TransparencySoftwareStatus?      result,
                                                [NotNullWhen(false)] out String?                          error,
                                                CustomJObjectParserDelegate<TransparencySoftwareStatus>?  custom,
                                                RoamingNetwork?                                           network)
        {

            result = null;
            error  = null;

            try
            {
                ArgumentNullException.ThrowIfNull(JSON);

                InfrastructureJson.ValidateFields(JSON, "transparencySoftwareId", "legalStatus", "certificateId");
                var softwareId = TransparencySoftware_Id.Parse(TransparencyJson.RequiredText(JSON, "transparencySoftwareId"));
                var software = network?.GetTransparencySoftwareById(softwareId) ??
                               throw new ArgumentException($"transparencySoftwareId: unresolved software '{softwareId}'; supply its network registry.");
                var certificateId = InfrastructureJson.Text(JSON, "certificateId");
                var certificate = certificateId is null ? null : network?.GetTransparencySoftwareCertificateById(
                    TransparencySoftwareCertificate_Id.Parse(certificateId)) ??
                    throw new ArgumentException($"certificateId: unresolved document '{certificateId}'.");

                var state = TransparencyJson.RequiredText(JSON, "legalStatus");

                if (!LegalStatus.TryParse(state, out var legal))
                    throw new ArgumentException("legalStatus: invalid legal status.");

                var parsed = new TransparencySoftwareStatus(
                                 TransparencySoftware: software,
                                 LegalStatus: legal,
                                 Certificate: certificate
                             );

                result = custom is null
                             ? parsed
                             : custom(JSON, parsed) ??
                               throw new ArgumentException("The custom software status parser returned null.");

                return true;
            }
            catch (Exception exception)
            {
                result = null;
                error  = $"TransparencySoftwareStatus: {exception.Message}";
                return false;
            }

        }

    }

}
