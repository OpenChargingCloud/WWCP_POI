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
    /// New values: the content holds what a change set gives, as it gives it (SPECIFICATION.md, section 4).
    /// </summary>
    /// <remarks>
    /// A change writes its new value into the JSON of the entity holding it, which is read again: the content is what the
    /// changed entity then writes, its arrays in their order, its identifications, timestamps and texts as their types write
    /// them, embedded objects without their JSON-LD context (section 2). A new value written otherwise is refused, so that
    /// what the owners of a change set sign is what the content holds, and an implementation applying change sets to JSON
    /// takes their values as they are. CreateChangeSet and merges write every new value as it is held.
    /// </remarks>
    public sealed partial class RoamingNetwork
    {

        #region (private static) FindHeld(HolderJSON, HolderType, Change)

        /// <summary>
        /// The entity the given change addresses within the given JSON of an entity holding it, or being it, as it was read
        /// again and written: found as the merge finds entities, by its type and its identification as its type compares
        /// them (MergeChildren).
        /// </summary>
        /// <param name="HolderJSON">The JSON of the changed entity holding it, or being it, written anew.</param>
        /// <param name="HolderType">The entity type of the holder.</param>
        /// <param name="Change">The change.</param>
        private static JObject? FindHeld(JObject?              HolderJSON,
                                         String                HolderType,
                                         RoamingNetworkChange  Change)
        {

            JObject? Walk(String     Type,
                          MergeKey?  Parent,
                          JObject    JSON)
            {

                var key = MergeKey.Of(Type, JSON[BasePropertyKeys.Id]?.Value<String>() ?? "", Parent);

                if (Type == Change.EntityType && key == MergeKey.Of(Change.EntityType, Change.EntityId, Parent))
                    return JSON;

                foreach (var (property, childType) in MergeChildren.TryGetValue(Type, out var children) ? children : [])
                {

                    IEnumerable<JObject> childObjects = JSON[property] is JObject single
                                                            ? [ single ]
                                                            : (JSON[property] as JArray ?? []).OfType<JObject>();

                    foreach (var child in childObjects)
                        if (Walk(childType, key, child) is JObject found)
                            return found;

                }

                return null;

            }

            return HolderJSON is not null
                       ? Walk(HolderType, null, HolderJSON)
                       : null;

        }

        #endregion

        #region (private static) TryHeldAsGiven(Given, Held, Description, AsHeld, out HeldValue, out ErrorResponse)

        /// <summary>
        /// Whether the content holds the new value of the given change as the change gives it, compared as canonical JSON:
        /// of an UpdateProperty the property, of an Add the entity without its lastChange (and without its created, unless
        /// given), of an AddElement or ReplaceElement the element, of an UpdateElementProperty the property of the element.
        /// A removal gives no new value. A new value the content does not hold at all, e.g. an empty array written as none,
        /// is refused as well.
        /// </summary>
        /// <param name="Given">The change as the change set gives it, an element change not resolved.</param>
        /// <param name="Held">The entity it addresses, as the content holds it after the change (FindHeld); asked for only when needed.</param>
        /// <param name="Description">The entity type in words, e.g. "charging pool".</param>
        /// <param name="AsHeld">Take a new value as the content holds it, instead of refusing it: as CreateChangeSet and merges do.</param>
        /// <param name="HeldValue">The new value as the content holds it; null for a removal.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        private static Boolean TryHeldAsGiven(RoamingNetworkChange              Given,
                                              Func<JObject?>                    Held,
                                              String                            Description,
                                              Boolean                           AsHeld,
                                              out JToken?                       HeldValue,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            HeldValue      = null;
            ErrorResponse  = null;

            var given      = Given.NewValue;
            String what;

            switch (Given.Kind)
            {

                case RoamingNetworkChangeKind.UpdateProperty:
                    what       = $"the property '{Given.PropertyName}' of the {Description} '{Given.EntityId}'";
                    HeldValue  = Held()?[Given.PropertyName!]?.DeepClone();
                    break;

                case RoamingNetworkChangeKind.Add:

                    what = $"the {Description} '{Given.EntityId}' to add";

                    // The change set sets its lastChange, and its created unless given.
                    if (Held()?.DeepClone() is JObject held)
                    {

                        held.Remove(BasePropertyKeys.LastChange);

                        if (given is JObject givenEntity && givenEntity[BasePropertyKeys.Created] is null)
                            held.Remove(BasePropertyKeys.Created);

                        HeldValue = held;

                    }

                    if (given is JObject givenWithLastChange && givenWithLastChange[BasePropertyKeys.LastChange] is not null)
                    {
                        var withoutLastChange = (JObject) givenWithLastChange.DeepClone();
                        withoutLastChange.Remove(BasePropertyKeys.LastChange);
                        given = withoutLastChange;
                    }

                    break;

                case RoamingNetworkChangeKind.AddElement:
                case RoamingNetworkChangeKind.ReplaceElement:
                case RoamingNetworkChangeKind.UpdateElementProperty:

                    var elementProperty = Given.Kind == RoamingNetworkChangeKind.UpdateElementProperty;
                    what = $"'{POIElements.PathText(Given.ElementPath, elementProperty ? Given.PropertyName : null)}' of the {Description} '{Given.EntityId}'";

                    // What its removal would find there.
                    var removal = new RoamingNetworkChange(elementProperty ? RoamingNetworkChangeKind.RemoveElementProperty : RoamingNetworkChangeKind.RemoveElement,
                                                           Given.EntityType,
                                                           Given.EntityId,
                                                           Given.PropertyName,
                                                           null,
                                                           null,
                                                           Given.ParentEntityType,
                                                           Given.ParentEntityId,
                                                           Given.ElementPath);

                    HeldValue = POIElements.TryApply(removal, Held()?[Given.EntityPropertyName!], out _, out var found, out _)
                                    ? found?.DeepClone()
                                    : null;

                    break;

                default:
                    return true;

            }

            if (HeldValue is null)
            {
                ErrorResponse = $"{what} is given as '{POIJSON.ContentText(given)}', which the content does not hold at all!";
                return false;
            }

            if (AsHeld || POIJSON.SameContent(given, HeldValue))
                return true;

            ErrorResponse = $"{what} is not given as the content holds it, '{POIJSON.ContentText(HeldValue)}'!";
            return false;

        }

        #endregion

    }

}
