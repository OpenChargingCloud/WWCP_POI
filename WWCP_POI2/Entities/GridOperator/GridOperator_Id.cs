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

using System;
using System.Text.RegularExpressions;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The unique identification of an Electric Vehicle Service Provider (EVSP Id).
    /// </summary>
    public sealed class GridOperator_Id : IId,
                                        IEquatable <GridOperator_Id>,
                                        IComparable<GridOperator_Id>

    {

        #region Data

        /// <summary>
        /// The regular expression for parsing a grid operator identification:
        /// the ISO 15118 form "DE*ABC" or "DEABC"; the obsolete DIN "+49*822" is no POI identification.
        /// </summary>
        public static readonly Regex  OperatorId_RegEx  = new (@"^([A-Z]{2})\*?([A-Z0-9]{3})$");

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
        public Boolean IsNullOrEmpty
            => Suffix.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => Suffix.IsNotNullOrEmpty();

        /// <summary>
        /// Returns the length of the identification.
        /// </summary>
        public UInt64 Length
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

            #region Initial checks

            if (Suffix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Suffix),  "The grid operator identification suffix must not be null or empty!");

            #endregion

            this.CountryCode  = CountryCode;
            this.Suffix       = Suffix;

        }

        #endregion


        #region Parse(Text)

        /// <summary>
        /// Parse the given text representation of a grid operator identification.
        /// </summary>
        /// <param name="Text">A text representation of a grid operator identification.</param>
        public static GridOperator_Id Parse(String Text)
        {

            #region Initial checks

            if (Text.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Text), "The given text representation of a grid operator identification must not be null or empty!");

            #endregion

            var MatchCollection = OperatorId_RegEx.Matches(Text);

            if (MatchCollection.Count != 1)
                throw new ArgumentException($"Illegal text representation of a grid operator identification: '{Text}'!",
                                            nameof(Text));

            if (Country.TryParseAlpha2Code(MatchCollection[0].Groups[1].Value, out var countryCode))
                return new GridOperator_Id(countryCode,
                                           MatchCollection[0].Groups[2].Value);

            throw new ArgumentException($"Unknown country code in the grid operator identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region Parse(CountryCode, Suffix)

        /// <summary>
        /// Parse the given string as a grid operator identification.
        /// </summary>
        /// <param name="CountryCode">A country code.</param>
        /// <param name="Suffix">The suffix of a grid operator identification.</param>
        public static GridOperator_Id Parse(Country  CountryCode,
                                            String   Suffix)
        {

            #region Initial checks

            if (CountryCode is null)
                throw new ArgumentNullException(nameof(CountryCode),  "The given country must not be null!");

            if (Suffix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Suffix),       "The given grid operator identification suffix must not be null or empty!");

            #endregion

            return Parse(CountryCode.Alpha2Code + "*" + Suffix);

        }

        #endregion

        #region TryParse(Text, out GridOperatorId)

        /// <summary>
        /// Try to parse the given text representation of a grid operator identification.
        /// </summary>
        /// <param name="Text">A text representation of a grid operator identification.</param>
        /// <param name="GridOperatorId">The parsed grid operator identification.</param>
        public static Boolean TryParse(String               Text,
                                       out GridOperator_Id  GridOperatorId)
        {

            GridOperatorId = default!;

            if (Text.IsNullOrEmpty())
                return false;

            var MatchCollection = OperatorId_RegEx.Matches(Text);

            if (MatchCollection.Count != 1 ||
                !Country.TryParseAlpha2Code(MatchCollection[0].Groups[1].Value, out var countryCode))
                return false;

            GridOperatorId = new GridOperator_Id(countryCode,
                                                 MatchCollection[0].Groups[2].Value);

            return true;

        }

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

        #region Operator == (ChargingStationOperatorId1, ChargingStationOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationOperatorId1">A charging station operator identification.</param>
        /// <param name="ChargingStationOperatorId2">Another charging station operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (GridOperator_Id ChargingStationOperatorId1, GridOperator_Id ChargingStationOperatorId2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ChargingStationOperatorId1, ChargingStationOperatorId2))
                return true;

            // If one is null, but not both, return false.
            if (((Object) ChargingStationOperatorId1 is null) || ((Object) ChargingStationOperatorId2 is null))
                return false;

            return ChargingStationOperatorId1.Equals(ChargingStationOperatorId2);

        }

        #endregion

        #region Operator != (ChargingStationOperatorId1, ChargingStationOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationOperatorId1">A charging station operator identification.</param>
        /// <param name="ChargingStationOperatorId2">Another charging station operator identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (GridOperator_Id ChargingStationOperatorId1, GridOperator_Id ChargingStationOperatorId2)
        {
            return !(ChargingStationOperatorId1 == ChargingStationOperatorId2);
        }

        #endregion

        #region Operator <  (ChargingStationOperatorId1, ChargingStationOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationOperatorId1">A charging station operator identification.</param>
        /// <param name="ChargingStationOperatorId2">Another charging station operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (GridOperator_Id ChargingStationOperatorId1, GridOperator_Id ChargingStationOperatorId2)
        {

            if ((Object) ChargingStationOperatorId1 is null)
                throw new ArgumentNullException("The given ChargingStationOperatorId1 must not be null!");

            return ChargingStationOperatorId1.CompareTo(ChargingStationOperatorId2) < 0;

        }

        #endregion

        #region Operator <= (ChargingStationOperatorId1, ChargingStationOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationOperatorId1">A charging station operator identification.</param>
        /// <param name="ChargingStationOperatorId2">Another charging station operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (GridOperator_Id ChargingStationOperatorId1, GridOperator_Id ChargingStationOperatorId2)
        {
            return !(ChargingStationOperatorId1 > ChargingStationOperatorId2);
        }

        #endregion

        #region Operator >  (ChargingStationOperatorId1, ChargingStationOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationOperatorId1">A charging station operator identification.</param>
        /// <param name="ChargingStationOperatorId2">Another charging station operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (GridOperator_Id ChargingStationOperatorId1, GridOperator_Id ChargingStationOperatorId2)
        {

            if ((Object) ChargingStationOperatorId1 is null)
                throw new ArgumentNullException("The given ChargingStationOperatorId1 must not be null!");

            return ChargingStationOperatorId1.CompareTo(ChargingStationOperatorId2) > 0;

        }

        #endregion

        #region Operator >= (ChargingStationOperatorId1, ChargingStationOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationOperatorId1">A charging station operator identification.</param>
        /// <param name="ChargingStationOperatorId2">Another charging station operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (GridOperator_Id ChargingStationOperatorId1, GridOperator_Id ChargingStationOperatorId2)
        {
            return !(ChargingStationOperatorId1 < ChargingStationOperatorId2);
        }

        #endregion

        #endregion

        #region IComparable<ChargingStationOperatorId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public Int32 CompareTo(Object Object)
        {

            if (Object is null)
                throw new ArgumentNullException(nameof(Object), "The given object must not be null!");

            if (!(Object is GridOperator_Id))
                throw new ArgumentException("The given object is not a charging station operator identification!", nameof(Object));

            return CompareTo((GridOperator_Id) Object);

        }

        #endregion

        #region CompareTo(ChargingStationOperatorId)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationOperatorId">An object to compare with.</param>
        public Int32 CompareTo(GridOperator_Id ChargingStationOperatorId)
        {

            if ((Object) ChargingStationOperatorId is null)
                throw new ArgumentNullException(nameof(ChargingStationOperatorId), "The given charging station operator identification must not be null!");

            // Compare the length of the ChargingStationOperatorIds
            var _Result = Length.CompareTo(ChargingStationOperatorId.Length);

            // If equal: Compare country codes
            if (_Result == 0)
                _Result = CountryCode.CompareTo(ChargingStationOperatorId.CountryCode);

            // If equal: Compare operator ids
            if (_Result == 0)
                _Result = String.Compare(Suffix, ChargingStationOperatorId.Suffix, StringComparison.Ordinal);

            return _Result;

        }

        #endregion

        #endregion

        #region IEquatable<ChargingStationOperatorId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public override Boolean Equals(Object Object)
        {

            if (Object is null)
                return false;

            if (!(Object is GridOperator_Id))
                return false;

            return this.Equals((GridOperator_Id) Object);

        }

        #endregion

        #region Equals(ChargingStationOperatorId)

        /// <summary>
        /// Compares two ChargingStationOperatorIds for equality.
        /// </summary>
        /// <param name="ChargingStationOperatorId">A ChargingStationOperatorId to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(GridOperator_Id ChargingStationOperatorId)
        {

            if ((Object) ChargingStationOperatorId is null)
                return false;

            return CountryCode.Equals(ChargingStationOperatorId.CountryCode) &&
                   Suffix.     Equals(ChargingStationOperatorId.Suffix);

        }

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => CountryCode.GetHashCode() ^
               Suffix.     GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object: always the ISO 15118 form with a '*' separator.
        /// </summary>
        public override String ToString()

            => String.Concat(CountryCode?.Alpha2Code, "*", Suffix ?? "");

        #endregion

    }

}
