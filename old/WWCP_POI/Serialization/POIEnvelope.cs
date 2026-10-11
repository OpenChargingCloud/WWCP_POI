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

using Newtonsoft.Json.Linq;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// The POI envelope of a document: its ETags and its content profile.
/// Values of WWCP_POI2 know no envelope, so it is checked and removed before they parse.
/// </summary>
internal static class POIEnvelope
{
    /// <summary>
    /// The document without its envelope; the document itself when it has none.
    /// </summary>
    internal static JObject Content(JObject json)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (json["ETags"] is null && json[POIContentProfile.PropertyName] is null)
            return json;
        if (json[POIContentProfile.PropertyName] is { } profile)
            POIContentProfile.Require(profile.Type == JTokenType.String ? profile.Value<String>() : null);
        var content = (JObject) json.DeepClone();
        content.Remove("ETags");
        content.Remove(POIContentProfile.PropertyName);
        return content;
    }
}
