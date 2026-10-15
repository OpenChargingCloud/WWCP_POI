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
    /// Extension methods for parking space group admin status types.
    /// </summary>
    public static class ParkingSpaceGroupAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this parking space group admin status types is null or empty.
        /// </summary>
        /// <param name="ParkingSpaceGroupAdminStatusType">A parking space group admin status type.</param>
        public static Boolean IsNullOrEmpty(this ParkingSpaceGroupAdminStatusType? ParkingSpaceGroupAdminStatusType)
            => !ParkingSpaceGroupAdminStatusType.HasValue || ParkingSpaceGroupAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking space group admin status types is null or empty.
        /// </summary>
        /// <param name="ParkingSpaceGroupAdminStatusType">A parking space group admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingSpaceGroupAdminStatusType? ParkingSpaceGroupAdminStatusType)
            => ParkingSpaceGroupAdminStatusType.HasValue && ParkingSpaceGroupAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a parking space group.
    /// </summary>
    public readonly struct ParkingSpaceGroupAdminStatusType : IId,
                                                                    IEquatable<ParkingSpaceGroupAdminStatusType>,
                                                                    IComparable<ParkingSpaceGroupAdminStatusType>
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
        /// The length of the parking space group admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking space group admin status type based on the given string.
        /// </summary>
        private ParkingSpaceGroupAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a parking space group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking space group admin status type.</param>
        public static ParkingSpaceGroupAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out ParkingSpaceGroupAdminStatusType parkingSpaceGroupAdminStatusType))
                return parkingSpaceGroupAdminStatusType;

            throw new ArgumentException($"Invalid text representation of a parking space group admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a parking space group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking space group admin status type.</param>
        public static ParkingSpaceGroupAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ParkingSpaceGroupAdminStatusType parkingSpaceGroupAdminStatusType))
                return parkingSpaceGroupAdminStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingSpaceGroupAdminStatusType)

        /// <summary>
        /// Parse the given string as a parking space group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking space group admin status type.</param>
        /// <param name="ParkingSpaceGroupAdminStatusType">The parsed parking space group admin status type.</param>
        public static Boolean TryParse(String Text, out ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingSpaceGroupAdminStatusType = new ParkingSpaceGroupAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingSpaceGroupAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking space group admin status type.
        /// </summary>
        public ParkingSpaceGroupAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the parking space group.
        /// </summary>
        public static readonly ParkingSpaceGroupAdminStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear admin status of the parking space group.
        /// </summary>
        public static readonly ParkingSpaceGroupAdminStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The parking space group is planned for the future.
        /// </summary>
        public static readonly ParkingSpaceGroupAdminStatusType Planned      = new ("planned");

        /// <summary>
        /// The parking space group is being deployed, not fully operational yet.
        /// </summary>
        public static readonly ParkingSpaceGroupAdminStatusType InDeployment = new ("inDeployment");

        /// <summary>
        /// The parking space group is operational.
        /// </summary>
        public static readonly ParkingSpaceGroupAdminStatusType Operational  = new ("operational");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly ParkingSpaceGroupAdminStatusType InternalUse  = new ("internalUse");

        /// <summary>
        /// The parking space group is under maintenance.
        /// </summary>
        public static readonly ParkingSpaceGroupAdminStatusType OutOfService = new ("outOfService");

        #endregion


        #region Operator overloading

        #region Operator == (ParkingSpaceGroupAdminStatusType1, ParkingSpaceGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceGroupAdminStatusType1">A parking space group admin status type.</param>
        /// <param name="ParkingSpaceGroupAdminStatusType2">Another parking space group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType1,
                                           ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType2)

            => ParkingSpaceGroupAdminStatusType1.Equals(ParkingSpaceGroupAdminStatusType2);

        #endregion

        #region Operator != (ParkingSpaceGroupAdminStatusType1, ParkingSpaceGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceGroupAdminStatusType1">A parking space group admin status type.</param>
        /// <param name="ParkingSpaceGroupAdminStatusType2">Another parking space group admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType1,
                                           ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType2)

            => !ParkingSpaceGroupAdminStatusType1.Equals(ParkingSpaceGroupAdminStatusType2);

        #endregion

        #region Operator <  (ParkingSpaceGroupAdminStatusType1, ParkingSpaceGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceGroupAdminStatusType1">A parking space group admin status type.</param>
        /// <param name="ParkingSpaceGroupAdminStatusType2">Another parking space group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType1,
                                          ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType2)

            => ParkingSpaceGroupAdminStatusType1.CompareTo(ParkingSpaceGroupAdminStatusType2) < 0;

        #endregion

        #region Operator <= (ParkingSpaceGroupAdminStatusType1, ParkingSpaceGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceGroupAdminStatusType1">A parking space group admin status type.</param>
        /// <param name="ParkingSpaceGroupAdminStatusType2">Another parking space group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType1,
                                           ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType2)

            => ParkingSpaceGroupAdminStatusType1.CompareTo(ParkingSpaceGroupAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (ParkingSpaceGroupAdminStatusType1, ParkingSpaceGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceGroupAdminStatusType1">A parking space group admin status type.</param>
        /// <param name="ParkingSpaceGroupAdminStatusType2">Another parking space group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType1,
                                          ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType2)

            => ParkingSpaceGroupAdminStatusType1.CompareTo(ParkingSpaceGroupAdminStatusType2) > 0;

        #endregion

        #region Operator >= (ParkingSpaceGroupAdminStatusType1, ParkingSpaceGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceGroupAdminStatusType1">A parking space group admin status type.</param>
        /// <param name="ParkingSpaceGroupAdminStatusType2">Another parking space group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType1,
                                           ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType2)

            => ParkingSpaceGroupAdminStatusType1.CompareTo(ParkingSpaceGroupAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingSpaceGroupAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking space group admin status types.
        /// </summary>
        /// <param name="Object">A parking space group admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingSpaceGroupAdminStatusType parkingSpaceGroupAdminStatusType
                   ? CompareTo(parkingSpaceGroupAdminStatusType)
                   : throw new ArgumentException("The given object is not a parking space group admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingSpaceGroupAdminStatusType)

        /// <summary>
        /// Compares two parking space group admin status types.
        /// </summary>
        /// <param name="ParkingSpaceGroupAdminStatusType">A parking space group admin status type to compare with.</param>
        public Int32 CompareTo(ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType)

            => String.Compare(InternalId,
                              ParkingSpaceGroupAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingSpaceGroupAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking space group admin status types for equality.
        /// </summary>
        /// <param name="Object">A parking space group admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSpaceGroupAdminStatusType parkingSpaceGroupAdminStatusType &&
                   Equals(parkingSpaceGroupAdminStatusType);

        #endregion

        #region Equals(ParkingSpaceGroupAdminStatusType)

        /// <summary>
        /// Compares two parking space group admin status types for equality.
        /// </summary>
        /// <param name="ParkingSpaceGroupAdminStatusType">A parking space group admin status type to compare with.</param>
        public Boolean Equals(ParkingSpaceGroupAdminStatusType ParkingSpaceGroupAdminStatusType)

            => String.Equals(InternalId,
                             ParkingSpaceGroupAdminStatusType.InternalId,
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
