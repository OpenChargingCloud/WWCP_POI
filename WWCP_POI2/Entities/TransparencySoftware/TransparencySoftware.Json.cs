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
        /// Parse the software properties and its complete license object.
        /// Unknown properties are errors, unless a custom parser handles them.
        /// </summary>
        private static Boolean TryParseDocument(JObject                                             JSON,
                                                [NotNullWhen(true)]  out TransparencySoftware?      TransparencySoftware,
                                                [NotNullWhen(false)] out String?                    ErrorResponse,
                                                CustomJObjectParserDelegate<TransparencySoftware>?  CustomTransparencySoftwareParser)
        {

            TransparencySoftware  = null;
            ErrorResponse         = null;

            try
            {

                ArgumentNullException.ThrowIfNull(JSON);

                if (CustomTransparencySoftwareParser is null)
                    TransparencyJSON.OnlyProperties(JSON, "@id", "name", "version", "openSourceLicenses", "vendor",
                                                          "logo", "howToUse", "moreInformation", "sourceCodeRepository");

                var transparencySoftware = new TransparencySoftware(
                                               Id:                    TransparencySoftware_Id.Parse(TransparencyJSON.RequiredText(JSON, "@id")),
                                               Name:                  TransparencyJSON.RequiredName(JSON, "name"),
                                               Version:               TransparencySoftwareVersion.Parse(TransparencyJSON.RequiredText(JSON, "version")),
                                               OpenSourceLicenses:    TransparencyJSON.Licenses(JSON, "openSourceLicenses"),
                                               Vendor:                TransparencyJSON.RequiredText(JSON, "vendor"),
                                               Logo:                  TransparencyJSON.Link(JSON, "logo"),
                                               HowToUse:              TransparencyJSON.Link(JSON, "howToUse"),
                                               MoreInformation:       TransparencyJSON.Link(JSON, "moreInformation"),
                                               SourceCodeRepository:  TransparencyJSON.Link(JSON, "sourceCodeRepository")
                                           );

                TransparencySoftware = CustomTransparencySoftwareParser is null
                                           ? transparencySoftware
                                           : CustomTransparencySoftwareParser(JSON, transparencySoftware) ??
                                             throw new ArgumentException("The custom software parser returned null.");

                return true;

            }
            catch (Exception exception)
            {
                TransparencySoftware  = null;
                ErrorResponse         = $"TransparencySoftware: {exception.Message}";
                return false;
            }

        }

    }

}
