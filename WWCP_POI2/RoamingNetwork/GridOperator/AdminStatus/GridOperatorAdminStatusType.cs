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
    /// Extension methods for grid operator admin status types.
    /// </summary>
    public static class GridOperatorAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this grid operator admin status types is null or empty.
        /// </summary>
        /// <param name="GridOperatorAdminStatusType">A grid operator admin status type.</param>
        public static Boolean IsNullOrEmpty(this GridOperatorAdminStatusType? GridOperatorAdminStatusType)
            => !GridOperatorAdminStatusType.HasValue || GridOperatorAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this grid operator admin status types is null or empty.
        /// </summary>
        /// <param name="GridOperatorAdminStatusType">A grid operator admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this GridOperatorAdminStatusType? GridOperatorAdminStatusType)
            => GridOperatorAdminStatusType.HasValue && GridOperatorAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a grid operator.
    /// </summary>
    public readonly struct GridOperatorAdminStatusType : IId,
                                                                    IEquatable<GridOperatorAdminStatusType>,
                                                                    IComparable<GridOperatorAdminStatusType>
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
        /// The length of the grid operator admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new grid operator admin status type based on the given string.
        /// </summary>
        private GridOperatorAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a grid operator admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a grid operator admin status type.</param>
        public static GridOperatorAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out GridOperatorAdminStatusType gridOperatorAdminStatusType))
                return gridOperatorAdminStatusType;

            throw new ArgumentException($"Invalid text representation of a grid operator admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a grid operator admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a grid operator admin status type.</param>
        public static GridOperatorAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out GridOperatorAdminStatusType gridOperatorAdminStatusType))
                return gridOperatorAdminStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out GridOperatorAdminStatusType)

        /// <summary>
        /// Parse the given string as a grid operator admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a grid operator admin status type.</param>
        /// <param name="GridOperatorAdminStatusType">The parsed grid operator admin status type.</param>
        public static Boolean TryParse(String Text, out GridOperatorAdminStatusType GridOperatorAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    GridOperatorAdminStatusType = new GridOperatorAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            GridOperatorAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this grid operator admin status type.
        /// </summary>
        public GridOperatorAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the grid operator.
        /// </summary>
        public static readonly GridOperatorAdminStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear admin status of the grid operator.
        /// </summary>
        public static readonly GridOperatorAdminStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The grid operator is planned for the future.
        /// </summary>
        public static readonly GridOperatorAdminStatusType Planned      = new ("planned");

        /// <summary>
        /// The grid operator is being deployed, not fully operational yet.
        /// </summary>
        public static readonly GridOperatorAdminStatusType InDeployment = new ("inDeployment");

        /// <summary>
        /// The grid operator is operational.
        /// </summary>
        public static readonly GridOperatorAdminStatusType Operational  = new ("operational");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly GridOperatorAdminStatusType InternalUse  = new ("internalUse");

        /// <summary>
        /// The grid operator is under maintenance.
        /// </summary>
        public static readonly GridOperatorAdminStatusType OutOfService = new ("outOfService");

        #endregion


        #region Operator overloading

        #region Operator == (GridOperatorAdminStatusType1, GridOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorAdminStatusType1">A grid operator admin status type.</param>
        /// <param name="GridOperatorAdminStatusType2">Another grid operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (GridOperatorAdminStatusType GridOperatorAdminStatusType1,
                                           GridOperatorAdminStatusType GridOperatorAdminStatusType2)

            => GridOperatorAdminStatusType1.Equals(GridOperatorAdminStatusType2);

        #endregion

        #region Operator != (GridOperatorAdminStatusType1, GridOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorAdminStatusType1">A grid operator admin status type.</param>
        /// <param name="GridOperatorAdminStatusType2">Another grid operator admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (GridOperatorAdminStatusType GridOperatorAdminStatusType1,
                                           GridOperatorAdminStatusType GridOperatorAdminStatusType2)

            => !GridOperatorAdminStatusType1.Equals(GridOperatorAdminStatusType2);

        #endregion

        #region Operator <  (GridOperatorAdminStatusType1, GridOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorAdminStatusType1">A grid operator admin status type.</param>
        /// <param name="GridOperatorAdminStatusType2">Another grid operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (GridOperatorAdminStatusType GridOperatorAdminStatusType1,
                                          GridOperatorAdminStatusType GridOperatorAdminStatusType2)

            => GridOperatorAdminStatusType1.CompareTo(GridOperatorAdminStatusType2) < 0;

        #endregion

        #region Operator <= (GridOperatorAdminStatusType1, GridOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorAdminStatusType1">A grid operator admin status type.</param>
        /// <param name="GridOperatorAdminStatusType2">Another grid operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (GridOperatorAdminStatusType GridOperatorAdminStatusType1,
                                           GridOperatorAdminStatusType GridOperatorAdminStatusType2)

            => GridOperatorAdminStatusType1.CompareTo(GridOperatorAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (GridOperatorAdminStatusType1, GridOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorAdminStatusType1">A grid operator admin status type.</param>
        /// <param name="GridOperatorAdminStatusType2">Another grid operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (GridOperatorAdminStatusType GridOperatorAdminStatusType1,
                                          GridOperatorAdminStatusType GridOperatorAdminStatusType2)

            => GridOperatorAdminStatusType1.CompareTo(GridOperatorAdminStatusType2) > 0;

        #endregion

        #region Operator >= (GridOperatorAdminStatusType1, GridOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GridOperatorAdminStatusType1">A grid operator admin status type.</param>
        /// <param name="GridOperatorAdminStatusType2">Another grid operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (GridOperatorAdminStatusType GridOperatorAdminStatusType1,
                                           GridOperatorAdminStatusType GridOperatorAdminStatusType2)

            => GridOperatorAdminStatusType1.CompareTo(GridOperatorAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<GridOperatorAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two grid operator admin status types.
        /// </summary>
        /// <param name="Object">A grid operator admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is GridOperatorAdminStatusType gridOperatorAdminStatusType
                   ? CompareTo(gridOperatorAdminStatusType)
                   : throw new ArgumentException("The given object is not a grid operator admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(GridOperatorAdminStatusType)

        /// <summary>
        /// Compares two grid operator admin status types.
        /// </summary>
        /// <param name="GridOperatorAdminStatusType">A grid operator admin status type to compare with.</param>
        public Int32 CompareTo(GridOperatorAdminStatusType GridOperatorAdminStatusType)

            => String.Compare(InternalId,
                              GridOperatorAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<GridOperatorAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two grid operator admin status types for equality.
        /// </summary>
        /// <param name="Object">A grid operator admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is GridOperatorAdminStatusType gridOperatorAdminStatusType &&
                   Equals(gridOperatorAdminStatusType);

        #endregion

        #region Equals(GridOperatorAdminStatusType)

        /// <summary>
        /// Compares two grid operator admin status types for equality.
        /// </summary>
        /// <param name="GridOperatorAdminStatusType">A grid operator admin status type to compare with.</param>
        public Boolean Equals(GridOperatorAdminStatusType GridOperatorAdminStatusType)

            => String.Equals(InternalId,
                             GridOperatorAdminStatusType.InternalId,
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
