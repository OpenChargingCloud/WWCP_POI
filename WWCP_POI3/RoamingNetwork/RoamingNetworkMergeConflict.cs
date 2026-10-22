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
    /// A conflict of two versions of a roaming network: what their common ancestor, the left and the right version
    /// have at one place. The owner resolves it with a change set on one of them, e.g. taking the value of the other.
    /// </summary>
    /// <param name="Kind">Why they conflict.</param>
    /// <param name="EntityType">The type of the entity.</param>
    /// <param name="EntityId">The identification of the entity.</param>
    /// <param name="PropertyName">The property, unless the entity conflicts as a whole.</param>
    /// <param name="Base">The value of the common ancestor; null when absent. Where several best common ancestors conflict,
    /// a conflict marker, { "@conflict": { base?, left?, right? } }, with what they have.</param>
    /// <param name="Left">The value of the left version; null when absent.</param>
    /// <param name="Right">The value of the right version; null when absent.</param>
    /// <param name="Description">A description.</param>
    public sealed record RoamingNetworkMergeConflict(RoamingNetworkMergeConflictKind  Kind,
                                                     String                           EntityType,
                                                     String                           EntityId,
                                                     String?                          PropertyName,
                                                     JToken?                          Base,
                                                     JToken?                          Left,
                                                     JToken?                          Right,
                                                     String                           Description)
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/mergeConflict/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context        = "@context";

            /// <summary>
            /// The property of why they conflict.
            /// </summary>
            public const String  Kind           = "kind";

            /// <summary>
            /// The property of its entity type.
            /// </summary>
            public const String  EntityType     = "entityType";

            /// <summary>
            /// The property of its entity identification.
            /// </summary>
            public const String  EntityId       = "entityId";

            /// <summary>
            /// The property of its description.
            /// </summary>
            public const String  Description    = "description";

            /// <summary>
            /// The property of its property name.
            /// </summary>
            public const String  PropertyName   = "propertyName";

            /// <summary>
            /// The property of the value of the common ancestor.
            /// </summary>
            public const String  Base           = "base";

            /// <summary>
            /// The property of the value of the left version.
            /// </summary>
            public const String  Left           = "left";

            /// <summary>
            /// The property of the value of the right version.
            /// </summary>
            public const String  Right          = "right";

        }

        #endregion

        #endregion


        #region ToJSON(Embedded = false, CustomRoamingNetworkMergeConflictSerializer = null)

        /// <summary>
        /// Return the JSON representation of this conflict.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomRoamingNetworkMergeConflictSerializer">An optional custom serializer.</param>
        public JObject ToJSON(Boolean                                                        Embedded                                      = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkMergeConflict>?  CustomRoamingNetworkMergeConflictSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,       JSONLDContext);

            json.Add(PropertyKeys.Kind,              Kind.ToString());
            json.Add(PropertyKeys.EntityType,        EntityType);
            json.Add(PropertyKeys.EntityId,          EntityId);
            json.Add(PropertyKeys.Description,       Description);

            if (PropertyName is not null)
                json.Add(PropertyKeys.PropertyName,  PropertyName);

            if (Base  is not null)
                json.Add(PropertyKeys.Base,          Base. DeepClone());

            if (Left  is not null)
                json.Add(PropertyKeys.Left,          Left. DeepClone());

            if (Right is not null)
                json.Add(PropertyKeys.Right,         Right.DeepClone());

            return CustomRoamingNetworkMergeConflictSerializer is not null
                       ? CustomRoamingNetworkMergeConflictSerializer(this, json)
                       : json;

        }

        #endregion


    }

}
