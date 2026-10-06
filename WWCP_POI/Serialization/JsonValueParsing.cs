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

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Reads scalar JSON values without converting JSON numbers through the current culture.
    /// </summary>
    internal static class JsonValueParsing
    {

        internal delegate Boolean ScalarParser<T>(String  text,
                                                  out T   value)
            where T : struct;

        internal static Boolean TryReadOptional<T>(JObject          json,
                                                   String           name,
                                                   ScalarParser<T>  parser,
                                                   out T?           value,
                                                   out String?      error)
            where T : struct
        {

            value = null;
            error = null;

            var token = json[name];

            if (token is null || token.Type == JTokenType.Null)
                return true;

            var text = token.Type switch
            {
                JTokenType.String                    => token.Value<String>(),
                JTokenType.Integer or JTokenType.Float => Convert.ToString(((JValue) token).Value, CultureInfo.InvariantCulture),
                _                                    => null
            };

            if (text is not null && parser(text, out var parsed))
            {
                value = parsed;
                return true;
            }

            error = $"Invalid '{name}': expected a valid scalar value.";

            return false;

        }

    }

}
