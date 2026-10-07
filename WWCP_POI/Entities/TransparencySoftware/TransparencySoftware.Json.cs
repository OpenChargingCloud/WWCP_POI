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

    public partial class TransparencySoftware
    {

        /// <summary>
        /// Parse software properties and a complete license object.
        /// </summary>
        private static Boolean TryParseDocument(JObject                                             JSON,
                                                [NotNullWhen(true)]  out TransparencySoftware?      result,
                                                [NotNullWhen(false)] out String?                    error,
                                                CustomJObjectParserDelegate<TransparencySoftware>?  custom)
        {

            result = null;
            error  = null;

            try
            {
                ArgumentNullException.ThrowIfNull(JSON);
                if (custom is null)
                    InfrastructureJson.ValidateFields(JSON, "name", "version", "openSourceLicense", "vendor", "logo",
                                                      "howToUse", "moreInformation", "sourceCodeRepository");

                var parsed = new TransparencySoftware(
                                 Name:                 TransparencyJson.RequiredText(JSON, "name"),
                                 Version:              TransparencyJson.RequiredText(JSON, "version"),
                                 OpenSourceLicense:    InfrastructureJson.At("openSourceLicense", () => TransparencyJson.License(JSON)),
                                 Vendor:               TransparencyJson.RequiredText(JSON, "vendor"),
                                 Logo:                 TransparencyJson.Link(JSON, "logo"),
                                 HowToUse:             TransparencyJson.Link(JSON, "howToUse"),
                                 MoreInformation:      TransparencyJson.Link(JSON, "moreInformation"),
                                 SourceCodeRepository: TransparencyJson.Link(JSON, "sourceCodeRepository")
                             );

                result = custom is null
                             ? parsed
                             : custom(JSON, parsed) ??
                               throw new ArgumentException("The custom software parser returned null.");

                return true;
            }
            catch (Exception exception)
            {
                result = null;
                error  = $"TransparencySoftware: {exception.Message}";
                return false;
            }

        }

    }

}
