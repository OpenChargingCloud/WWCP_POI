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

    public readonly partial struct ChargingPriceComponent
    {

        #region Parse/TryParse

        /// <summary>
        /// Parse a price component from JSON text, retaining decimal precision.
        /// </summary>
        public static ChargingPriceComponent Parse(String JSON)
            => Parse(InfrastructureJson.ReadObject(JSON));

        /// <summary>
        /// Parse a tariff dimension, its price and billing step.
        /// </summary>
        public static ChargingPriceComponent Parse(JObject JSON)
        {

            if (TryParse(JSON, out var result, out var error))
                return result;

            throw new ArgumentException(error, nameof(JSON));

        }

        /// <summary>
        /// Try to parse a price component with a positive integral billing step.
        /// </summary>
        public static Boolean TryParse(JObject                           JSON,
                                       out ChargingPriceComponent        result,
                                       [NotNullWhen(false)] out String?  error)
        {

            result = default;
            error  = null;

            try
            {
                InfrastructureJson.ValidateFields(JSON, "type", "price", "stepSize");

                var dimension = InfrastructureJson.Text(JSON, "type") ?? "";

                if (!TariffJson.EnumValue(dimension, out ChargingDimensionTypes type))
                    throw new ArgumentException("type: invalid or missing tariff dimension.");

                var price = TariffJson.Decimal(JSON, "price") ??
                            throw new ArgumentException("price: missing price.");

                var step = JSON["stepSize"];

                if (step is null || step.Type != JTokenType.Integer || !UInt32.TryParse(step.ToString(), out var size))
                    throw new ArgumentException("stepSize: expected an unsigned integer.");

                result = new ChargingPriceComponent(type, price, size);

                return true;
            }
            catch (Exception exception)
            {
                error = $"ChargingPriceComponent: {exception.Message}";
                return false;
            }

        }

        #endregion

        #region Validate billing increment

        private static UInt32 BillingSeconds(TimeSpan Increment)
        {

            if (Increment.Ticks <= 0 ||
                Increment.Ticks % TimeSpan.TicksPerSecond != 0 ||
                Increment.TotalSeconds > UInt32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("increment",
                                                      "Billing increments must be positive whole seconds within UInt32 range.");
            }

            return (UInt32) Increment.TotalSeconds;

        }

        #endregion

    }

}
