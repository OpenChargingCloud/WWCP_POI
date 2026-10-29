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

using System.Text.RegularExpressions;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Extension methods for grid operator identifications.
    /// </summary>
    public static class GridOperatorIdExtensions
    {

    }


    /// <summary>
    /// The unique identification of a grid operator.
    /// </summary>
    public readonly struct GridOperator_Id : IId<GridOperator_Id>
    {

        #region Data

        /// <summary>
        /// The regular expression for parsing a grid operator identification.
        /// </summary>
        public static readonly Regex  GridOperatorId_RegEx = new (@"^([A-Z]{2})\*?([A-Z0-9]{3})\z");

        #endregion

        #region Properties

        /// <summary>
        /// The country code.
        /// </summary>
        public Country            CountryCode   { get; }

        /// <summary>
        /// The identifier suffix.
        /// </summary>
        public String             Suffix        { get; }

        /// <summary>
        /// Indicates whether this identification is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => Suffix.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this identification is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => Suffix.IsNotNullOrEmpty();

        /// <summary>
        /// Returns the length of the identification.
        /// </summary>
        public UInt64   Length

            => (UInt64) (CountryCode.Alpha2Code.Length + 1 + Suffix.Length);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new grid operator identification.
        /// </summary>
        /// <param name="CountryCode">The country code.</param>
        /// <param name="Suffix">The suffix of the grid operator identification.</param>
        private GridOperator_Id(Country  CountryCode,
                                           String   Suffix)
        {

            this.CountryCode  = CountryCode;
            this.Suffix       = Suffix;

        }

        #endregion


        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given text representation of a grid operator identification.
        /// </summary>
        /// <param name="Text">A text representation of a grid operator identification.</param>
        public static GridOperator_Id Parse(String Text)
        {

            if (TryParse(Text, out var gridOperatorId))
                return gridOperatorId;

            throw new ArgumentException($"Invalid text representation of a grid operator identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) Parse    (CountryCode, Suffix)

        /// <summary>
        /// Parse the given string as an grid operator identification.
        /// </summary>
        /// <param name="CountryCode">A country code.</param>
        /// <param name="Suffix">The suffix of an grid operator identification.</param>
        public static GridOperator_Id Parse(Country  CountryCode,
                                                       String   Suffix)

            => Parse(
                   String.Concat(
                       CountryCode.Alpha2Code,
                       "*",
                       Suffix.Trim()
                   )
               );

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text representation of a grid operator identification.
        /// </summary>
        /// <param name="Text">A text representation of a grid operator identification.</param>
        public static GridOperator_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var gridOperatorId))
                return gridOperatorId;

            return null;

        }

        #endregion

        #region (static) TryParse (CountryCode, Suffix)

        /// <summary>
        /// Try to parse the given text representation of a grid operator identification.
        /// </summary>
        /// <param name="CountryCode">A country code.</param>
        /// <param name="Suffix">The suffix of an grid operator identification.</param>
        public static GridOperator_Id? TryParse(Country  CountryCode,
                                                           String   Suffix)

            => TryParse(String.Concat(CountryCode.Alpha2Code, "*", Suffix.Trim()));

        #endregion

        #region (static) TryParse (Text,                out GridOperatorId)

        /// <summary>
        /// Try to parse the given text representation of a grid operator identification.
        /// </summary>
        /// <param name="Text">A text representation of a grid operator identification.</param>
        /// <param name="GridOperatorId">The parsed grid operator identification.</param>
        public static Boolean TryParse(String                          Text,
                                       out GridOperator_Id  GridOperatorId)
        {

            #region Initial checks

            GridOperatorId = default;

            if (Text is null)
                return false;

            Text = Text.Trim();

            if (Text.IsNullOrEmpty())
                return false;

            #endregion

            try
            {

                var matchCollection = GridOperatorId_RegEx.Matches(Text.ToUpperInvariant());

                if (matchCollection.Count != 1)
                    return false;

                if (Country.TryParseAlpha2Code(matchCollection[0].Groups[1].Value, out var countryCode))
                {

                    GridOperatorId = new (countryCode,
                                                     matchCollection[0].Groups[2].Value);

                    return true;

                }

                return false;

            }

            catch
            { }

            return false;

        }

        #endregion

        #region (static) TryParse (CountryCode, Suffix, out GridOperatorId)

        /// <summary>
        /// Try to parse the given text representation of a grid operator identification.
        /// </summary>
        /// <param name="CountryCode">A country code.</param>
        /// <param name="Suffix">The suffix of an grid operator identification.</param>
        /// <param name="GridOperatorId">The parsed grid operator identification.</param>
        public static Boolean TryParse(Country                         CountryCode,
                                       String                          Suffix,
                                       out GridOperator_Id  GridOperatorId)

            => TryParse(
                   String.Concat(
                       CountryCode.Alpha2Code,
                       "*",
                       Suffix.Trim()
                   ),
                   out GridOperatorId
               );

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this grid operator identification.
        /// </summary>
        public GridOperator_Id Clone()

            => new (
                   CountryCode.Clone(),
                   Suffix.     CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (GridOperatorId1, GridOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorId1">A grid operator identification.</param>
        /// <param name="GridOperatorId2">Another grid operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (GridOperator_Id GridOperatorId1,
                                           GridOperator_Id GridOperatorId2)

            => GridOperatorId1.Equals(GridOperatorId2);

        #endregion

        #region Operator != (GridOperatorId1, GridOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorId1">A grid operator identification.</param>
        /// <param name="GridOperatorId2">Another grid operator identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (GridOperator_Id GridOperatorId1,
                                           GridOperator_Id GridOperatorId2)

            => !GridOperatorId1.Equals(GridOperatorId2);

        #endregion

        #region Operator <  (GridOperatorId1, GridOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorId1">A grid operator identification.</param>
        /// <param name="GridOperatorId2">Another grid operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (GridOperator_Id GridOperatorId1,
                                          GridOperator_Id GridOperatorId2)

            => GridOperatorId1.CompareTo(GridOperatorId2) < 0;

        #endregion

        #region Operator <= (GridOperatorId1, GridOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorId1">A grid operator identification.</param>
        /// <param name="GridOperatorId2">Another grid operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (GridOperator_Id GridOperatorId1,
                                           GridOperator_Id GridOperatorId2)

            => GridOperatorId1.CompareTo(GridOperatorId2) <= 0;

        #endregion

        #region Operator >  (GridOperatorId1, GridOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorId1">A grid operator identification.</param>
        /// <param name="GridOperatorId2">Another grid operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (GridOperator_Id GridOperatorId1,
                                          GridOperator_Id GridOperatorId2)

            => GridOperatorId1.CompareTo(GridOperatorId2) > 0;

        #endregion

        #region Operator >= (GridOperatorId1, GridOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorId1">A grid operator identification.</param>
        /// <param name="GridOperatorId2">Another grid operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (GridOperator_Id GridOperatorId1,
                                           GridOperator_Id GridOperatorId2)

            => GridOperatorId1.CompareTo(GridOperatorId2) >= 0;

        #endregion

        #endregion

        #region IComparable<GridOperatorId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two grid operator identifications.
        /// </summary>
        /// <param name="Object">A grid operator identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is GridOperator_Id gridOperatorId
                   ? CompareTo(gridOperatorId)
                   : throw new ArgumentException("The given object is not a grid operator identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(GridOperatorId)

        /// <summary>
        /// Compares two grid operator identifications.
        /// </summary>
        /// <param name="GridOperatorId">A grid operator identification to compare with.</param>
        public Int32 CompareTo(GridOperator_Id GridOperatorId)
        {

            var c = CountryCode.CompareTo(GridOperatorId.CountryCode);

            if (c == 0)
                c = String.Compare(Suffix,
                                   GridOperatorId.Suffix,
                                   StringComparison.OrdinalIgnoreCase);

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<GridOperatorId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two grid operator identifications for equality.
        /// </summary>
        /// <param name="Object">A grid operator identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is GridOperator_Id gridOperatorId &&
                   Equals(gridOperatorId);

        #endregion

        #region Equals(GridOperatorId)

        /// <summary>
        /// Compares two grid operator identifications for equality.
        /// </summary>
        /// <param name="GridOperatorId">A grid operator identification to compare with.</param>
        public Boolean Equals(GridOperator_Id GridOperatorId)

            => (CountryCode is null
                    ? GridOperatorId.CountryCode is null
                    : CountryCode.Equals(GridOperatorId.CountryCode)) &&

               String.Equals(Suffix,
                             GridOperatorId.Suffix,
                             StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => (CountryCode?.GetHashCode() ?? 0) ^
               (Suffix is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Suffix));

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object: always the ISO 15118 form with a '*' separator.
        /// </summary>
        public override String ToString()

            => String.Concat(
                   CountryCode?.Alpha2Code,
                   "*",
                   Suffix ?? ""
               );

        #endregion

    }

}
