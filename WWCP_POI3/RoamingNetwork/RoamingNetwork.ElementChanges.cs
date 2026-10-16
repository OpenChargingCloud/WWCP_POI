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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Element changes: AddElement, RemoveElement, ReplaceElement, UpdateElementProperty and RemoveElementProperty
    /// address one element of a property of an entity by their element path (see POIElements).
    /// </summary>
    /// <remarks>
    /// An element change amounts to an UpdateProperty, or a RemoveProperty when nothing is left, of the property of the
    /// entity its path starts with, and is checked as that change: by the owners of that property, as a property a change
    /// set can change, and by reading the changed entity again. Its old value is compared with the element, or the
    /// property of the element, it addresses, so that concurrent changes of other elements of the same property do not
    /// conflict with it. The keys of a party are changed as a whole.
    /// </remarks>
    public sealed partial class RoamingNetwork
    {

        #region (private static) TryResolveElementChange(ref Change, EntityJSON, Description, out ElementValue, out ErrorResponse)

        /// <summary>
        /// Replace the given element change by the UpdateProperty or RemoveProperty of the property of the entity it amounts to,
        /// after comparing its old value with the value it addresses. Any other change stays as it is.
        /// </summary>
        /// <param name="Change">A change, replaced by the change of the property, when it is an element change.</param>
        /// <param name="EntityJSON">The JSON representation of the entity, null when it does not exist.</param>
        /// <param name="Description">The description of the entity type, e.g. "charging pool".</param>
        /// <param name="ElementValue">The value the element change finds: the element, or the property of the element; null when absent.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        private static Boolean TryResolveElementChange(ref RoamingNetworkChange           Change,
                                                       JObject?                           EntityJSON,
                                                       String                             Description,
                                                       out JToken?                        ElementValue,
                                                       [NotNullWhen(false)] out String?   ErrorResponse)
        {

            ElementValue   = null;
            ErrorResponse  = null;

            if (!Change.IsElementChange)
                return true;

            var propertyName = Change.EntityPropertyName!;

            if (EntityJSON is null)
            {
                ErrorResponse = $"the {Description} '{Change.EntityId}' does not exist!";
                return false;
            }

            if (POIPartyKeys.IsKeyProperty(propertyName))
            {
                ErrorResponse = $"the {propertyName} of the {Description} '{Change.EntityId}' are changed as a whole, not element by element!";
                return false;
            }

            if (!POIElements.TryApply(Change,
                                      EntityJSON[propertyName],
                                      out var newPropertyValue,
                                      out ElementValue,
                                      out var elementError))
            {
                ErrorResponse = $"{Change.Kind} of the {Description} '{Change.EntityId}': {elementError}";
                return false;
            }

            if (Change.OldValue is JToken expected &&
                !OldValueMatches(expected, ElementValue))
            {
                ErrorResponse = $"'{POIElements.PathText(Change.ElementPath, Change.PropertyName)}' of the {Description} '{Change.EntityId}' is no longer the expected old value, but '{ElementValue?.ToString(Newtonsoft.Json.Formatting.None) ?? "absent"}'!";
                return false;
            }

            Change = newPropertyValue is not null
                         ? new RoamingNetworkChange(RoamingNetworkChangeKind.UpdateProperty, Change.EntityType, Change.EntityId, propertyName, null, newPropertyValue, Change.ParentEntityType, Change.ParentEntityId)
                         : new RoamingNetworkChange(RoamingNetworkChangeKind.RemoveProperty, Change.EntityType, Change.EntityId, propertyName, null, null,             Change.ParentEntityType, Change.ParentEntityId);

            return true;

        }

        #endregion

    }

}
