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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The members of groups giving them properties, in the JSON an importer changes them in: what a source tells of a
    /// member as its groups give it stays in its groups, so that a later change of a group still reaches the member.
    /// The same rules as at runtime (see POIGroupValues): a single value is its own, else the one of its groups; an array
    /// is its own united with those of its groups, of the same key its own winning.
    /// </summary>
    public static class POIGroupMembers
    {

        #region Data

        /// <summary>
        /// The arrays a member takes whole, its own, else the one of its groups: written as arrays, they are single
        /// values, e.g. the flags of the current types of an EVSE, or its charging tariffs.
        /// </summary>
        public static readonly IReadOnlySet<String> TakenWhole = new HashSet<String> {
            "currentTypes", "chargingModes", "chargingTariffIds"
        };

        /// <summary>
        /// The keys of the elements of the arrays a member unites with those of its groups, by which its own element wins:
        /// e.g. an authentication mode by its type. The elements of the other arrays are keyed by what they are.
        /// </summary>
        public static readonly IReadOnlyDictionary<String, String> ElementKeys = new Dictionary<String, String> {
            ["authenticationModes"]  = "type",
            ["brands"]               = "@id",
            ["chargingConnectors"]   = "@id",
            ["dataLicenses"]         = "@id",
            ["images"]               = "url"
        };

        #endregion


        #region KeyOf(Property, Element)

        /// <summary>
        /// The key of an element of the given array: its own one wins over one of its groups of the same key.
        /// </summary>
        /// <param name="Property">The JSON key of the array.</param>
        /// <param name="Element">An element of it.</param>
        public static String KeyOf(String  Property,
                                   JToken  Element)

            => ElementKeys.TryGetValue(Property, out var key) && Element is JObject element && element[key] is JValue value
                   ? value.ToString()
                   : Element.ToString(Formatting.None);

        #endregion

        #region KeepWhatGroupsGive(Desired, Stored, Effective, Properties)

        /// <summary>
        /// Keep in the groups of a member what they give it, of the given properties of the member as it is to be:
        /// - what it is to have as it has it with them, as it is effective, it keeps as it has it of its own, also nothing,
        /// - an array it takes whole it takes as its own when it is to have another one,
        /// - of an array it unites with theirs, it takes as its own only the elements whose keys they do not give it: a
        ///   source may tell less of an element than its group gives, e.g. an RFID authentication mode without its card
        ///   types, which so stay,
        /// - what else it is to have otherwise becomes its own, as its own wins.
        /// </summary>
        /// <param name="Desired">The member as it is to be, changed in place.</param>
        /// <param name="Stored">The member as it is stored, if it is.</param>
        /// <param name="Effective">The member with the properties its groups give it, as it is at runtime.</param>
        /// <param name="Properties">The properties to look at: those the source tells.</param>
        public static void KeepWhatGroupsGive(JObject              Desired,
                                              JObject?             Stored,
                                              JObject              Effective,
                                              IEnumerable<String>  Properties)
        {

            foreach (var property in Properties)
            {

                var desired    = Desired  [property];
                var stored     = Stored?  [property];
                var effective  = Effective[property];

                if (POIJSON.SameValue(desired, effective))
                {
                    if (stored is not null)
                        Desired[property] = stored.DeepClone();
                    else
                        Desired.Remove(property);
                }

                else if (TakenWhole.Contains(property))
                    continue;

                else if (desired is JArray desiredElements && effective is JArray effectiveElements)
                {

                    var own        = (stored as JArray ?? []).Select(element => KeyOf(property, element)).ToHashSet();
                    var given      = effectiveElements.Select(element => KeyOf(property, element)).Where(key => !own.Contains(key)).ToHashSet();
                    var remaining  = desiredElements.Where (element => !given.Contains(KeyOf(property, element))).
                                                     Select(element => element.DeepClone()).
                                                     ToArray();

                    if (remaining.Length > 0)
                        Desired[property] = new JArray(remaining);
                    else
                        Desired.Remove(property);

                }

            }

        }

        #endregion

    }

}
