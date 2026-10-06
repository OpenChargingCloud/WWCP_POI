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

using System.Text.Json;
using System.Text.Json.Serialization;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Distinguishes an omitted operation value from an explicit JSON null.
    /// Omitted values remain nullable null; a present null is a defined JsonElement.
    /// </summary>
    internal sealed class OptionalJsonElementConverter : JsonConverter<JsonElement?>
    {
        public override Boolean HandleNull => true;

        public override JsonElement? Read(ref Utf8JsonReader     reader,
                                          Type                   typeToConvert,
                                          JsonSerializerOptions  options)
        {

            using var document = JsonDocument.ParseValue(ref reader);

            return document.RootElement.Clone();

        }

        public override void Write(Utf8JsonWriter         writer,
                                   JsonElement?           value,
                                   JsonSerializerOptions  options)
        {

            if (value.HasValue)
            {
                value.Value.WriteTo(writer);
            }
            else
            {
                writer.WriteNullValue();
            }

        }
    }
}
