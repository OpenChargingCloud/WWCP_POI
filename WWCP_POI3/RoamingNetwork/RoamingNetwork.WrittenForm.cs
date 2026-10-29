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

using System.Globalization;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The written form of a version: what a roaming network writes again once it has read it, which is how the content
    /// holds it (SPECIFICATION.md, section 2, "Written forms").
    /// </summary>
    /// <remarks>
    /// A version read in another form, e.g. its payment options in another order, a timestamp with an offset, a language
    /// in upper case, a list without elements or a property it does not know, would be written in its own: another text
    /// than the one its ETags were calculated over by whoever wrote it. It is refused, as a new value of a change set given
    /// otherwise than the content holds it is (TryHeldAsGiven), and in the words of WWCP_POI.TS, which refuses it as well.
    /// </remarks>
    public sealed partial class RoamingNetwork
    {

        #region (private static) Data

        /// <summary>
        /// The texts in languages of the content, as ImmutableI18NString writes them: each language by its name in lower case.
        /// </summary>
        private static readonly HashSet<String> textsInLanguages = [ "name", "description", "comment", "assistanceServiceDetails", "arrivalInstructions", "additionalInformation" ];

        /// <summary>
        /// A timestamp as the content holds it: UTC to the millisecond.
        /// </summary>
        private static readonly Regex heldTimestamp = new (@"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}Z\z", RegexOptions.CultureInvariant);

        #endregion


        #region (private static) NotWrittenAsHeld(Given, Written)

        /// <summary>
        /// The error, when the given JSON of a roaming network is not the one it writes again once it has read it, its ETags
        /// and, when the given JSON names none, its JSON-LD context aside; null when it is.
        /// </summary>
        /// <param name="Given">The JSON of a roaming network as it was given.</param>
        /// <param name="Written">The JSON the roaming network read from it writes, without its ETags.</param>
        private static String? NotWrittenAsHeld(JObject  Given,
                                                JObject  Written)

            // Its ETags are checked against its content, and its JSON-LD context may be left out.
            => FirstNotWritten(Given,
                               Written,
                               "",
                               null,
                               Given[BasePropertyKeys.Context] is null
                                   ? [ BasePropertyKeys.ETags, BasePropertyKeys.Context ]
                                   : [ BasePropertyKeys.ETags ]) is (String path, String reason)

                   ? $"'{path}' is not written as the content holds it: {reason}!"
                   : null;

        #endregion

        #region (private static) FirstNotWritten(Given, Written, Path, Property)

        /// <summary>
        /// Where the given value is not written as the content holds it, e.g. "chargingStationOperators[0].chargingPools[1].paymentOptions",
        /// and how the content holds it; null when it is.
        /// </summary>
        /// <param name="Given">The value as it was given.</param>
        /// <param name="Written">The value as it is written again.</param>
        /// <param name="Path">The path of the value.</param>
        /// <param name="Property">The name of the property holding the value, if a property holds it.</param>
        /// <param name="Ignored">The properties of the value compared neither given nor written, if it is an object.</param>
        private static (String Path, String Reason)? FirstNotWritten(JToken                Given,
                                                                     JToken                Written,
                                                                     String                Path,
                                                                     String?               Property,
                                                                     HashSet<String>?      Ignored   = null)
        {

            switch (Given)
            {

                case JObject givenObject when Written is JObject writtenObject:

                    if (Property is not null && textsInLanguages.Contains(Property) &&
                        !givenObject.Properties().Select(property => property.Name).Order(StringComparer.Ordinal).
                             SequenceEqual(writtenObject.Properties().Select(property => property.Name).Order(StringComparer.Ordinal)))
                    {
                        return (Path, "a text in languages names each language as Styx writes it, in lower case, with a text");
                    }

                    foreach (var property in givenObject.Properties().Where(property => Ignored?.Contains(property.Name) != true))
                    {

                        var at = Path.Length == 0 ? property.Name : $"{Path}.{property.Name}";

                        if (writtenObject[property.Name] is not JToken written)
                        {

                            if (property.Name == BasePropertyKeys.Context)
                                return (Path, "embedded, an object names no JSON-LD context");

                            if (property.Value.Type == JTokenType.Null)
                                return (at,   "a property without a value is left out");

                            if (property.Value is JArray { Count: 0 })
                                return (at,   "a list without elements is left out");

                            return (at, "the content leaves it out");

                        }

                        if (FirstNotWritten(property.Value, written, at, property.Name) is (String, String) found)
                            return found;

                    }

                    foreach (var property in writtenObject.Properties().Where(property => Ignored?.Contains(property.Name) != true))
                        if (givenObject[property.Name] is null)
                            return (Path.Length == 0 ? property.Name : $"{Path}.{property.Name}",
                                    $"the content holds it as '{POIJSON.ContentText(property.Value)}'");

                    return null;


                case JArray givenArray when Written is JArray writtenArray:

                    // The texts of an ordered list trimmed, as its type writes them.
                    if (givenArray. All(element => element.Type == JTokenType.String) &&
                        writtenArray.All(element => element.Type == JTokenType.String) &&
                        givenArray. Any(element => element.Value<String>()!.Trim() != element.Value<String>()) &&
                        givenArray. Select(element => element.Value<String>()!.Trim()).ToHashSet(StringComparer.Ordinal).
                            SetEquals(writtenArray.Select(element => element.Value<String>()!)))
                    {
                        return (Path, "the texts of its elements are trimmed");
                    }

                    // Its elements by their identification, others by what they are.
                    var givenKeys    = givenArray.  Select(ElementKey).ToArray();
                    var writtenKeys  = writtenArray.Select(ElementKey).ToArray();

                    if (!givenKeys.SequenceEqual(writtenKeys, StringComparer.Ordinal))
                    {

                        if (givenKeys.Order(StringComparer.Ordinal).SequenceEqual(writtenKeys.Order(StringComparer.Ordinal), StringComparer.Ordinal))
                            return (Path, "its elements are in the order the content holds them in");

                        if (givenKeys.Length > writtenKeys.Length &&
                            givenKeys.ToHashSet(StringComparer.Ordinal).SetEquals(writtenKeys))
                        {
                            return (Path, "its elements are in the order the content holds them in, each once");
                        }

                        if (givenKeys.Length != writtenKeys.Length)
                            return (Path, $"the content holds it as '{POIJSON.ContentText(writtenArray)}'");

                    }

                    for (var index = 0; index < givenArray.Count; index++)
                        if (FirstNotWritten(givenArray[index], writtenArray[index], $"{Path}[{index}]", null) is (String, String) found)
                            return found;

                    return null;


                case JValue { Type: JTokenType.String } givenText when Written is JValue { Type: JTokenType.String } writtenText &&
                                                                     givenText.Value<String>() != writtenText.Value<String>() &&
                                                                     heldTimestamp.IsMatch(writtenText.Value<String>()!):

                    return (Path, "a timestamp is UTC to the millisecond, yyyy-MM-ddTHH:mm:ss.fffZ");


                // Read by a reader making dates of texts, e.g. JObject.Parse(), not by POIJSON.Read(), a timestamp has lost
                // how it was written: it is compared by the instant it names.
                case JValue { Type: JTokenType.Date } givenDate when Written is JValue { Type: JTokenType.String } writtenText &&
                                                                   DateTimeOffset.TryParse(writtenText.Value<String>(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var writtenTimestamp):

                    var givenTimestamp = givenDate.Value switch {
                                             DateTimeOffset timestamp => timestamp,
                                             DateTime       timestamp => new DateTimeOffset(timestamp.Kind == DateTimeKind.Unspecified
                                                                                                ? DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
                                                                                                : timestamp),
                                             _                        => (DateTimeOffset?) null
                                         };

                    return givenTimestamp?.UtcTicks == writtenTimestamp.UtcTicks
                               ? null
                               : (Path, $"the content holds it as '{POIJSON.ContentText(Written)}'");


                default:
                    return POIJSON.SameContent(Given, Written)
                               ? null
                               : (Path, $"the content holds it as '{POIJSON.ContentText(Written)}'");

            }

        }

        #endregion

        #region (private static) ElementKey(Element)

        /// <summary>
        /// What tells the given element of a list from the others: its identification, if it has one, what it is otherwise.
        /// </summary>
        /// <param name="Element">An element of a list.</param>
        private static String ElementKey(JToken Element)

            => Element is JObject { } element && element[BasePropertyKeys.Id] is JValue { Type: JTokenType.String } id
                   ? "@id " + id.Value<String>()
                   : POIJSON.ContentText(Element);

        #endregion

    }

}
