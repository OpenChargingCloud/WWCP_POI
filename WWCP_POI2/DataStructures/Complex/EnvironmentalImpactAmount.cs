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

using System.Globalization;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Extension methods for amounts of environmental impacts.
    /// </summary>
    public static class EnvironmentalImpactAmountExtensions
    {

        #region TryParse (JSON, PropertyKey, out Amounts, out ErrorResponse)

        /// <summary>
        /// Try to parse the mandatory array of [category, amount] pairs in the given property,
        /// e.g. "environmentalImpacts": [["CarbonDioxide", "350 g/kWh"]]. A category must not be given twice.
        /// </summary>
        /// <param name="JSON">The JSON object containing the array.</param>
        /// <param name="PropertyKey">The property key of the array.</param>
        /// <param name="Amounts">The parsed amounts, in their order.</param>
        /// <param name="ErrorResponse">An error response naming the path of the invalid element.</param>
        public static Boolean TryParse(JObject                                                        JSON,
                                       String                                                         PropertyKey,
                                       [NotNullWhen(true)]  out IEnumerable<EnvironmentalImpactAmount>  Amounts,
                                       [NotNullWhen(false)] out String?                               ErrorResponse)
        {

            Amounts = [];

            if (JSON?[PropertyKey] is not JArray jsonArray)
            {
                ErrorResponse = $"The '{PropertyKey}' property must be an array!";
                return false;
            }

            var amounts = new List<EnvironmentalImpactAmount>();

            for (var index = 0; index < jsonArray.Count; index++)
            {

                if (jsonArray[index] is not JArray pair)
                {
                    ErrorResponse = $"{PropertyKey}[{index}]: expected an array of the environmental impact and its amount!";
                    return false;
                }

                if (!EnvironmentalImpactAmount.TryParse(pair, out var amount, out var errorResponse))
                {
                    ErrorResponse = $"{PropertyKey}[{index}]{errorResponse}";
                    return false;
                }

                if (amounts.Any(existing => existing.Category == amount.Category))
                {
                    ErrorResponse = $"{PropertyKey}[{index}][0]: the environmental impact '{amount.Category}' must not be given twice!";
                    return false;
                }

                amounts.Add(amount);

            }

            Amounts        = amounts;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region ToJSON   (Amounts)

        /// <summary>
        /// The JSON representation of the given amounts of environmental impacts: [category, amount] pairs.
        /// </summary>
        public static JArray ToJSON(this IEnumerable<EnvironmentalImpactAmount> Amounts)

            => new (Amounts.Select(amount => amount.ToJSON()));

        #endregion

    }


    /// <summary>
    /// The amount of an environmental impact per kWh of the energy delivered, e.g. 350 g of carbon dioxide.
    /// </summary>
    /// <param name="Category">The environmental impact, e.g. CarbonDioxide or NuclearWaste.</param>
    /// <param name="GramsPerKWh">The amount in g/kWh, not negative.</param>
    public readonly struct EnvironmentalImpactAmount(EnvironmentalImpact  Category,
                                                     Decimal              GramsPerKWh)

        : IEquatable<EnvironmentalImpactAmount>

    {

        #region Data

        /// <summary>
        /// The unit of the amount in its text.
        /// </summary>
        public const String Unit = "g/kWh";

        #endregion

        #region Properties

        /// <summary>
        /// The environmental impact, e.g. CarbonDioxide or NuclearWaste.
        /// </summary>
        public EnvironmentalImpact  Category       { get; } = Category.IsNullOrEmpty
                                                                 ? throw new ArgumentException("An environmental impact needs a category!", nameof(Category))
                                                                 : Category;

        /// <summary>
        /// The amount in g/kWh, not negative.
        /// </summary>
        public Decimal              GramsPerKWh    { get; } = GramsPerKWh < 0
                                                                 ? throw new ArgumentException($"The amount of the environmental impact '{Category}' must not be negative!", nameof(GramsPerKWh))
                                                                 : GramsPerKWh;

        /// <summary>
        /// The amount with its unit, e.g. "350 g/kWh": culture invariant, without trailing zeros.
        /// </summary>
        public String               AmountText
            => GramsPerKWh.ToString("0.############################", CultureInfo.InvariantCulture) + " " + Unit;

        #endregion


        #region TryParse(JSON, out Amount, out ErrorResponse)

        /// <summary>
        /// Try to parse the given [category, amount] pair, e.g. ["CarbonDioxide", "350 g/kWh"].
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Amount">The parsed amount.</param>
        /// <param name="ErrorResponse">An error response, starting with the index of the invalid element, if any.</param>
        public static Boolean TryParse(JArray                            JSON,
                                       out EnvironmentalImpactAmount     Amount,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Amount = default;

            if (JSON is null || JSON.Count != 2)
            {
                ErrorResponse = $": expected an array of exactly two elements: the environmental impact and its amount, e.g. [\"CarbonDioxide\", \"350 {Unit}\"]!";
                return false;
            }

            if (JSON[0].Type != JTokenType.String ||
                !EnvironmentalImpact.TryParse(JSON[0].Value<String>()!, out var category))
            {
                ErrorResponse = "[0]: expected a valid environmental impact string!";
                return false;
            }

            var amountText = JSON[1].Type == JTokenType.String
                                 ? JSON[1].Value<String>()!.Trim()
                                 : null;

            if (amountText is null || !amountText.EndsWith(Unit, StringComparison.Ordinal) ||
                !Decimal.TryParse(amountText[..^Unit.Length].TrimEnd(), NumberStyles.Float, CultureInfo.InvariantCulture, out var gramsPerKWh))
            {
                ErrorResponse = $"[1]: expected an amount string with '{Unit}' unit, e.g. \"350 {Unit}\"!";
                return false;
            }

            if (gramsPerKWh < 0)
            {
                ErrorResponse = "[1]: expected an amount not below 0!";
                return false;
            }

            Amount         = new EnvironmentalImpactAmount(category, gramsPerKWh);
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// The JSON representation: [category, amount], e.g. ["CarbonDioxide", "350 g/kWh"].
        /// </summary>
        public JArray ToJSON()

            => new (Category.ToString(), AmountText);

        #endregion


        #region IEquatable<EnvironmentalImpactAmount> Members

        /// <inheritdoc/>
        public Boolean Equals(EnvironmentalImpactAmount Other)

            => Category    == Other.Category &&
               GramsPerKWh == Other.GramsPerKWh;

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is EnvironmentalImpactAmount other &&
               Equals(other);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => HashCode.Combine(Category, GramsPerKWh);

        /// <inheritdoc/>
        public override String ToString()

            => $"{Category}: {AmountText}";

        #endregion

    }

}
