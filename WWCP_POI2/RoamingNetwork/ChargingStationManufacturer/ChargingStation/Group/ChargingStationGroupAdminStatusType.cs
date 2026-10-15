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
    /// Extension methods for charging station group admin status types.
    /// </summary>
    public static class ChargingStationGroupAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this charging station group admin status types is null or empty.
        /// </summary>
        /// <param name="ChargingStationGroupAdminStatusType">A charging station group admin status type.</param>
        public static Boolean IsNullOrEmpty(this ChargingStationGroupAdminStatusType? ChargingStationGroupAdminStatusType)
            => !ChargingStationGroupAdminStatusType.HasValue || ChargingStationGroupAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this charging station group admin status types is null or empty.
        /// </summary>
        /// <param name="ChargingStationGroupAdminStatusType">A charging station group admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this ChargingStationGroupAdminStatusType? ChargingStationGroupAdminStatusType)
            => ChargingStationGroupAdminStatusType.HasValue && ChargingStationGroupAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a charging station group.
    /// </summary>
    public readonly struct ChargingStationGroupAdminStatusType : IId,
                                                                    IEquatable<ChargingStationGroupAdminStatusType>,
                                                                    IComparable<ChargingStationGroupAdminStatusType>
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
        /// The length of the charging station group admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station group admin status type based on the given string.
        /// </summary>
        private ChargingStationGroupAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a charging station group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging station group admin status type.</param>
        public static ChargingStationGroupAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out ChargingStationGroupAdminStatusType chargingStationGroupAdminStatusType))
                return chargingStationGroupAdminStatusType;

            throw new ArgumentException($"Invalid text representation of a charging station group admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a charging station group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging station group admin status type.</param>
        public static ChargingStationGroupAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ChargingStationGroupAdminStatusType chargingStationGroupAdminStatusType))
                return chargingStationGroupAdminStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ChargingStationGroupAdminStatusType)

        /// <summary>
        /// Parse the given string as a charging station group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging station group admin status type.</param>
        /// <param name="ChargingStationGroupAdminStatusType">The parsed charging station group admin status type.</param>
        public static Boolean TryParse(String Text, out ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChargingStationGroupAdminStatusType = new ChargingStationGroupAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ChargingStationGroupAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging station group admin status type.
        /// </summary>
        public ChargingStationGroupAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the charging station group.
        /// </summary>
        public static readonly ChargingStationGroupAdminStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear admin status of the charging station group.
        /// </summary>
        public static readonly ChargingStationGroupAdminStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The charging station group is planned for the future.
        /// </summary>
        public static readonly ChargingStationGroupAdminStatusType Planned      = new ("planned");

        /// <summary>
        /// The charging station group is being deployed, not fully operational yet.
        /// </summary>
        public static readonly ChargingStationGroupAdminStatusType InDeployment = new ("inDeployment");

        /// <summary>
        /// The charging station group is operational.
        /// </summary>
        public static readonly ChargingStationGroupAdminStatusType Operational  = new ("operational");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly ChargingStationGroupAdminStatusType InternalUse  = new ("internalUse");

        /// <summary>
        /// The charging station group is under maintenance.
        /// </summary>
        public static readonly ChargingStationGroupAdminStatusType OutOfService = new ("outOfService");

        #endregion


        #region Operator overloading

        #region Operator == (ChargingStationGroupAdminStatusType1, ChargingStationGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationGroupAdminStatusType1">A charging station group admin status type.</param>
        /// <param name="ChargingStationGroupAdminStatusType2">Another charging station group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType1,
                                           ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType2)

            => ChargingStationGroupAdminStatusType1.Equals(ChargingStationGroupAdminStatusType2);

        #endregion

        #region Operator != (ChargingStationGroupAdminStatusType1, ChargingStationGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationGroupAdminStatusType1">A charging station group admin status type.</param>
        /// <param name="ChargingStationGroupAdminStatusType2">Another charging station group admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType1,
                                           ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType2)

            => !ChargingStationGroupAdminStatusType1.Equals(ChargingStationGroupAdminStatusType2);

        #endregion

        #region Operator <  (ChargingStationGroupAdminStatusType1, ChargingStationGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationGroupAdminStatusType1">A charging station group admin status type.</param>
        /// <param name="ChargingStationGroupAdminStatusType2">Another charging station group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType1,
                                          ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType2)

            => ChargingStationGroupAdminStatusType1.CompareTo(ChargingStationGroupAdminStatusType2) < 0;

        #endregion

        #region Operator <= (ChargingStationGroupAdminStatusType1, ChargingStationGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationGroupAdminStatusType1">A charging station group admin status type.</param>
        /// <param name="ChargingStationGroupAdminStatusType2">Another charging station group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType1,
                                           ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType2)

            => ChargingStationGroupAdminStatusType1.CompareTo(ChargingStationGroupAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (ChargingStationGroupAdminStatusType1, ChargingStationGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationGroupAdminStatusType1">A charging station group admin status type.</param>
        /// <param name="ChargingStationGroupAdminStatusType2">Another charging station group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType1,
                                          ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType2)

            => ChargingStationGroupAdminStatusType1.CompareTo(ChargingStationGroupAdminStatusType2) > 0;

        #endregion

        #region Operator >= (ChargingStationGroupAdminStatusType1, ChargingStationGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationGroupAdminStatusType1">A charging station group admin status type.</param>
        /// <param name="ChargingStationGroupAdminStatusType2">Another charging station group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType1,
                                           ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType2)

            => ChargingStationGroupAdminStatusType1.CompareTo(ChargingStationGroupAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingStationGroupAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging station group admin status types.
        /// </summary>
        /// <param name="Object">A charging station group admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingStationGroupAdminStatusType chargingStationGroupAdminStatusType
                   ? CompareTo(chargingStationGroupAdminStatusType)
                   : throw new ArgumentException("The given object is not a charging station group admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingStationGroupAdminStatusType)

        /// <summary>
        /// Compares two charging station group admin status types.
        /// </summary>
        /// <param name="ChargingStationGroupAdminStatusType">A charging station group admin status type to compare with.</param>
        public Int32 CompareTo(ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType)

            => String.Compare(InternalId,
                              ChargingStationGroupAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChargingStationGroupAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging station group admin status types for equality.
        /// </summary>
        /// <param name="Object">A charging station group admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingStationGroupAdminStatusType chargingStationGroupAdminStatusType &&
                   Equals(chargingStationGroupAdminStatusType);

        #endregion

        #region Equals(ChargingStationGroupAdminStatusType)

        /// <summary>
        /// Compares two charging station group admin status types for equality.
        /// </summary>
        /// <param name="ChargingStationGroupAdminStatusType">A charging station group admin status type to compare with.</param>
        public Boolean Equals(ChargingStationGroupAdminStatusType ChargingStationGroupAdminStatusType)

            => String.Equals(InternalId,
                             ChargingStationGroupAdminStatusType.InternalId,
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
