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

        #region Properties

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

            if (String.IsNullOrWhiteSpace(PropertyName))
                throw new ArgumentException("A schema property name is required.", nameof(PropertyName));

            if (ElementId is not null && String.IsNullOrWhiteSpace(ElementId))
                throw new ArgumentException("An element identifier must not be empty.", nameof(ElementId));

            this.PropertyName  = PropertyName;
            this.ElementId     = ElementId;

        }

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

                #region Parse PropertyName    [mandatory]

                if (!JSON.ParseMandatoryText("propertyName",
                                             "property name",
                                             out var propertyName,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse ElementId       [optional]

                if (JSON.ParseOptional("elementId",
                                       "element identification",
                                       out String? elementId,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                POIElementPathSegment = new POIElementPathSegment(
                                            propertyName,
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

        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of this element path segment.
        /// </summary>
        public JObject ToJSON()

            => JSONObject.Create(

                         new JProperty("propertyName",  PropertyName),

                   ElementId is not null
                       ? new JProperty("elementId",     ElementId)
                       : null

               );

        #endregion

    }

}
