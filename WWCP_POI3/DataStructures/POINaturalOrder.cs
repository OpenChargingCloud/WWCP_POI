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
    /// The natural order of identifications, as a reader expects a list: a run of digits by the number it is, every
    /// other character ordinally - P1, P2, P10, not P1, P10, P2.
    /// </summary>
    /// <remarks>
    /// For lists to read only. The canonical JSON, and so every ETag, keeps the ordinal order: what is signed and
    /// compared does not depend on how numbers are read. Two identifications of the same numbers, e.g. P01 and P1,
    /// are in their ordinal order, so the order is total.
    /// </remarks>
    public sealed class POINaturalOrder : IComparer<String>
    {

        /// <summary>
        /// The natural order.
        /// </summary>
        public static POINaturalOrder Instance { get; } = new ();

        private POINaturalOrder()
        { }

        /// <summary>
        /// Compare the given texts in their natural order.
        /// </summary>
        public Int32 Compare(String? Text1,
                             String? Text2)
        {

            if (ReferenceEquals(Text1, Text2))
                return 0;

            if (Text1 is null)
                return -1;

            if (Text2 is null)
                return 1;

            var at1 = 0;
            var at2 = 0;

            while (at1 < Text1.Length && at2 < Text2.Length)
            {

                if (Char.IsAsciiDigit(Text1[at1]) && Char.IsAsciiDigit(Text2[at2]))
                {

                    var start1 = at1;
                    var start2 = at2;

                    while (at1 < Text1.Length && Char.IsAsciiDigit(Text1[at1])) at1++;
                    while (at2 < Text2.Length && Char.IsAsciiDigit(Text2[at2])) at2++;

                    // The numbers without their leading zeros: the longer one is the larger, else digit by digit.
                    var number1 = Text1.AsSpan(start1, at1 - start1).TrimStart('0');
                    var number2 = Text2.AsSpan(start2, at2 - start2).TrimStart('0');

                    if (number1.Length != number2.Length)
                        return number1.Length.CompareTo(number2.Length);

                    var byDigits = number1.SequenceCompareTo(number2);
                    if (byDigits != 0)
                        return byDigits;

                    continue;

                }

                var byCharacter = Text1[at1].CompareTo(Text2[at2]);
                if (byCharacter != 0)
                    return byCharacter;

                at1++;
                at2++;

            }

            if (at1 < Text1.Length)
                return 1;

            if (at2 < Text2.Length)
                return -1;

            return String.CompareOrdinal(Text1, Text2);

        }

    }

}
