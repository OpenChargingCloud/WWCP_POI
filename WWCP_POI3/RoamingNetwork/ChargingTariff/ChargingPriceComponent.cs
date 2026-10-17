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
    /// What a price component of a charging tariff is billed for, as in OCPI.
    /// </summary>
    public enum ChargingDimensionTypes
    {

        /// <summary>
        /// The charged energy, per kWh; its step size in Wh.
        /// </summary>
        ENERGY,

        /// <summary>
        /// A flat fee, without a step size.
        /// </summary>
        FLAT,

        /// <summary>
        /// The maximum current, per A; its step size in A.
        /// </summary>
        MAX_CURRENT,

        /// <summary>
        /// The minimum current, per A; its step size in A.
        /// </summary>
        MIN_CURRENT,

        /// <summary>
        /// The time not charging, per hour; its step size in seconds.
        /// </summary>
        PARKING_TIME,

        /// <summary>
        /// The time charging, per hour; its step size in seconds.
        /// </summary>
        TIME

    }


    /// <summary>
    /// A price component of a charging tariff element: an immutable value without an identification.
    /// </summary>
    /// <param name="Type">What is billed.</param>
    /// <param name="Price">The price per unit (kWh, A or hour) or the flat fee, excluding VAT, in the currency of the tariff.</param>
    /// <param name="VAT">The VAT in percent, unless unknown.</param>
    /// <param name="StepSize">The billing increment in base units: Wh, A or seconds; none for a flat fee.</param>
    public sealed record ChargingPriceComponent(ChargingDimensionTypes  Type,
                                                Decimal                 Price,
                                                Decimal?                VAT        = null,
                                                Decimal?                StepSize   = null)
    {

        #region Validate()

        /// <summary>
        /// The error, when this price component is invalid.
        /// </summary>
        internal String? Validate()
        {

            if (Price < 0)
                return $"The price of a {Type} price component must not be negative!";

            if (VAT is < 0 or > 100)
                return $"The VAT of a {Type} price component must be between 0 and 100 percent!";

            if (Type == ChargingDimensionTypes.FLAT)
                return StepSize.HasValue
                           ? "A FLAT price component has no step size!"
                           : null;

            return StepSize is > 0
                       ? null
                       : $"A {Type} price component needs a positive step size!";

        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this price component.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("type",   Type.ToString()),
                           new JProperty("price",  POIJSON.Number(Price))
                       );

            if (VAT.HasValue)
                json.Add("vat",       POIJSON.Number(VAT.Value));

            if (StepSize.HasValue)
                json.Add("stepSize",  POIJSON.Number(StepSize.Value));

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingPriceComponent, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a price component.
        /// </summary>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out ChargingPriceComponent?  ChargingPriceComponent,
                                       [NotNullWhen(false)] out String?                  ErrorResponse)
        {

            ChargingPriceComponent = null;

            if (!POIJSON.TryParseEnum(JSON, "type", "price component type", out ChargingDimensionTypes? type, out ErrorResponse))
                return false;

            if (!type.HasValue)
            {
                ErrorResponse = "A price component needs its type!";
                return false;
            }

            if (!JSON.ParseMandatory("price",
                                     "price",
                                     out Decimal price,
                                     out ErrorResponse))
            {
                return false;
            }

            if (JSON.ParseOptional("vat",      "VAT",        out Decimal? vat,      out ErrorResponse))
                if (ErrorResponse is not null) return false;

            if (JSON.ParseOptional("stepSize", "step size",  out Decimal? stepSize, out ErrorResponse))
                if (ErrorResponse is not null) return false;

            var priceComponent = new ChargingPriceComponent(type.Value, price, vat, stepSize);

            if (priceComponent.Validate() is String error)
            {
                ErrorResponse = error;
                return false;
            }

            ChargingPriceComponent  = priceComponent;
            ErrorResponse           = null;
            return true;

        }

        #endregion

    }

}
