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

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// An element of a charging tariff, as in OCPI: its price components apply while its restrictions hold.
    /// An immutable value without an identification; a tariff's elements are changed as a whole.
    /// </summary>
    public sealed class ChargingTariffElement
    {

        #region Properties

        /// <summary>
        /// The price components, in their given order; at least one.
        /// </summary>
        public ImmutableArray<ChargingPriceComponent>  PriceComponents    { get; }

        /// <summary>
        /// When the element applies; always, unless given.
        /// </summary>
        public ChargingTariffRestrictions?             Restrictions       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging tariff element.
        /// </summary>
        public ChargingTariffElement(IEnumerable<ChargingPriceComponent>  PriceComponents,
                                     ChargingTariffRestrictions?          Restrictions   = null)
        {

            var priceComponents = (PriceComponents ?? []).ToArray();

            if (priceComponents.Length == 0)
                throw new ArgumentException("A charging tariff element needs at least one price component!", nameof(PriceComponents));

            foreach (var priceComponent in priceComponents)
                if ((priceComponent ?? throw new ArgumentException("Price components must not contain null entries!", nameof(PriceComponents))).Validate() is String error)
                    throw new ArgumentException(error, nameof(PriceComponents));

            this.PriceComponents  = [.. priceComponents];
            this.Restrictions     = Restrictions;

        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this charging tariff element.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("priceComponents",  new JArray(PriceComponents.Select(priceComponent => priceComponent.ToJSON())))
                       );

            if (Restrictions is not null)
                json.Add("restrictions", Restrictions.ToJSON());

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingTariffElement, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging tariff element.
        /// </summary>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out ChargingTariffElement?  ChargingTariffElement,
                                       [NotNullWhen(false)] out String?                 ErrorResponse)
        {

            ChargingTariffElement  = null;
            ErrorResponse          = null;

            if (JSON["priceComponents"] is not JArray priceComponentsJSON)
            {
                ErrorResponse = "A charging tariff element needs its price components as a JSON array!";
                return false;
            }

            var priceComponents = new List<ChargingPriceComponent>();

            foreach (var priceComponentJSON in priceComponentsJSON)
            {

                if (priceComponentJSON is not JObject priceComponentObject ||
                    !ChargingPriceComponent.TryParse(priceComponentObject, out var priceComponent, out ErrorResponse))
                {
                    ErrorResponse ??= "A price component must be a JSON object!";
                    return false;
                }

                priceComponents.Add(priceComponent);

            }

            if (priceComponents.Count == 0)
            {
                ErrorResponse = "A charging tariff element needs at least one price component!";
                return false;
            }

            ChargingTariffRestrictions? restrictions = null;

            if (JSON["restrictions"] is JToken restrictionsJSON &&
               (restrictionsJSON is not JObject restrictionsObject ||
                !ChargingTariffRestrictions.TryParse(restrictionsObject, out restrictions, out ErrorResponse)))
            {
                ErrorResponse ??= "The restrictions of a charging tariff element must be a JSON object!";
                return false;
            }

            ChargingTariffElement  = new ChargingTariffElement(priceComponents, restrictions);
            ErrorResponse          = null;
            return true;

        }

        #endregion

    }

}
