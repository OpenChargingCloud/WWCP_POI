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

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/chargingPriceComponent";

        #endregion

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context    = "@context";

            /// <summary>
            /// The property of its type.
            /// </summary>
            public const String  Type       = "type";

            /// <summary>
            /// The property of its price.
            /// </summary>
            public const String  Price      = "price";

            /// <summary>
            /// The property of its VAT.
            /// </summary>
            public const String  VAT        = "vat";

            /// <summary>
            /// The property of its step size.
            /// </summary>
            public const String  StepSize   = "stepSize";

        }

        #endregion


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


        #region ToJSON(Embedded = false, CustomChargingPriceComponentSerializer = null)

        /// <summary>
        /// Return the JSON representation of this price component.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomChargingPriceComponentSerializer">A delegate to serialize custom price components.</param>
        public JObject ToJSON(Boolean                                                   Embedded                                 = false,
                              CustomJObjectSerializerDelegate<ChargingPriceComponent>?  CustomChargingPriceComponentSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,   JSONLDContext);

            json.Add(PropertyKeys.Type,          Type.ToString());
            json.Add(PropertyKeys.Price,         POIJSON.Number(Price));

            if (VAT.HasValue)
                json.Add(PropertyKeys.VAT,       POIJSON.Number(VAT.Value));

            if (StepSize.HasValue)
                json.Add(PropertyKeys.StepSize,  POIJSON.Number(StepSize.Value));

            return CustomChargingPriceComponentSerializer is not null
                       ? CustomChargingPriceComponentSerializer(this, json)
                       : json;

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

            if (JSON[PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of the price component must be '{JSONLDContext}'!";
                return false;
            }

            if (!POIJSON.TryParseEnum(JSON, PropertyKeys.Type, "price component type", out ChargingDimensionTypes? type, out ErrorResponse))
                return false;

            if (!type.HasValue)
            {
                ErrorResponse = "A price component needs its type!";
                return false;
            }

            if (!JSON.ParseMandatory(PropertyKeys.Price,
                                     "price",
                                     out Decimal price,
                                     out ErrorResponse))
            {
                return false;
            }

            if (JSON.ParseOptional(PropertyKeys.VAT,       "VAT",        out Decimal? vat,      out ErrorResponse))
                if (ErrorResponse is not null) return false;

            if (JSON.ParseOptional(PropertyKeys.StepSize,  "step size",  out Decimal? stepSize, out ErrorResponse))
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
