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
    /// One immutable ownership step from a graph entity to a nested POI value or collection element.
    /// Property names are schema fields; collection elements use their existing domain identifiers.
    /// </summary>
    public sealed record POIElementPathSegment
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/elementPathSegment/v1";

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
            /// The property of its schema property name.
            /// </summary>
            public const String  PropertyName   = "propertyName";

            /// <summary>
            /// The property of its element identification.
            /// </summary>
            public const String  ElementId      = "elementId";

        }

        #endregion


        /// <summary>
        /// The exact schema property name on the current owner.
        /// </summary>
        public String   PropertyName    { get; }

        /// <summary>
        /// The existing element identity, mandatory for arrays and optional for identified singletons.
        /// </summary>
        public String?  ElementId       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a property step with an optional element identity. Arrays require an identity.
        /// Singleton values can use their ownership slot, optionally asserting their existing identity.
        /// </summary>
        /// <param name="PropertyName">The exact schema property name on the current owner.</param>
        /// <param name="ElementId">The existing element identity.</param>
        public POIElementPathSegment(String   PropertyName,
                                     String?  ElementId   = null)
        {

            if (Refusal(PropertyName, ElementId) is String refusal)
                throw new ArgumentException(refusal);

            this.PropertyName  = PropertyName;
            this.ElementId     = ElementId;

        }

        #endregion


        #region (private static) Refusal(PropertyName, ElementId)

        /// <summary>
        /// Why the given parts make no element path segment, or null: the constructor throws it, TryParse returns it.
        /// </summary>
        private static String? Refusal(String?  PropertyName,
                                       String?  ElementId)

            => String.IsNullOrWhiteSpace(PropertyName)                         ? "a schema property name is required!"
             : ElementId is not null && String.IsNullOrWhiteSpace(ElementId)   ? "an element identifier must not be empty!"
             : null;

        #endregion


        #region TryParse (JSON, out POIElementPathSegment, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an element path segment.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="POIElementPathSegment">The parsed element path segment.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out POIElementPathSegment?      POIElementPathSegment,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            POIElementPathSegment = null;

            try
            {

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"the JSON-LD context of the element path segment must be '{JSONLDContext}'!";
                    return false;
                }

                #region Parse PropertyName    [mandatory]

                // Trimmed, as ParseMandatoryText reads it; missing or anything but a text as empty.
                var propertyName = JSON[PropertyKeys.PropertyName] is JToken { Type: JTokenType.String } propertyNameJSON
                                       ? propertyNameJSON.Value<String>()!.Trim()
                                       : null;

                #endregion

                #region Parse ElementId       [optional]

                // As given; anything but a text, also JSON null, as empty.
                String? elementId = null;

                if (JSON.TryGetValue(PropertyKeys.ElementId, out var elementIdJSON))
                    elementId = elementIdJSON.Type == JTokenType.String
                                    ? elementIdJSON.Value<String>()
                                    : "";

                #endregion


                if (Refusal(propertyName, elementId) is String refusal)
                {
                    ErrorResponse = refusal;
                    return false;
                }

                POIElementPathSegment = new POIElementPathSegment(
                                            propertyName!,
                                            elementId
                                        );

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                POIElementPathSegment  = null;
                ErrorResponse          = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false, CustomPOIElementPathSegmentSerializer = null)

        /// <summary>
        /// Return a JSON representation of this element path segment.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomPOIElementPathSegmentSerializer">An optional custom serializer.</param>
        public JObject ToJSON(Boolean                                                  Embedded                                = false,
                              CustomJObjectSerializerDelegate<POIElementPathSegment>?  CustomPOIElementPathSegmentSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,       JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.PropertyName,  PropertyName),

                           ElementId is not null
                               ? new JProperty(PropertyKeys.ElementId,     ElementId)
                               : null

                       );

            return CustomPOIElementPathSegmentSerializer is not null
                       ? CustomPOIElementPathSegmentSerializer(this, json)
                       : json;

        }

        #endregion

    }

}
