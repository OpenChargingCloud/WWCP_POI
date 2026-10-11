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

    /// <summary>
    /// Transparency software, its documents and its legal status within a roaming network:
    /// references resolve against the network's registries, and documents may carry
    /// the POI envelope (ETags, content profile) of the network they were exported from.
    /// </summary>
    public static class TransparencySoftwareExtensions
    {

        #region TransparencySoftwareCertificate

        extension(TransparencySoftwareCertificate)
        {

            /// <summary>
            /// Parse an approval or compatibility document and resolve its software
            /// and manufacturer references in the given roaming network.
            /// </summary>
            /// <param name="JSON">The JSON to parse.</param>
            /// <param name="RoamingNetwork">The roaming network with the referenced registries.</param>
            public static TransparencySoftwareCertificate Parse(JObject         JSON,
                                                                RoamingNetwork  RoamingNetwork)
            {

                ArgumentNullException.ThrowIfNull(RoamingNetwork);

                var certificate = TransparencySoftwareCertificate.Parse(POIEnvelope.Content(JSON));

                foreach (var transparencySoftwareId in certificate.VerifiedTransparencySoftwareIds.Concat(certificate.CompatibleTransparencySoftwareIds))
                    if (RoamingNetwork.GetTransparencySoftwareById(transparencySoftwareId) is null)
                        throw new ArgumentException($"Unresolved software reference '{transparencySoftwareId}'.");

                if (certificate.ChargingStationManufacturerId is { } manufacturerId &&
                    RoamingNetwork.GetChargingStationManufacturerById(manufacturerId) is null)
                    throw new ArgumentException($"Unresolved manufacturer reference '{manufacturerId}'.");

                return certificate;

            }

            /// <summary>
            /// Try to parse an approval or compatibility document and resolve its software
            /// and manufacturer references in the given roaming network.
            /// </summary>
            /// <param name="JSON">The JSON to parse.</param>
            /// <param name="RoamingNetwork">The roaming network with the referenced registries.</param>
            /// <param name="Certificate">The parsed document.</param>
            /// <param name="ErrorResponse">An optional error response.</param>
            public static Boolean TryParse(JObject                                                   JSON,
                                           RoamingNetwork                                            RoamingNetwork,
                                           [NotNullWhen(true)]  out TransparencySoftwareCertificate?  Certificate,
                                           [NotNullWhen(false)] out String?                           ErrorResponse)
            {
                try
                {
                    Certificate    = TransparencySoftwareCertificate.Parse(JSON, RoamingNetwork);
                    ErrorResponse  = null;
                    return true;
                }
                catch (Exception exception)
                {
                    Certificate    = null;
                    ErrorResponse  = exception.Message;
                    return false;
                }
            }

        }

        #endregion

        #region TransparencySoftwareStatus

        extension(TransparencySoftwareStatus)
        {

            /// <summary>
            /// Parse a legal status of a transparency software release and resolve
            /// its software release and document in the given roaming network.
            /// </summary>
            /// <param name="JSON">The JSON to parse.</param>
            /// <param name="CustomTransparencySoftwareStatusParser">An optional delegate to parse custom transparency software status JSON objects.</param>
            /// <param name="Network">The roaming network with the referenced registries.</param>
            public static TransparencySoftwareStatus Parse(JObject                                                   JSON,
                                                           CustomJObjectParserDelegate<TransparencySoftwareStatus>?  CustomTransparencySoftwareStatusParser   = null,
                                                           RoamingNetwork?                                           Network                                  = null)

                => TransparencySoftwareStatus.Parse(POIEnvelope.Content(JSON),
                                                    id => Network?.GetTransparencySoftwareById(id),
                                                    id => Network?.GetTransparencySoftwareCertificateById(id),
                                                    CustomTransparencySoftwareStatusParser);

            /// <summary>
            /// Try to parse a legal status of a transparency software release and resolve
            /// its software release and document in the given roaming network.
            /// </summary>
            /// <param name="JSON">The JSON to parse.</param>
            /// <param name="TransparencySoftwareStatus">The parsed legal status.</param>
            /// <param name="ErrorResponse">An optional error response.</param>
            /// <param name="CustomTransparencySoftwareStatusParser">An optional delegate to parse custom transparency software status JSON objects.</param>
            /// <param name="Network">The roaming network with the referenced registries.</param>
            public static Boolean TryParse(JObject                                                   JSON,
                                           [NotNullWhen(true)]  out TransparencySoftwareStatus?      TransparencySoftwareStatus,
                                           [NotNullWhen(false)] out String?                          ErrorResponse,
                                           CustomJObjectParserDelegate<TransparencySoftwareStatus>?  CustomTransparencySoftwareStatusParser   = null,
                                           RoamingNetwork?                                           Network                                  = null)
            {

                JObject content;

                try
                {
                    content = POIEnvelope.Content(JSON);
                }
                catch (Exception exception)
                {
                    TransparencySoftwareStatus  = null;
                    ErrorResponse               = $"TransparencySoftwareStatus: {exception.Message}";
                    return false;
                }

                return POI.TransparencySoftwareStatus.TryParse(content,
                                                               id => Network?.GetTransparencySoftwareById(id),
                                                               id => Network?.GetTransparencySoftwareCertificateById(id),
                                                               out TransparencySoftwareStatus,
                                                               out ErrorResponse,
                                                               CustomTransparencySoftwareStatusParser);

            }

        }

        extension(TransparencySoftwareStatus TransparencySoftwareStatus)
        {

            /// <summary>
            /// Copy this legal status, resolving its software release and document
            /// in the given roaming network, when supplied.
            /// </summary>
            /// <param name="Network">An optional roaming network with the referenced registries.</param>
            public TransparencySoftwareStatus Clone(RoamingNetwork? Network = null)

                => Network is null
                       ? TransparencySoftwareStatus
                       : TransparencySoftwareStatus.Clone(Network.GetTransparencySoftwareById,
                                                          Network.GetTransparencySoftwareCertificateById);

        }

        #endregion

    }

}
