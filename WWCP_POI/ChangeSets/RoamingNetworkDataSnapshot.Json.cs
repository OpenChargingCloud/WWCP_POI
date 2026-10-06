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

using System.Text;
using System.Text.Json;

using Newtonsoft.Json.Linq;

using EntityMap = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, cloud.charging.open.protocols.WWCP.POI.InfrastructureEntitySnapshot>;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public sealed partial class RoamingNetworkDataSnapshot
    {

        #region Export entity documents

        /// <summary>
        /// Export the entity's own properties, excluding parent and child links.
        /// </summary>
        internal static JObject OwnJSON(InfrastructureEntitySnapshot entity)
        {

            var json = new JObject();

            foreach (var property in entity.Properties)
                json[property.Key] = ReadToken(property.Value.GetRawText());

            return json;

        }

        private JsonElement Json(InfrastructureEntityKey  key,
                                 EntityMap                map)

            => JsonDocumentValue(new RoamingNetworkDataSnapshot(Root, map, Revision, AppliedChangeSetId).Export(key));

        private String Export(InfrastructureEntityKey key)
        {

            using var stream = new MemoryStream();

            using (var writer = new Utf8JsonWriter(stream))
                WriteNode(writer, key);

            return Encoding.UTF8.GetString(stream.ToArray());

        }

        #endregion

        #region Write nested JSON directly

        private void WriteNode(Utf8JsonWriter           writer,
                               InfrastructureEntityKey  key)
        {

            var entity = Entities[key];

            writer.WriteStartObject();

            foreach (var property in entity.Properties.OrderBy(property => property.Key, StringComparer.Ordinal))
            {
                writer.WritePropertyName(property.Key);
                property.Value.WriteTo(writer);
            }

            WriteChildren(writer, entity);

            if (key == Root)
                WriteRevisionMetadata(writer);

            writer.WriteEndObject();

        }

        private void WriteChildren(Utf8JsonWriter                writer,
                                   InfrastructureEntitySnapshot  entity)
        {

            foreach (var group in entity.Children.GroupBy(child => child.Type).OrderBy(group => group.Key))
            {
                writer.WritePropertyName(InfrastructureChangeSchema.Relations[group.Key].Field);
                writer.WriteStartArray();

                foreach (var child in group.OrderBy(child => child.Id, StringComparer.Ordinal))
                    WriteNode(writer, child);

                writer.WriteEndArray();
            }

        }

        private void WriteRevisionMetadata(Utf8JsonWriter writer)
        {

            writer.WriteNumber("revision", Revision);

            if (AppliedChangeSetId is not null)
                writer.WriteString("appliedChangeSetId", AppliedChangeSetId);

        }

        #endregion

        #region Read owned JSON values

        private static JsonElement JsonDocumentValue(String json)
        {

            using var document = JsonDocument.Parse(json);

            return document.RootElement.Clone();

        }

        internal static JObject ReadJSON(String json)

            => (JObject) ReadToken(json);

        private static JToken ReadToken(String json)

            => InfrastructureJson.ReadToken(json);

        #endregion

    }

}
