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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Extension methods for grid operator status types.
    /// </summary>
    public static class GridOperatorStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this grid operator status types is null or empty.
        /// </summary>
        /// <param name="GridOperatorStatusType">A grid operator status type.</param>
        public static Boolean IsNullOrEmpty(this GridOperatorStatusType? GridOperatorStatusType)
            => !GridOperatorStatusType.HasValue || GridOperatorStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this grid operator status types is null or empty.
        /// </summary>
        /// <param name="GridOperatorStatusType">A grid operator status type.</param>
        public static Boolean IsNotNullOrEmpty(this GridOperatorStatusType? GridOperatorStatusType)
            => GridOperatorStatusType.HasValue && GridOperatorStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of a grid operator.
    /// </summary>
    public readonly struct GridOperatorStatusType : IId,
                                                               IEquatable<GridOperatorStatusType>,
                                                               IComparable<GridOperatorStatusType>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the grid operator status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new grid operator status type based on the given string.
        /// </summary>
        private GridOperatorStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a grid operator status type.
        /// </summary>
        /// <param name="Text">A text representation of a grid operator status type.</param>
        public static GridOperatorStatusType Parse(String Text)
        {

            if (TryParse(Text, out GridOperatorStatusType gridOperatorStatusType))
                return gridOperatorStatusType;

            throw new ArgumentException($"Invalid text representation of a grid operator status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a grid operator status type.
        /// </summary>
        /// <param name="Text">A text representation of a grid operator status type.</param>
        public static GridOperatorStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out GridOperatorStatusType gridOperatorStatusType))
                return gridOperatorStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out GridOperatorStatusType)

        /// <summary>
        /// Parse the given string as a grid operator status type.
        /// </summary>
        /// <param name="Text">A text representation of a grid operator status type.</param>
        /// <param name="GridOperatorStatusType">The parsed grid operator status type.</param>
        public static Boolean TryParse(String Text, out GridOperatorStatusType GridOperatorStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    GridOperatorStatusType = new GridOperatorStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            GridOperatorStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this grid operator status type.
        /// </summary>
        public GridOperatorStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the grid operator.
        /// </summary>
        public static readonly GridOperatorStatusType Unknown       = new("unknown");

        /// <summary>
        /// Unclear status of the grid operator.
        /// </summary>
        public static readonly GridOperatorStatusType Unspecified   = new("unspecified");

        /// <summary>
        /// The grid operator is currently offline.
        /// </summary>
        public static readonly GridOperatorStatusType Offline       = new("offline");

        /// <summary>
        /// The grid operator is not fully operational yet.
        /// </summary>
        public static readonly GridOperatorStatusType InDeployment  = new("inDeployment");

        /// <summary>
        /// The grid operator is available.
        /// </summary>
        public static readonly GridOperatorStatusType Available     = new("available");

        /// <summary>
        /// A fatal error has occurred within the grid operator.
        /// </summary>
        public static readonly GridOperatorStatusType Error         = new("error");

        #endregion


        #region Operator overloading

        #region Operator == (GridOperatorStatusType1, GridOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorStatusType1">A grid operator status type.</param>
        /// <param name="GridOperatorStatusType2">Another grid operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (GridOperatorStatusType GridOperatorStatusType1,
                                           GridOperatorStatusType GridOperatorStatusType2)

            => GridOperatorStatusType1.Equals(GridOperatorStatusType2);

        #endregion

        #region Operator != (GridOperatorStatusType1, GridOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorStatusType1">A grid operator status type.</param>
        /// <param name="GridOperatorStatusType2">Another grid operator status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (GridOperatorStatusType GridOperatorStatusType1,
                                           GridOperatorStatusType GridOperatorStatusType2)

            => !GridOperatorStatusType1.Equals(GridOperatorStatusType2);

        #endregion

        #region Operator <  (GridOperatorStatusType1, GridOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorStatusType1">A grid operator status type.</param>
        /// <param name="GridOperatorStatusType2">Another grid operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (GridOperatorStatusType GridOperatorStatusType1,
                                          GridOperatorStatusType GridOperatorStatusType2)

            => GridOperatorStatusType1.CompareTo(GridOperatorStatusType2) < 0;

        #endregion

        #region Operator <= (GridOperatorStatusType1, GridOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorStatusType1">A grid operator status type.</param>
        /// <param name="GridOperatorStatusType2">Another grid operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (GridOperatorStatusType GridOperatorStatusType1,
                                           GridOperatorStatusType GridOperatorStatusType2)

            => GridOperatorStatusType1.CompareTo(GridOperatorStatusType2) <= 0;

        #endregion

        #region Operator >  (GridOperatorStatusType1, GridOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorStatusType1">A grid operator status type.</param>
        /// <param name="GridOperatorStatusType2">Another grid operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (GridOperatorStatusType GridOperatorStatusType1,
                                          GridOperatorStatusType GridOperatorStatusType2)

            => GridOperatorStatusType1.CompareTo(GridOperatorStatusType2) > 0;

        #endregion

        #region Operator >= (GridOperatorStatusType1, GridOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorStatusType1">A grid operator status type.</param>
        /// <param name="GridOperatorStatusType2">Another grid operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (GridOperatorStatusType GridOperatorStatusType1,
                                           GridOperatorStatusType GridOperatorStatusType2)

            => GridOperatorStatusType1.CompareTo(GridOperatorStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<GridOperatorStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two grid operator status types.
        /// </summary>
        /// <param name="Object">A grid operator status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is GridOperatorStatusType gridOperatorStatusType
                   ? CompareTo(gridOperatorStatusType)
                   : throw new ArgumentException("The given object is not a grid operator status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(GridOperatorStatusType)

        /// <summary>
        /// Compares two grid operator status types.
        /// </summary>
        /// <param name="GridOperatorStatusType">A grid operator status type to compare with.</param>
        public Int32 CompareTo(GridOperatorStatusType GridOperatorStatusType)

            => String.Compare(InternalId,
                              GridOperatorStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<GridOperatorStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two grid operator status types for equality.
        /// </summary>
        /// <param name="Object">A grid operator status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is GridOperatorStatusType gridOperatorStatusType &&
                   Equals(gridOperatorStatusType);

        #endregion

        #region Equals(GridOperatorStatusType)

        /// <summary>
        /// Compares two grid operator status types for equality.
        /// </summary>
        /// <param name="GridOperatorStatusType">A grid operator status type to compare with.</param>
        public Boolean Equals(GridOperatorStatusType GridOperatorStatusType)

            => String.Equals(InternalId,
                             GridOperatorStatusType.InternalId,
                             StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => InternalId is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(InternalId);

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => InternalId ?? "";

        #endregion

    }

}
