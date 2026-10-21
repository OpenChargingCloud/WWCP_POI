/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

#region Usings

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The elements of the properties of an entity, as element changes address them.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>An element of an array is identified by its <c>@id</c> text, a text element by itself.
    ///         An array is addressed element by element only when every element has an identification and no two the same.
    ///         The identifications of an array of identifications, e.g. <c>EVSEIds</c>, or of identified objects, e.g.
    ///         <c>chargingConnectors</c>, are the same as their type compares them, e.g. without case; any other ordinally.</item>
    ///   <item>The first segment of an element path names a property of the entity itself, every further one a property
    ///         of the element the segment before addresses. A segment on an array names the element by its identification;
    ///         a segment on any other value may name its <c>@id</c>, which it must then have.</item>
    ///   <item>An added element goes where the ordinal order of the identifications puts it, when the array is in that order,
    ///         otherwise at its end; to <c>templateIds</c>, whose order is content, always at its end. An array left without
    ///         elements is removed, as entities write no empty arrays.</item>
    /// </list>
    /// </remarks>
    public static class POIElements
    {

        #region Data

        /// <summary>
        /// The property of a member naming its templates, in an order that is content: the first template giving a value wins.
        /// </summary>
        public const String TemplateIdsProperty = "templateIds";

        /// <summary>
        /// The properties holding one identified object, not an array of them, e.g. the energy meter of an EVSE.
        /// </summary>
        private static readonly HashSet<String> SingleIdentifiedProperties = [
            EVSE.PropertyKeys.EnergyMeter
        ];

        /// <summary>
        /// The identification types of the elements of a property, by the name of the property:
        /// its elements are the same as their type compares them, e.g. without case.
        /// </summary>
        private static readonly Dictionary<String, Func<String, Object?>> ElementIdTypes = new () {
            { ChargingStation.PropertyKeys.EVSEIds,                           text => EVSE_Id.             TryParse(text, out var id) ? id : null },
            { ChargingPool.PropertyKeys.ChargingStationIds,                   text => ChargingStation_Id.  TryParse(text, out var id) ? id : null },
            { ChargingStationOperator.PropertyKeys.ChargingPoolIds,           text => ChargingPool_Id.     TryParse(text, out var id) ? id : null },
            { EVSE.PropertyKeys.ChargingTariffIds,                            text => ChargingTariff_Id.   TryParse(text, out var id) ? id : null },
            { ChargingPool.PropertyKeys.EnergyMeterIds,                       text => EnergyMeter_Id.      TryParse(text, out var id) ? id : null },
            { ChargingTariffOffer.PropertyKeys.ChargingStationOperatorIds,    text => ChargingStationOperator_Id.TryParse(text, out var id) ? id : null },
            { ChargingTariffOffer.PropertyKeys.EVSEGroupIds,                  text => EVSEGroup_Id.           TryParse(text, out var id) ? id : null },
            { ChargingTariffOffer.PropertyKeys.ChargingStationGroupIds,       text => ChargingStationGroup_Id.TryParse(text, out var id) ? id : null },
            { ChargingTariffOffer.PropertyKeys.ChargingPoolGroupIds,          text => ChargingPoolGroup_Id.   TryParse(text, out var id) ? id : null },
            { TemplateIdsProperty,                                            TemplateIdOf },
            { ChargingPool.PropertyKeys.Brands,                               text => Brand_Id.            TryParse(text, out var id) ? id : null },
            { ChargingStation.PropertyKeys.ParkingSpaceIds,                   text => ParkingSpace_Id.     TryParse(text, out var id) ? id : null },
            { ParkingOperator.PropertyKeys.ParkingSensorIds,                  text => ParkingSensor_Id.    TryParse(text, out var id) ? id : null },
            { ParkingOperator.PropertyKeys.ParkingProductIds,                 text => ParkingProduct_Id.   TryParse(text, out var id) ? id : null },
            { ChargingTariffOffer.PropertyKeys.EMobilityProviderIds,          text => EMobilityProvider_Id.TryParse(text, out var id) ? id : null },
            { EVSE.PropertyKeys.ChargingConnectors,                           text => ChargingConnector_Id.TryParse(text, out var id) ? id : null },
            { ChargingPool.PropertyKeys.EnergyMeters,                         text => EnergyMeter_Id.      TryParse(text, out var id) ? id : null },
            { "signatureMethods",                                             text => SignatureMethod.     TryParse(text, out var id) ? id : null }
        };

        #endregion


        #region (private static) TemplateIdOf(Text)

        /// <summary>
        /// The template identification the given text names: of an EVSE, charging station, charging pool or charging
        /// tariff template by its infix, otherwise of a parking space or energy meter template, both without case.
        /// </summary>
        private static Object? TemplateIdOf(String Text)

            => EVSETemplate_Id.           TryParse(Text, out var evseTemplateId)     ? evseTemplateId
             : ChargingStationTemplate_Id.TryParse(Text, out var stationTemplateId)  ? stationTemplateId
             : ChargingPoolTemplate_Id.   TryParse(Text, out var poolTemplateId)     ? poolTemplateId
             : ChargingTariffTemplate_Id. TryParse(Text, out var tariffTemplateId)   ? tariffTemplateId
             : ParkingSpaceTemplate_Id.   TryParse(Text, out var otherTemplateId)    ? otherTemplateId
             : null;

        #endregion

        #region IdOf(Element)

        /// <summary>
        /// The identification of the given array element: its <c>@id</c> text, or the text itself; null when it has none.
        /// </summary>
        /// <param name="Element">An array element.</param>
        public static String? IdOf(JToken? Element)

            => Element switch {
                   JValue  { Type: JTokenType.String } text                                     => text.Value<String>(),
                   JObject element when element["@id"] is JValue { Type: JTokenType.String } id  => id.Value<String>(),
                   _                                                                            => null
               };

        #endregion

        #region IsIdentified(Array)

        /// <summary>
        /// Whether every element of the given array has an identification, and no two the same.
        /// </summary>
        /// <param name="Array">An array.</param>
        public static Boolean IsIdentified(JArray Array)
        {

            var ids = new HashSet<String>(StringComparer.Ordinal);

            foreach (var element in Array)
                if (IdOf(element) is not String id || !ids.Add(id))
                    return false;

            return true;

        }

        #endregion

        #region IsIdentified(PropertyName, Array)

        /// <summary>
        /// Whether every element of the given array of the given property has an identification,
        /// and no two the same, as the identification type of the property compares them.
        /// </summary>
        /// <param name="PropertyName">The property holding the array.</param>
        /// <param name="Array">An array.</param>
        public static Boolean IsIdentified(String  PropertyName,
                                           JArray  Array)
        {

            var keys = new HashSet<Object>();

            foreach (var element in Array)
                if (IdOf(element) is not String id || !keys.Add(KeyOf(PropertyName, id)))
                    return false;

            return true;

        }

        #endregion

        #region KeyOf(PropertyName, Id)

        /// <summary>
        /// The given identification of an element of the given property, as its identification type compares it,
        /// e.g. an EVSE_Id of EVSEIds; the text itself for any other property, or a text its type does not read.
        /// </summary>
        /// <param name="PropertyName">The property holding the element.</param>
        /// <param name="Id">The identification of the element.</param>
        public static Object KeyOf(String  PropertyName,
                                   String  Id)

            => ElementIdTypes.TryGetValue(PropertyName, out var typed) && typed(Id) is Object key
                   ? key
                   : Id;

        #endregion

        #region (private static) SameId(PropertyName, Id1, Id2)

        /// <summary>
        /// Whether the given identifications name the same element of the given property.
        /// </summary>
        private static Boolean SameId(String   PropertyName,
                                      String?  Id1,
                                      String?  Id2)

            => Id1 is not null &&
               Id2 is not null &&
               KeyOf(PropertyName, Id1).Equals(KeyOf(PropertyName, Id2));

        #endregion

        #region PathText(ElementPath, PropertyName = null)

        /// <summary>
        /// The given element path as a text for messages, e.g. <c>chargingConnectors['1'].maxPower</c>.
        /// </summary>
        /// <param name="ElementPath">An element path.</param>
        /// <param name="PropertyName">The property of the addressed element, if any.</param>
        public static String PathText(IEnumerable<POIElementPathSegment>  ElementPath,
                                      String?                             PropertyName   = null)

            => String.Join(".", ElementPath.Select(segment => segment.ElementId is not null
                                                                  ? $"{segment.PropertyName}['{segment.ElementId}']"
                                                                  : segment.PropertyName).
                                            Concat(PropertyName is not null ? [ PropertyName ] : []));

        #endregion


        #region TryApply(Change, PropertyValue, out NewPropertyValue, out CurrentValue, out ErrorResponse)

        /// <summary>
        /// Apply the given element change to the value of the property of the entity its path starts with.
        /// </summary>
        /// <param name="Change">An element change.</param>
        /// <param name="PropertyValue">The value of the property its path starts with, null when absent. It is not changed.</param>
        /// <param name="NewPropertyValue">The new value of that property, null when it is removed.</param>
        /// <param name="CurrentValue">The value the change finds, as its old value names it: the element, or the property
        /// of the element; null when absent.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryApply(RoamingNetworkChange              Change,
                                       JToken?                           PropertyValue,
                                       out JToken?                       NewPropertyValue,
                                       out JToken?                       CurrentValue,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            NewPropertyValue  = PropertyValue;
            CurrentValue      = null;

            var path          = Change.ElementPath;

            if (path.IsDefaultOrEmpty)
            {
                ErrorResponse = $"{Change.Kind} needs an element path!";
                return false;
            }

            // The property of the entity, held by an object, so that every step has an object holding it.
            var root          = new JObject();

            if (PropertyValue is not null)
                root[path[0].PropertyName] = PropertyValue.DeepClone();

            var holder        = root;

            #region Down to the object holding the addressed value

            for (var index = 0; index < path.Length - 1; index++)
            {

                var segment  = path[index];
                var here     = PathText(path.Take(index + 1));
                var value    = holder[segment.PropertyName];

                JToken? element;

                if (value is JArray array)
                {

                    if (segment.ElementId is null)
                    {
                        ErrorResponse = $"'{here}' is an array: its elements are addressed by their identifications!";
                        return false;
                    }

                    if (!IsIdentified(segment.PropertyName, array))
                    {
                        ErrorResponse = $"The elements of '{PathText(path.Take(index).Append(new POIElementPathSegment(segment.PropertyName)))}' have no identifications of their own: it is changed as a whole!";
                        return false;
                    }

                    element = array.FirstOrDefault(item => SameId(segment.PropertyName, IdOf(item), segment.ElementId));

                }
                else
                    element = value is not null && (segment.ElementId is null || SameId(segment.PropertyName, IdOf(value), segment.ElementId))
                                  ? value
                                  : null;

                if (element is null)
                {
                    ErrorResponse = $"'{here}' does not exist!";
                    return false;
                }

                if (element is not JObject elementObject)
                {
                    ErrorResponse = $"'{here}' is no JSON object!";
                    return false;
                }

                holder = elementObject;

            }

            #endregion

            var last          = path[^1];
            var target        = PathText(path);
            var targetValue   = holder[last.PropertyName];
            var targetArray   = targetValue as JArray;
            var elementIndex  = -1;
            JToken? addressed = null;

            #region The addressed value: an element of an array, or the value of a property

            if (targetArray is not null)
            {

                if (last.ElementId is null)
                {
                    ErrorResponse = $"'{target}' is an array: its elements are addressed by their identifications!";
                    return false;
                }

                if (!IsIdentified(last.PropertyName, targetArray))
                {
                    ErrorResponse = $"The elements of '{PathText(path.Take(path.Length - 1).Append(new POIElementPathSegment(last.PropertyName)))}' have no identifications of their own: it is changed as a whole!";
                    return false;
                }

                for (var index = 0; index < targetArray.Count && elementIndex < 0; index++)
                    if (SameId(last.PropertyName, IdOf(targetArray[index]), last.ElementId))
                        elementIndex = index;

                addressed = elementIndex >= 0 ? targetArray[elementIndex] : null;

            }
            else if (targetValue is not null)
            {

                if (last.ElementId is not null && !SameId(last.PropertyName, IdOf(targetValue), last.ElementId))
                {
                    ErrorResponse = $"'{target}' does not exist: the value of '{last.PropertyName}' is not '{last.ElementId}'!";
                    return false;
                }

                addressed = targetValue;

            }

            #endregion

            switch (Change.Kind)
            {

                #region AddElement

                case RoamingNetworkChangeKind.AddElement:

                    if (addressed is not null)
                    {
                        ErrorResponse = $"'{target}' already exists!";
                        return false;
                    }

                    var newElement = Change.NewValue!;

                    // An element of an array, existing or new, or the value of an absent property.
                    if (targetArray is not null || last.ElementId is not null)
                    {

                        if (!SameId(last.PropertyName, IdOf(newElement), last.ElementId))
                        {
                            ErrorResponse = $"The element to add to '{last.PropertyName}' must have the identification '{last.ElementId}'!";
                            return false;
                        }

                        // An absent array is empty: the first element makes it, but a property of one identified object is set.
                        if (targetArray is null)
                            holder[last.PropertyName] = SingleIdentifiedProperties.Contains(last.PropertyName)
                                                            ? newElement
                                                            : new JArray(newElement);

                        else
                            Insert(last.PropertyName, targetArray, newElement);

                    }
                    else
                        holder[last.PropertyName] = newElement;

                    break;

                #endregion

                #region RemoveElement

                case RoamingNetworkChangeKind.RemoveElement:

                    if (addressed is null)
                    {
                        ErrorResponse = $"'{target}' does not exist!";
                        return false;
                    }

                    CurrentValue = addressed.DeepClone();

                    if (targetArray is not null)
                    {

                        targetArray.RemoveAt(elementIndex);

                        if (targetArray.Count == 0)
                            holder.Remove(last.PropertyName);

                    }
                    else
                        holder.Remove(last.PropertyName);

                    break;

                #endregion

                #region ReplaceElement

                case RoamingNetworkChangeKind.ReplaceElement:

                    if (addressed is null)
                    {
                        ErrorResponse = $"'{target}' does not exist!";
                        return false;
                    }

                    CurrentValue = addressed.DeepClone();

                    var replacement = Change.NewValue!;

                    if (targetArray is not null)
                    {

                        if (!SameId(last.PropertyName, IdOf(replacement), last.ElementId))
                        {
                            ErrorResponse = $"'{target}' must keep its identification: another element is removed and added!";
                            return false;
                        }

                        targetArray[elementIndex] = replacement;

                    }
                    else
                    {

                        // One identified object keeps its identification as well.
                        if (addressed is JObject && IdOf(addressed) is String addressedId &&
                            !SameId(last.PropertyName, IdOf(replacement), addressedId))
                        {
                            ErrorResponse = $"'{target}' must keep its identification '{addressedId}': another one is removed and added!";
                            return false;
                        }

                        holder[last.PropertyName] = replacement;

                    }

                    break;

                #endregion

                #region UpdateElementProperty, RemoveElementProperty

                case RoamingNetworkChangeKind.UpdateElementProperty:
                case RoamingNetworkChangeKind.RemoveElementProperty:

                    if (addressed is not JObject addressedObject)
                    {
                        ErrorResponse = addressed is null
                                            ? $"'{target}' does not exist!"
                                            : $"'{target}' is no JSON object!";
                        return false;
                    }

                    var propertyName = Change.PropertyName!;

                    if (propertyName == "@id")
                    {
                        ErrorResponse = $"The identification of '{target}' can not be changed: another element is removed and added!";
                        return false;
                    }

                    CurrentValue = addressedObject[propertyName]?.DeepClone();

                    if (Change.Kind == RoamingNetworkChangeKind.RemoveElementProperty)
                    {

                        if (CurrentValue is null)
                        {
                            ErrorResponse = $"'{PathText(path, propertyName)}' does not exist!";
                            return false;
                        }

                        addressedObject.Remove(propertyName);

                    }
                    else
                        addressedObject[propertyName] = Change.NewValue;

                    break;

                #endregion

                default:
                    ErrorResponse = $"{Change.Kind} is no element change!";
                    return false;

            }

            NewPropertyValue  = root[path[0].PropertyName];
            ErrorResponse     = null;
            return true;

        }

        #endregion

        #region Diff(EntityType, EntityId, PropertyName, OldValue, NewValue)

        /// <summary>
        /// The changes leading from the given value of a property of an entity to the new one, with their old values.
        /// Element changes, when both values are arrays of identified elements, an absent value counting as an empty array,
        /// applying them leads to exactly the new value, and there are not more of them than elements in the new value:
        /// the removed elements in their old order, then the added and changed ones in their new order, a changed object
        /// property by property. Otherwise one UpdateProperty or RemoveProperty. None, when both values are the same.
        /// </summary>
        /// <param name="EntityType">The type of the entity.</param>
        /// <param name="EntityId">The identification of the entity.</param>
        /// <param name="PropertyName">A property of the entity itself.</param>
        /// <param name="OldValue">Its value, null when absent.</param>
        /// <param name="NewValue">Its new value, null when absent.</param>
        public static IReadOnlyList<RoamingNetworkChange> Diff(String   EntityType,
                                                               String   EntityId,
                                                               String   PropertyName,
                                                               JToken?  OldValue,
                                                               JToken?  NewValue)
        {

            if (Same(OldValue, NewValue))
                return [];

            if (TryDiffElements(EntityType, EntityId, PropertyName, OldValue, NewValue, out var elementChanges))
                return elementChanges;

            return [ NewValue is null
                         ? RoamingNetworkChange.RemoveProperty(EntityType, EntityId, PropertyName, OldValue)
                         : RoamingNetworkChange.UpdateProperty(EntityType, EntityId, PropertyName, OldValue ?? JValue.CreateNull(), NewValue) ];

        }

        #endregion

        #region TryMerge(PropertyName, Base, Left, Right, out Merged)

        /// <summary>
        /// Merge the changes of the right value of a property against the base value into the left one, element by element,
        /// when all of them are arrays of identified elements, an absent value counting as an empty array: an element only
        /// the right one added is added, one it removed is removed, one it changed takes its change, unless the left one
        /// changed it otherwise; an object both changed differently is merged property by property, each as a whole.
        /// Otherwise, or when they conflict, there is no merge.
        /// </summary>
        /// <param name="PropertyName">The property, whose identification type compares its elements.</param>
        /// <param name="Base">The base value, null when absent.</param>
        /// <param name="Left">The left value, null when absent.</param>
        /// <param name="Right">The right value, null when absent.</param>
        /// <param name="Merged">The merged value, null when absent.</param>
        public static Boolean TryMerge(String       PropertyName,
                                       JToken?      Base,
                                       JToken?      Left,
                                       JToken?      Right,
                                       out JToken?  Merged)
        {

            Merged = null;

            if (!TryIdentified(PropertyName, Base,  out var baseArray)  ||
                !TryIdentified(PropertyName, Left,  out var leftArray)  ||
                !TryIdentified(PropertyName, Right, out var rightArray) ||
                (Base is null && Left is null && Right is null))
            {
                return false;
            }

            var merged      = (JArray) leftArray.DeepClone();
            var rightKeys   = rightArray.Select(element => KeyOf(PropertyName, IdOf(element)!)).ToHashSet();

            JToken? Find(JArray Array, String Id)
                => Array.FirstOrDefault(element => SameId(PropertyName, IdOf(element), Id));

            // Removed by the right one: removed, unless the left one changed it.
            foreach (var baseElement in baseArray)
            {

                var id = IdOf(baseElement)!;

                if (rightKeys.Contains(KeyOf(PropertyName, id)))
                    continue;

                var leftElement = Find(merged, id);

                if (leftElement is null)
                    continue;

                if (!POIJSON.SameValue(leftElement, baseElement))
                    return false;

                leftElement.Remove();

            }

            // Added or changed by the right one.
            foreach (var rightElement in rightArray)
            {

                var id           = IdOf(rightElement)!;
                var baseElement  = Find(baseArray, id);
                var leftElement  = Find(merged,    id);

                if (Same(rightElement, baseElement) || Same(rightElement, leftElement))
                    continue;

                // Added by the right one, or changed by it and removed by the left one.
                if (leftElement is null)
                {

                    if (baseElement is not null)
                        return false;

                    Insert(PropertyName, merged, rightElement.DeepClone());
                    continue;

                }

                // Added by both differently, or changed by both.
                if (baseElement is null)
                    return false;

                if (POIJSON.SameValue(leftElement, baseElement))
                {
                    leftElement.Replace(rightElement.DeepClone());
                    continue;
                }

                if (leftElement  is not JObject leftObject  ||
                    rightElement is not JObject rightObject ||
                    baseElement  is not JObject baseObject)
                {
                    return false;
                }

                foreach (var propertyName in leftObject. Properties().Select(property => property.Name).
                                             Union(rightObject.Properties().Select(property => property.Name)).
                                             Union(baseObject. Properties().Select(property => property.Name)).
                                             ToArray())
                {

                    var bv = baseObject [propertyName];
                    var lv = leftObject [propertyName];
                    var rv = rightObject[propertyName];

                    if (Same(lv, rv) || Same(rv, bv))
                        continue;

                    if (!Same(lv, bv))
                        return false;

                    if (rv is null)
                        leftObject.Remove(propertyName);
                    else
                        leftObject[propertyName] = rv.DeepClone();

                }

            }

            Merged = merged.Count > 0 ? merged : null;
            return true;

        }

        #endregion


        #region (private static) TryDiffElements(EntityType, EntityId, PropertyName, OldValue, NewValue, out Changes)

        private static Boolean TryDiffElements(String                                                      EntityType,
                                               String                                                      EntityId,
                                               String                                                      PropertyName,
                                               JToken?                                                     OldValue,
                                               JToken?                                                     NewValue,
                                               [NotNullWhen(true)] out IReadOnlyList<RoamingNetworkChange>? Changes)
        {

            Changes = null;

            if (!TryIdentified(PropertyName, OldValue, out var oldArray) ||
                !TryIdentified(PropertyName, NewValue, out var newArray))
            {
                return false;
            }

            var changes  = new List<RoamingNetworkChange>();
            var newKeys  = newArray.Select(element => KeyOf(PropertyName, IdOf(element)!)).ToHashSet();

            ImmutableArray<POIElementPathSegment> PathOf(String Id)
                => [ new POIElementPathSegment(PropertyName, Id) ];

            foreach (var oldElement in oldArray)
                if (!newKeys.Contains(KeyOf(PropertyName, IdOf(oldElement)!)))
                    changes.Add(RoamingNetworkChange.RemoveElement(EntityType, EntityId, PathOf(IdOf(oldElement)!), oldElement));

            foreach (var newElement in newArray)
            {

                var id          = IdOf(newElement)!;
                var oldElement  = oldArray.FirstOrDefault(element => SameId(PropertyName, IdOf(element), id));

                if (oldElement is null)
                    changes.Add(RoamingNetworkChange.AddElement(EntityType, EntityId, PathOf(id), newElement));

                else if (!POIJSON.SameValue(oldElement, newElement))
                {

                    if (oldElement is JObject oldObject && newElement is JObject newObject)
                    {
                        foreach (var propertyName in oldObject.Properties().Select(property => property.Name).
                                                     Union(newObject.Properties().Select(property => property.Name)).
                                                     Where(propertyName => !Same(oldObject[propertyName], newObject[propertyName])))
                        {
                            changes.Add(newObject[propertyName] is JToken newPropertyValue
                                            ? RoamingNetworkChange.UpdateElementProperty(EntityType, EntityId, PathOf(id), propertyName, oldObject[propertyName] ?? JValue.CreateNull(), newPropertyValue)
                                            : RoamingNetworkChange.RemoveElementProperty(EntityType, EntityId, PathOf(id), propertyName, oldObject[propertyName]));
                        }
                    }
                    else
                        changes.Add(RoamingNetworkChange.ReplaceElement(EntityType, EntityId, PathOf(id), oldElement, newElement));

                }

            }

            if (changes.Count > newArray.Count)
                return false;

            // Applied one after the other, they must lead to exactly the new value, in its order.
            var value = OldValue;

            foreach (var change in changes)
                if (!TryApply(change, value, out value, out _, out _))
                    return false;

            if (!Same(value, NewValue))
                return false;

            Changes = changes;
            return true;

        }

        #endregion

        #region (private static) TryIdentified(Value, out Array)

        /// <summary>
        /// The given value as an array of identified elements, an absent value as an empty one.
        /// </summary>
        private static Boolean TryIdentified(String                          PropertyName,
                                             JToken?                         Value,
                                             [NotNullWhen(true)] out JArray?  Array)
        {

            Array = Value switch {
                        null                                                 => [],
                        JArray array when IsIdentified(PropertyName, array)  => array,
                        _                                                    => null
                    };

            return Array is not null;

        }

        #endregion

        #region (private static) Insert(PropertyName, Array, Element)

        /// <summary>
        /// Insert the given element where the ordinal order of the identifications puts it, when the array is in that order,
        /// otherwise at its end; into <c>templateIds</c>, whose order is content, always at its end.
        /// </summary>
        private static void Insert(String  PropertyName,
                                   JArray  Array,
                                   JToken  Element)
        {

            var id       = IdOf(Element)!;
            var ids      = Array.Select(element => IdOf(element)!).ToArray();
            var ordered  = PropertyName != TemplateIdsProperty &&
                           ids.Zip(ids.Skip(1)).All(pair => String.CompareOrdinal(pair.First, pair.Second) < 0);

            if (ordered)
                Array.Insert(ids.TakeWhile(other => String.CompareOrdinal(other, id) < 0).Count(), Element);

            else
                Array.Add(Element);

        }

        #endregion

        #region (private static) Same(Value1, Value2)

        /// <summary>
        /// Whether the given values are equal; null means absent.
        /// </summary>
        private static Boolean Same(JToken? Value1, JToken? Value2)

            => Value1 is null
                   ? Value2 is null
                   : Value2 is not null && POIJSON.SameValue(Value1, Value2);

        #endregion

    }

}
