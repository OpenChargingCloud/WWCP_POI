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
        /// Try to parse a price component with a positive, explicitly typed billing step.
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

                if (type == ChargingDimensionTypes.FLAT && JSON["stepSize"] is not null)
                    throw new ArgumentException("stepSize: flat fees have no physical billing increment.");
                result = type switch
                {
                    ChargingDimensionTypes.FLAT => FlatRate(price),
                    ChargingDimensionTypes.ENERGY => Energy(price, MetrologyJson.Read<WattHour>(JSON, "stepSize", WattHour.TryParse) ??
                                                                    throw new ArgumentException("stepSize: missing energy increment.")),
                    ChargingDimensionTypes.MAX_CURRENT => MaximumCurrent(price, MetrologyJson.Read<Ampere>(JSON, "stepSize", Ampere.TryParse) ??
                                                                                throw new ArgumentException("stepSize: missing current increment.")),
                    ChargingDimensionTypes.MIN_CURRENT => MinimumCurrent(price, MetrologyJson.Read<Ampere>(JSON, "stepSize", Ampere.TryParse) ??
                                                                                throw new ArgumentException("stepSize: missing current increment.")),
                    ChargingDimensionTypes.TIME => ChargingTime(price, MetrologyJson.ReadDuration(JSON, "stepSize") ??
                                                                       throw new ArgumentException("stepSize: missing duration increment.")),
                    ChargingDimensionTypes.PARKING_TIME => ParkingTime(price, MetrologyJson.ReadDuration(JSON, "stepSize") ??
                                                                             throw new ArgumentException("stepSize: missing duration increment.")),
                    _ => throw new ArgumentException("type: unsupported tariff dimension.")
                };

                return true;
            }
            catch (Exception exception)
            {
                error = $"ChargingPriceComponent: {exception.Message}";
                return false;
            }

        }

        #endregion



    }

}
