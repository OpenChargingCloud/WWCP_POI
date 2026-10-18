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

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The values the templates of one member give it, as the member takes them at runtime, in the order its templateIds
    /// name them:
    /// - a single value is its own, else the one of the first of its templates giving one,
    /// - an array is its own united with those of all its templates; of elements with the same key its own, else the one
    ///   of the first template giving it.
    /// Templates never disagree: their order decides.
    /// </summary>
    /// <typeparam name="TTemplate">The type of the templates.</typeparam>
    internal sealed class POITemplateValues<TTemplate>

        where TTemplate : class, IPOITemplate

    {

        #region Properties

        /// <summary>
        /// The templates of the member, in the order of its templateIds.
        /// </summary>
        public TTemplate[]  Templates  { get; }

        /// <summary>
        /// The templates, named for a message, e.g. "'DE*GEF*ST2', 'DE*GEF*ST1'".
        /// </summary>
        public String       Names
            => String.Join(", ", Templates.Select(template => $"'{template.TemplateId}'"));

        /// <summary>
        /// What the member takes, for a message: "with the properties of its templates 'DE*GEF*ST2', 'DE*GEF*ST1'",
        /// or, without templates, what it embeds is as it is at runtime.
        /// </summary>
        public String       With
            => Templates.Length > 0
                   ? $"with the properties of its templates {Names}"
                   : "with what it embeds as it is at runtime";

        #endregion

        #region Constructor(s)

        /// <summary>
        /// The values the given templates give a member.
        /// </summary>
        /// <param name="Templates">The templates of the member, in the order of its templateIds.</param>
        public POITemplateValues(IEnumerable<TTemplate> Templates)
        {
            this.Templates = [.. Templates];
        }

        #endregion


        #region Single(Own, ValueOf)

        /// <summary>
        /// The own value, else the one of the first template giving one.
        /// </summary>
        /// <param name="Own">The own value of the member, if any.</param>
        /// <param name="ValueOf">The value a template gives, if any.</param>
        public T? Single<T>(T?                     Own,
                            Func<TTemplate, T?>    ValueOf)

            where T : class

            => Own ?? Templates.Select(ValueOf).FirstOrDefault(value => value is not null);

        /// <summary>
        /// The own value, else the one of the first template giving one.
        /// </summary>
        /// <param name="Own">The own value of the member, if any.</param>
        /// <param name="ValueOf">The value a template gives, if any.</param>
        public T? Single<T>(T?                     Own,
                            Func<TTemplate, T?>    ValueOf)

            where T : struct

            => Own ?? Templates.Select(ValueOf).FirstOrDefault(value => value.HasValue);

        #endregion

        #region Union(Own, ValuesOf)

        /// <summary>
        /// The own values united with those of all templates, each once, e.g. features.
        /// </summary>
        public IEnumerable<T> Union<T>(IEnumerable<T>                    Own,
                                       Func<TTemplate, IEnumerable<T>>   ValuesOf)

            => Own.Concat(Templates.SelectMany(ValuesOf)).Distinct();

        #endregion

        #region Union(Own, ValuesOf, KeyOf)

        /// <summary>
        /// The own elements and those of all templates: of the same key the own one, else the one of the first template
        /// giving it, e.g. brands by their identifications.
        /// </summary>
        /// <param name="Own">The own elements.</param>
        /// <param name="ValuesOf">The elements a template gives.</param>
        /// <param name="KeyOf">The key of an element.</param>
        public IEnumerable<T> Union<T, TKey>(IEnumerable<T>                    Own,
                                             Func<TTemplate, IEnumerable<T>>   ValuesOf,
                                             Func<T, TKey>                     KeyOf)

            where TKey : notnull

        {

            var elements  = new List<T>(Own);
            var taken     = new HashSet<TKey>(elements.Select(KeyOf));

            foreach (var template in Templates)
                foreach (var element in ValuesOf(template))
                    if (taken.Add(KeyOf(element)))
                        elements.Add(element);

            return elements;

        }

        #endregion

        #region LastChange(Own)

        /// <summary>
        /// The latest of the given last change of the member and those of its templates.
        /// </summary>
        public DateTimeOffset LastChange(DateTimeOffset Own)

            => Templates.Select(template => template.LastChangeDate).Append(Own).Max();

        #endregion

    }

}
