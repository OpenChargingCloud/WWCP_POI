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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public sealed partial class EMobilityProvider
    {

        /// <summary>
        /// The JSON-LD context of an e-mobility provider.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/eMobilityProvider";

        #region Parse/TryParse

        /// <summary>
        /// Parse a provider into the supplied roaming network without registering it.
        /// </summary>
        public static EMobilityProvider Parse(JObject                                          JSON,
                                              RoamingNetwork                                   RoamingNetwork,
                                              CustomJObjectParserDelegate<EMobilityProvider>?  CustomEMobilityProviderParser = null)
        {

            if (TryParse(JSON, RoamingNetwork, out var provider, out var error, CustomEMobilityProviderParser))
                return provider;

            throw new ArgumentException(error, nameof(JSON));

        }

        /// <summary>
        /// Try to reconstruct a provider's properties, licenses and current statuses.
        /// </summary>
        public static Boolean TryParse(JObject                                          JSON,
                                       RoamingNetwork                                   RoamingNetwork,
                                       [NotNullWhen(true)]  out EMobilityProvider?      provider,
                                       [NotNullWhen(false)] out String?                 error,
                                       CustomJObjectParserDelegate<EMobilityProvider>?  CustomEMobilityProviderParser = null)
        {

            provider = null;
            error    = null;

            try
            {
                ArgumentNullException.ThrowIfNull(RoamingNetwork);
                InfrastructureJson.Validate(JSON, JSONLDContext);

                var text = InfrastructureJson.Text(JSON, "@id");

                if (text is null || !EMobilityProvider_Id.TryParse(text, out var id))
                    throw new ArgumentException("@id: invalid or missing provider identifier.");

                InfrastructureJson.Parent(JSON, "roamingNetwork", RoamingNetwork.Id.ToString());

                var parsed = new EMobilityProvider(id,
                                                   RoamingNetwork,
                                                   Priority: JSON["priority"] is { } rank && rank.Type != JTokenType.Null
                                                                 ? rank.Type == JTokenType.Integer ? new EMobilityProviderPriority(rank.Value<Int32>())
                                                                     : throw new ArgumentException("priority: expected an integer.") : null,
                                                   Name:        InfrastructureJson.Name(JSON, "name"),
                                                   Description: InfrastructureJson.Name(JSON, "description"),
                                                   DataSource:  InfrastructureJson.Text(JSON, "dataSource"),
                                                   CustomData:  InfrastructureJson.CustomData(JSON))
                {
                    _Homepage           = InfrastructureJson.Scalar<URL>(JSON, "homepage", URL.TryParse),
                    _HotlinePhoneNumber = InfrastructureJson.Scalar<PhoneNumber>(JSON, "hotline", PhoneNumber.TryParse)
                };

                if (InfrastructureJson.Address(JSON) is { } address)
                    parsed._Address = address;

                parsed.ParseProviderLogo(JSON);

                foreach (var license in InfrastructureJson.Licenses(JSON))
                    parsed.dataLicenses.TryAdd(license.Id, license);

                InfrastructureJson.RestoreMetadata(JSON, parsed, EMobilityProviderAdminStatusTypes.TryParse, EMobilityProviderStatusTypes.TryParse);

                provider = CustomEMobilityProviderParser is null
                               ? parsed
                               : CustomEMobilityProviderParser(JSON, parsed) ??
                                 throw new ArgumentException("The custom provider parser returned null.");

                return true;
            }
            catch (Exception exception)
            {
                provider = null;
                error    = $"EMobilityProvider: {exception.Message}";
                return false;
            }

        }

        #endregion

        #region Parse provider logo

        private void ParseProviderLogo(JObject JSON)
        {

            var logos = InfrastructureJson.Array(JSON, "logos", token =>
                InfrastructureJson.Text(InfrastructureJson.Entry(token), "uri") ??
                throw new ArgumentException("Missing logo URI."));

            if (logos.Count > 1)
                throw new ArgumentException("logos: this provider supports one logo.");

            if (logos.Count == 1)
                _Logo = logos[0];

        }

        #endregion

    }

}
