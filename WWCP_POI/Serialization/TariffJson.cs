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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Scalar parsing shared by tariff price components and restrictions.
    /// </summary>
    internal static class TariffJson
    {

        internal static Boolean EnumValue<T>(String Text, out T Value)
            where T : struct, Enum

            => Enum.TryParse(Text, true, out Value) &&
               Enum.IsDefined(Value) &&
               String.Equals(Text, Value.ToString(), StringComparison.OrdinalIgnoreCase);

        internal static Decimal? Decimal(JObject JSON, String Field)
        {

            if (JSON[Field] is not { } token)
                return null;

            var text = token.Type == JTokenType.String
                           ? token.Value<String>()
                           : token.ToString(Formatting.None);

            if (token.Type is not (JTokenType.Float or JTokenType.Integer or JTokenType.String) ||
                !System.Decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                throw new ArgumentException($"{Field}: expected an invariant decimal number.");
            }

            return value;

        }

    }

}
