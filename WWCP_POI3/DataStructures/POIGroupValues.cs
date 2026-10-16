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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A group giving its members properties, which they take at runtime: of the groups of a member,
    /// the one with the higher priority decides.
    /// </summary>
    public interface IPOIPrioritizedGroup
    {

        /// <summary>
        /// The identification of the group, as a text.
        /// </summary>
        String          GroupId         { get; }

        /// <summary>
        /// Its priority over the other groups of a member: the higher wins; 0, unless given.
        /// </summary>
        Int32?          Priority        { get; }

        /// <summary>
        /// The timestamp of its last change.
        /// </summary>
        DateTimeOffset  LastChangeDate  { get; }

        /// <summary>
        /// Its JSON representation, writing the properties it gives its members as they write them.
        /// </summary>
        JObject         ToJSON();

    }


    /// <summary>
    /// The values the groups of one member give it, as the member takes them at runtime:
    /// - a single value is its own, else the one of its group with the highest priority giving one,
    /// - an array is its own united with those of all its groups; of elements with the same key its own,
    ///   else the one of the group with the higher priority.
    /// Groups with the same priority must not give different values that decide: an ArgumentException tells it.
    /// </summary>
    /// <typeparam name="TGroup">The type of the groups.</typeparam>
    internal sealed class POIGroupValues<TGroup>

        where TGroup : class, IPOIPrioritizedGroup

    {

        #region Data

        private readonly String                 member;
        private readonly Func<TGroup, JObject>  jsonOf;

        #endregion

        #region Properties

        /// <summary>
        /// The groups of the member, the highest priority first, of the same priority in the ordinal order of their identifications.
        /// </summary>
        public TGroup[]  Groups  { get; }

        /// <summary>
        /// The groups, named for a message, e.g. "'DE*GEF*SG2', 'DE*GEF*SG1'".
        /// </summary>
        public String    Names
            => String.Join(", ", Groups.Select(group => $"'{group.GroupId}'"));

        /// <summary>
        /// What the member takes, for a message: "with the properties of its groups 'DE*GEF*SG2', 'DE*GEF*SG1'",
        /// or, without groups, what it embeds is as it is at runtime.
        /// </summary>
        public String    With
            => Groups.Length > 0
                   ? $"with the properties of its groups {Names}"
                   : "with what it embeds as it is at runtime";

        #endregion

        #region Constructor(s)

        /// <summary>
        /// The values the given groups give a member.
        /// </summary>
        /// <param name="Member">The member, for a message, e.g. "the charging station 'DE*GEF*S1'".</param>
        /// <param name="Groups">The groups of the member giving it properties.</param>
        /// <param name="JSONOf">The JSON representation of a group, e.g. calculated once for all its members.</param>
        public POIGroupValues(String                 Member,
                              IEnumerable<TGroup>    Groups,
                              Func<TGroup, JObject>  JSONOf)
        {

            this.member  = Member;
            this.jsonOf  = JSONOf;
            this.Groups  = [.. Groups.OrderByDescending(group => group.Priority ?? 0).
                                      ThenBy           (group => group.GroupId, StringComparer.Ordinal)];

        }

        #endregion


        #region Single(PropertyName, Own, ValueOf)

        /// <summary>
        /// The own value, else the one of the group with the highest priority giving one.
        /// </summary>
        /// <param name="PropertyName">The JSON key of the property, as the groups and the member write it.</param>
        /// <param name="Own">The own value of the member, if any.</param>
        /// <param name="ValueOf">The value a group gives, if any.</param>
        public T? Single<T>(String               PropertyName,
                            T?                   Own,
                            Func<TGroup, T?>     ValueOf)

            where T : class

            => Own ?? (T?) Decided(PropertyName, group => ValueOf(group));

        /// <summary>
        /// The own value, else the one of the group with the highest priority giving one.
        /// </summary>
        /// <param name="PropertyName">The JSON key of the property, as the groups and the member write it.</param>
        /// <param name="Own">The own value of the member, if any.</param>
        /// <param name="ValueOf">The value a group gives, if any.</param>
        public T? Single<T>(String               PropertyName,
                            T?                   Own,
                            Func<TGroup, T?>     ValueOf)

            where T : struct

            => Own ?? (T?) Decided(PropertyName, group => ValueOf(group));

        #endregion

        #region Union(Own, ValuesOf)

        /// <summary>
        /// The own values united with those of all groups, each once, e.g. features.
        /// </summary>
        public IEnumerable<T> Union<T>(IEnumerable<T>                 Own,
                                       Func<TGroup, IEnumerable<T>>   ValuesOf)

            => Own.Concat(Groups.SelectMany(ValuesOf)).Distinct();

        #endregion

        #region Union(ElementName, Own, ValuesOf, KeyOf, ToJSON)

        /// <summary>
        /// The own elements and those of all groups: of the same key the own one, else the one of the group with the
        /// higher priority, e.g. brands by their identifications.
        /// </summary>
        /// <param name="ElementName">What an element is, for a message, e.g. "brand".</param>
        /// <param name="Own">The own elements.</param>
        /// <param name="ValuesOf">The elements a group gives.</param>
        /// <param name="KeyOf">The key of an element.</param>
        /// <param name="ToJSON">The JSON representation of an element, to compare two of the same key.</param>
        public IEnumerable<T> Union<T, TKey>(String                         ElementName,
                                             IEnumerable<T>                 Own,
                                             Func<TGroup, IEnumerable<T>>   ValuesOf,
                                             Func<T, TKey>                  KeyOf,
                                             Func<T, JToken>                ToJSON)

            where TKey : notnull

        {

            var elements  = new List<T>(Own);
            var taken     = new Dictionary<TKey, (T Element, TGroup? Group)>();

            foreach (var element in elements)
                taken.TryAdd(KeyOf(element), (element, null));

            foreach (var group in Groups)
            {
                foreach (var element in ValuesOf(group))
                {

                    var key = KeyOf(element);

                    if (!taken.TryGetValue(key, out var before))
                    {
                        taken.Add(key, (element, group));
                        elements.Add(element);
                        continue;
                    }

                    if (before.Group is not null &&
                        (before.Group.Priority ?? 0) == (group.Priority ?? 0) &&
                        !SameButIdentification(ToJSON(before.Element), ToJSON(element)))
                    {
                        throw Conflict(before.Group, group, $"{ElementName} '{key}'");
                    }

                }
            }

            return elements;

        }

        #endregion

        #region LastChange(Own)

        /// <summary>
        /// The latest of the given last change of the member and those of its groups.
        /// </summary>
        public DateTimeOffset LastChange(DateTimeOffset Own)

            => Groups.Select(group => group.LastChangeDate).Append(Own).Max();

        #endregion


        #region (private) Decided(PropertyName, ValueOf)

        /// <summary>
        /// The value of the group with the highest priority giving one, unless another one with the same priority gives another.
        /// </summary>
        private Object? Decided(String                   PropertyName,
                                Func<TGroup, Object?>    ValueOf)
        {

            TGroup?  decidingGroup  = null;
            Object?  value          = null;

            foreach (var group in Groups)
            {

                var candidate = ValueOf(group);

                if (candidate is null)
                    continue;

                if (decidingGroup is null)
                {
                    decidingGroup  = group;
                    value          = candidate;
                    continue;
                }

                if ((group.Priority ?? 0) < (decidingGroup.Priority ?? 0))
                    break;

                // A value as its type compares it, e.g. an identification in another spelling, else as they write it,
                // e.g. two addresses, or two opening times.
                if (candidate is ValueType && Equals(candidate, value))
                    continue;

                if (!JToken.DeepEquals(jsonOf(group)[PropertyName], jsonOf(decidingGroup)[PropertyName]))
                    throw Conflict(decidingGroup, group, $"'{PropertyName}'");

            }

            return value;

        }

        #endregion

        #region (private static) SameButIdentification(Element1, Element2)

        /// <summary>
        /// Whether two elements of the same key are the same, but for the spelling of their identification.
        /// </summary>
        private static Boolean SameButIdentification(JToken  Element1,
                                                     JToken  Element2)
        {

            if (Element1 is not JObject object1 || Element2 is not JObject object2)
                return JToken.DeepEquals(Element1, Element2);

            var without1 = (JObject) object1.DeepClone();
            var without2 = (JObject) object2.DeepClone();

            without1.Remove("@id");
            without2.Remove("@id");

            return JToken.DeepEquals(without1, without2);

        }

        #endregion

        #region (private) Conflict(Group1, Group2, What)

        /// <summary>
        /// Two groups of the same priority give the member different values.
        /// </summary>
        private ArgumentException Conflict(TGroup  Group1,
                                           TGroup  Group2,
                                           String  What)

            => new ($"The groups '{Group1.GroupId}' and '{Group2.GroupId}' give {member} different values of {What} with the same priority {Group1.Priority ?? 0}: their priorities must tell which one it takes!");

        #endregion

    }

}
