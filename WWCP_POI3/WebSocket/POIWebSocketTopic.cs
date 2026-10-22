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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A topic of a POI WebSocket server, which clients subscribe to.
    /// </summary>
    /// <param name="Name">The name of the topic, e.g. commitAnnouncements.</param>
    /// <param name="Action">The action of its pushes, e.g. commitAnnouncement.</param>
    /// <param name="Description">What it pushes.</param>
    public sealed record POIWebSocketTopic(String  Name,
                                           String  Action,
                                           String  Description)
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/webSocketTopic/v1";

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
            public const String  Context      = "@context";

            /// <summary>
            /// The property of its name.
            /// </summary>
            public const String  Name         = "name";

            /// <summary>
            /// The property of the action of its pushes.
            /// </summary>
            public const String  Action       = "action";

            /// <summary>
            /// The property of what it pushes.
            /// </summary>
            public const String  Description  = "description";

        }

        #endregion

        /// <summary>
        /// Return a JSON representation of this topic.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomTopicSerializer">An optional custom serializer of topics.</param>
        public JObject ToJSON(Boolean                                              Embedded                = false,
                              CustomJObjectSerializerDelegate<POIWebSocketTopic>?  CustomTopicSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,      JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.Name,         Name),
                                 new JProperty(PropertyKeys.Action,       Action),
                                 new JProperty(PropertyKeys.Description,  Description)

                       );

            return CustomTopicSerializer is not null
                       ? CustomTopicSerializer(this, json)
                       : json;

        }

    }

}
