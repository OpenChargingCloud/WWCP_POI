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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public readonly partial struct ChargingTariffElement
    {

        #region Parse/TryParse

        /// <summary>
        /// Parse tariff element JSON text, retaining decimal precision.
        /// </summary>
        public static ChargingTariffElement Parse(String JSON)
            => Parse(InfrastructureJson.ReadObject(JSON));

        /// <summary>
        /// Parse the price components and optional restrictions of a tariff element.
        /// </summary>
        public static ChargingTariffElement Parse(JObject JSON)
        {

            if (TryParse(JSON, out var result, out var error))
                return result;

            throw new ArgumentException(error, nameof(JSON));

        }

        /// <summary>
        /// Try to parse a tariff element containing at least one valid price component.
        /// </summary>
        public static Boolean TryParse(JObject                           JSON,
                                       out ChargingTariffElement         result,
                                       [NotNullWhen(false)] out String?  error)
        {

            result = default;
            error  = null;

            try
            {
                InfrastructureJson.ValidateFields(JSON, "priceComponents", "restrictions");

                var components = InfrastructureJson.Array(JSON, "priceComponents",
                                                          token => ChargingPriceComponent.Parse(InfrastructureJson.Entry(token)));

                var restrictions = InfrastructureJson.Array(JSON, "restrictions",
                                                            token => ChargingTariffRestriction.Parse(InfrastructureJson.Entry(token)));

                result = new ChargingTariffElement(components, restrictions);

                return true;
            }
            catch (Exception exception)
            {
                error = $"ChargingTariffElement: {exception.Message}";
                return false;
            }

        }

        #endregion

    }

}
