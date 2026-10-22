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
    /// The last change an effective member has at least, beyond what it and its groups tell: the floor it got when it
    /// became other than in an earlier version without a change of its own, e.g. when it left a group, so that its last
    /// change never goes back. Not content, but part of a version as it is calculated from its history: a snapshot keeps
    /// it, so that a history loaded again, or started from the snapshot, tells the same last changes.
    /// </summary>
    /// <param name="EntityType">The type of the member: EVSE, ChargingStation, ChargingPool or ParkingSpace.</param>
    /// <param name="EntityId">The identification of the member.</param>
    /// <param name="LastChange">The last change it has at least.</param>
    public sealed record POIEffectiveLastChange(String          EntityType,
                                                String          EntityId,
                                                DateTimeOffset  LastChange)
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/effectiveLastChange/v1";

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
            public const String  Context     = "@context";

            /// <summary>
            /// The property of the type of its member.
            /// </summary>
            public const String  EntityType  = "entityType";

            /// <summary>
            /// The property of the identification of its member.
            /// </summary>
            public const String  EntityId    = "entityId";

            /// <summary>
            /// The property of the last change its member has at least.
            /// </summary>
            public const String  LastChange  = "lastChange";

        }

        #endregion

        #region ToJSON(Embedded = false, CustomEffectiveLastChangeSerializer = null)

        /// <summary>
        /// Return the JSON representation of this floor.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomEffectiveLastChangeSerializer">An optional custom serializer of effective last changes.</param>
        public JObject ToJSON(Boolean                                                   Embedded                              = false,
                              CustomJObjectSerializerDelegate<POIEffectiveLastChange>?  CustomEffectiveLastChangeSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,     JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.EntityType,  EntityType),
                                 new JProperty(PropertyKeys.EntityId,    EntityId),
                                 new JProperty(PropertyKeys.LastChange,  LastChange.ToISO8601())

                       );

            return CustomEffectiveLastChangeSerializer is not null
                       ? CustomEffectiveLastChangeSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out EffectiveLastChange, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a floor.
        /// </summary>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out POIEffectiveLastChange?  EffectiveLastChange,
                                       [NotNullWhen(false)] out String?                  ErrorResponse)
        {

            EffectiveLastChange = null;

            if (JSON[PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of the effective last change must be '{JSONLDContext}'!";
                return false;
            }

            if (!POIJSON.TryParseString(JSON, PropertyKeys.EntityType, "entity type",        out var entityType, out ErrorResponse) ||
                !POIJSON.TryParseString(JSON, PropertyKeys.EntityId,   "entity identification", out var entityId,  out ErrorResponse))
            {
                return false;
            }

            if (entityType is null || entityId is null)
            {
                ErrorResponse = "An effective last change needs its entity type and its entity identification!";
                return false;
            }

            if (!JSON.ParseMandatory(PropertyKeys.LastChange, "last change", out DateTimeOffset lastChange, out ErrorResponse))
                return false;

            EffectiveLastChange  = new POIEffectiveLastChange(entityType, entityId, lastChange);
            ErrorResponse        = null;
            return true;

        }

        #endregion

    }

}
