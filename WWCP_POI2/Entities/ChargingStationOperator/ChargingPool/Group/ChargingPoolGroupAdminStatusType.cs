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
    /// Extension methods for charging pool group admin status types.
    /// </summary>
    public static class ChargingPoolGroupAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this charging pool group admin status types is null or empty.
        /// </summary>
        /// <param name="ChargingPoolGroupAdminStatusType">A charging pool group admin status type.</param>
        public static Boolean IsNullOrEmpty(this ChargingPoolGroupAdminStatusType? ChargingPoolGroupAdminStatusType)
            => !ChargingPoolGroupAdminStatusType.HasValue || ChargingPoolGroupAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this charging pool group admin status types is null or empty.
        /// </summary>
        /// <param name="ChargingPoolGroupAdminStatusType">A charging pool group admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this ChargingPoolGroupAdminStatusType? ChargingPoolGroupAdminStatusType)
            => ChargingPoolGroupAdminStatusType.HasValue && ChargingPoolGroupAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a charging pool group.
    /// </summary>
    public readonly struct ChargingPoolGroupAdminStatusType : IId,
                                                                    IEquatable<ChargingPoolGroupAdminStatusType>,
                                                                    IComparable<ChargingPoolGroupAdminStatusType>
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
        /// The length of the charging pool group admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging pool group admin status type based on the given string.
        /// </summary>
        private ChargingPoolGroupAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a charging pool group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging pool group admin status type.</param>
        public static ChargingPoolGroupAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out ChargingPoolGroupAdminStatusType chargingPoolGroupAdminStatusType))
                return chargingPoolGroupAdminStatusType;

            throw new ArgumentException($"Invalid text representation of a charging pool group admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a charging pool group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging pool group admin status type.</param>
        public static ChargingPoolGroupAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ChargingPoolGroupAdminStatusType chargingPoolGroupAdminStatusType))
                return chargingPoolGroupAdminStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ChargingPoolGroupAdminStatusType)

        /// <summary>
        /// Parse the given string as a charging pool group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging pool group admin status type.</param>
        /// <param name="ChargingPoolGroupAdminStatusType">The parsed charging pool group admin status type.</param>
        public static Boolean TryParse(String Text, out ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChargingPoolGroupAdminStatusType = new ChargingPoolGroupAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ChargingPoolGroupAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging pool group admin status type.
        /// </summary>
        public ChargingPoolGroupAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the charging pool group.
        /// </summary>
        public static readonly ChargingPoolGroupAdminStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear admin status of the charging pool group.
        /// </summary>
        public static readonly ChargingPoolGroupAdminStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The charging pool group is under maintenance.
        /// </summary>
        public static readonly ChargingPoolGroupAdminStatusType OutOfService = new ("outOfService");

        /// <summary>
        /// The charging pool group is operational.
        /// </summary>
        public static readonly ChargingPoolGroupAdminStatusType Operational  = new ("operational");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly ChargingPoolGroupAdminStatusType InternalUse  = new ("internalUse");

        #endregion


        #region Operator overloading

        #region Operator == (ChargingPoolGroupAdminStatusType1, ChargingPoolGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupAdminStatusType1">A charging pool group admin status type.</param>
        /// <param name="ChargingPoolGroupAdminStatusType2">Another charging pool group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType1,
                                           ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType2)

            => ChargingPoolGroupAdminStatusType1.Equals(ChargingPoolGroupAdminStatusType2);

        #endregion

        #region Operator != (ChargingPoolGroupAdminStatusType1, ChargingPoolGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupAdminStatusType1">A charging pool group admin status type.</param>
        /// <param name="ChargingPoolGroupAdminStatusType2">Another charging pool group admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType1,
                                           ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType2)

            => !ChargingPoolGroupAdminStatusType1.Equals(ChargingPoolGroupAdminStatusType2);

        #endregion

        #region Operator <  (ChargingPoolGroupAdminStatusType1, ChargingPoolGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupAdminStatusType1">A charging pool group admin status type.</param>
        /// <param name="ChargingPoolGroupAdminStatusType2">Another charging pool group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType1,
                                          ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType2)

            => ChargingPoolGroupAdminStatusType1.CompareTo(ChargingPoolGroupAdminStatusType2) < 0;

        #endregion

        #region Operator <= (ChargingPoolGroupAdminStatusType1, ChargingPoolGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupAdminStatusType1">A charging pool group admin status type.</param>
        /// <param name="ChargingPoolGroupAdminStatusType2">Another charging pool group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType1,
                                           ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType2)

            => ChargingPoolGroupAdminStatusType1.CompareTo(ChargingPoolGroupAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (ChargingPoolGroupAdminStatusType1, ChargingPoolGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupAdminStatusType1">A charging pool group admin status type.</param>
        /// <param name="ChargingPoolGroupAdminStatusType2">Another charging pool group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType1,
                                          ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType2)

            => ChargingPoolGroupAdminStatusType1.CompareTo(ChargingPoolGroupAdminStatusType2) > 0;

        #endregion

        #region Operator >= (ChargingPoolGroupAdminStatusType1, ChargingPoolGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupAdminStatusType1">A charging pool group admin status type.</param>
        /// <param name="ChargingPoolGroupAdminStatusType2">Another charging pool group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType1,
                                           ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType2)

            => ChargingPoolGroupAdminStatusType1.CompareTo(ChargingPoolGroupAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingPoolGroupAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging pool group admin status types.
        /// </summary>
        /// <param name="Object">A charging pool group admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingPoolGroupAdminStatusType chargingPoolGroupAdminStatusType
                   ? CompareTo(chargingPoolGroupAdminStatusType)
                   : throw new ArgumentException("The given object is not a charging pool group admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingPoolGroupAdminStatusType)

        /// <summary>
        /// Compares two charging pool group admin status types.
        /// </summary>
        /// <param name="ChargingPoolGroupAdminStatusType">A charging pool group admin status type to compare with.</param>
        public Int32 CompareTo(ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType)

            => String.Compare(InternalId,
                              ChargingPoolGroupAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChargingPoolGroupAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging pool group admin status types for equality.
        /// </summary>
        /// <param name="Object">A charging pool group admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingPoolGroupAdminStatusType chargingPoolGroupAdminStatusType &&
                   Equals(chargingPoolGroupAdminStatusType);

        #endregion

        #region Equals(ChargingPoolGroupAdminStatusType)

        /// <summary>
        /// Compares two charging pool group admin status types for equality.
        /// </summary>
        /// <param name="ChargingPoolGroupAdminStatusType">A charging pool group admin status type to compare with.</param>
        public Boolean Equals(ChargingPoolGroupAdminStatusType ChargingPoolGroupAdminStatusType)

            => String.Equals(InternalId,
                             ChargingPoolGroupAdminStatusType.InternalId,
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
