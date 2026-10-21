/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

#region Usings

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A precondition of a change set: the entity of the given type and identification still has the given ETag,
    /// the ETag of its representation as its roaming network embeds it, e.g. the HTTP ETag a client read it with.
    /// A change set applies, and is rebased, only while all its preconditions hold.
    /// </summary>
    /// <param name="EntityType">The entity type, e.g. ChargingPool.</param>
    /// <param name="EntityId">The identification of the entity, in any spelling its type considers equal.</param>
    /// <param name="ETag">The ETag the entity must have, in the format and with the hash algorithm of the ETag.</param>
    public sealed record RoamingNetworkPrecondition(String  EntityType,
                                                    String  EntityId,
                                                    ETag    ETag)
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetworkPrecondition";

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
            public const String  Context      = "@context";

            /// <summary>
            /// The property of its entity type.
            /// </summary>
            public const String  EntityType   = "entityType";

            /// <summary>
            /// The property of its entity identification.
            /// </summary>
            public const String  EntityId     = "entityId";

            /// <summary>
            /// The property of its ETag.
            /// </summary>
            public const String  ETag         = "eTag";

        }

        #endregion

        #endregion


        #region TryParse (JSON, out Precondition, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a precondition.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Precondition">The parsed precondition.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkPrecondition?   Precondition,
                                       [NotNullWhen(false)] out String?                       ErrorResponse)
        {

            Precondition = null;

            try
            {

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the precondition must be '{JSONLDContext}'!";
                    return false;
                }

                if (!JSON.ParseMandatoryText(PropertyKeys.EntityType, "entity type", out var entityType, out ErrorResponse) ||
                    !JSON.ParseMandatoryText(PropertyKeys.EntityId,   "entity identification", out var entityId, out ErrorResponse))
                {
                    return false;
                }

                if (JSON[PropertyKeys.ETag] is not JToken eTagJSON)
                {
                    ErrorResponse = "The ETag of a precondition is missing!";
                    return false;
                }

                Precondition   = new RoamingNetworkPrecondition(entityType, entityId, ETag.ParseHex(eTagJSON));
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                Precondition   = null;
                ErrorResponse  = "Invalid precondition: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false, CustomRoamingNetworkPreconditionSerializer = null)

        /// <summary>
        /// Return the JSON representation of this precondition: { "entityType", "entityId", "eTag" }.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomRoamingNetworkPreconditionSerializer">An optional custom serializer.</param>
        public JObject ToJSON(Boolean                                                       Embedded                                     = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkPrecondition>?  CustomRoamingNetworkPreconditionSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,     JSONLDContext);

            json.Add(PropertyKeys.EntityType,      EntityType);
            json.Add(PropertyKeys.EntityId,        EntityId);
            json.Add(PropertyKeys.ETag,            ETag.ToJSON());

            return CustomRoamingNetworkPreconditionSerializer is not null
                       ? CustomRoamingNetworkPreconditionSerializer(this, json)
                       : json;

        }

        #endregion

        #region ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{EntityType} '{EntityId}' has {ETag}";

        #endregion

    }

}
