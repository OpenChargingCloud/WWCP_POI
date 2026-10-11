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
    /// Extension methods for parking space admin status types.
    /// </summary>
    public static class ParkingSpaceAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this parking space admin status types is null or empty.
        /// </summary>
        /// <param name="ParkingSpaceAdminStatusType">A parking space admin status type.</param>
        public static Boolean IsNullOrEmpty(this ParkingSpaceAdminStatusType? ParkingSpaceAdminStatusType)
            => !ParkingSpaceAdminStatusType.HasValue || ParkingSpaceAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking space admin status types is null or empty.
        /// </summary>
        /// <param name="ParkingSpaceAdminStatusType">A parking space admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingSpaceAdminStatusType? ParkingSpaceAdminStatusType)
            => ParkingSpaceAdminStatusType.HasValue && ParkingSpaceAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a parking space.
    /// </summary>
    public readonly struct ParkingSpaceAdminStatusType : IId,
                                                                    IEquatable<ParkingSpaceAdminStatusType>,
                                                                    IComparable<ParkingSpaceAdminStatusType>
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
        /// The length of the parking space admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking space admin status type based on the given string.
        /// </summary>
        private ParkingSpaceAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a parking space admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking space admin status type.</param>
        public static ParkingSpaceAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out ParkingSpaceAdminStatusType parkingSpaceAdminStatusType))
                return parkingSpaceAdminStatusType;

            throw new ArgumentException($"Invalid text representation of a parking space admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a parking space admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking space admin status type.</param>
        public static ParkingSpaceAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ParkingSpaceAdminStatusType parkingSpaceAdminStatusType))
                return parkingSpaceAdminStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingSpaceAdminStatusType)

        /// <summary>
        /// Parse the given string as a parking space admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking space admin status type.</param>
        /// <param name="ParkingSpaceAdminStatusType">The parsed parking space admin status type.</param>
        public static Boolean TryParse(String Text, out ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingSpaceAdminStatusType = new ParkingSpaceAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingSpaceAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking space admin status type.
        /// </summary>
        public ParkingSpaceAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the parking space.
        /// </summary>
        public static readonly ParkingSpaceAdminStatusType Unknown       = new ("unknown");

        /// <summary>
        /// Unclear admin status of the parking space.
        /// </summary>
        public static readonly ParkingSpaceAdminStatusType Unspecified   = new ("unspecified");

        /// <summary>
        /// The parking space is under maintenance.
        /// </summary>
        public static readonly ParkingSpaceAdminStatusType OutOfService  = new ("outOfService");

        /// <summary>
        /// The parking space is operational.
        /// </summary>
        public static readonly ParkingSpaceAdminStatusType Operational   = new ("operational");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly ParkingSpaceAdminStatusType InternalUse   = new ("internalUse");

        #endregion


        #region Operator overloading

        #region Operator == (ParkingSpaceAdminStatusType1, ParkingSpaceAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceAdminStatusType1">A parking space admin status type.</param>
        /// <param name="ParkingSpaceAdminStatusType2">Another parking space admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType1,
                                           ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType2)

            => ParkingSpaceAdminStatusType1.Equals(ParkingSpaceAdminStatusType2);

        #endregion

        #region Operator != (ParkingSpaceAdminStatusType1, ParkingSpaceAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceAdminStatusType1">A parking space admin status type.</param>
        /// <param name="ParkingSpaceAdminStatusType2">Another parking space admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType1,
                                           ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType2)

            => !ParkingSpaceAdminStatusType1.Equals(ParkingSpaceAdminStatusType2);

        #endregion

        #region Operator <  (ParkingSpaceAdminStatusType1, ParkingSpaceAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceAdminStatusType1">A parking space admin status type.</param>
        /// <param name="ParkingSpaceAdminStatusType2">Another parking space admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType1,
                                          ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType2)

            => ParkingSpaceAdminStatusType1.CompareTo(ParkingSpaceAdminStatusType2) < 0;

        #endregion

        #region Operator <= (ParkingSpaceAdminStatusType1, ParkingSpaceAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceAdminStatusType1">A parking space admin status type.</param>
        /// <param name="ParkingSpaceAdminStatusType2">Another parking space admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType1,
                                           ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType2)

            => ParkingSpaceAdminStatusType1.CompareTo(ParkingSpaceAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (ParkingSpaceAdminStatusType1, ParkingSpaceAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceAdminStatusType1">A parking space admin status type.</param>
        /// <param name="ParkingSpaceAdminStatusType2">Another parking space admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType1,
                                          ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType2)

            => ParkingSpaceAdminStatusType1.CompareTo(ParkingSpaceAdminStatusType2) > 0;

        #endregion

        #region Operator >= (ParkingSpaceAdminStatusType1, ParkingSpaceAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceAdminStatusType1">A parking space admin status type.</param>
        /// <param name="ParkingSpaceAdminStatusType2">Another parking space admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType1,
                                           ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType2)

            => ParkingSpaceAdminStatusType1.CompareTo(ParkingSpaceAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingSpaceAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking space admin status types.
        /// </summary>
        /// <param name="Object">A parking space admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingSpaceAdminStatusType parkingSpaceAdminStatusType
                   ? CompareTo(parkingSpaceAdminStatusType)
                   : throw new ArgumentException("The given object is not a parking space admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingSpaceAdminStatusType)

        /// <summary>
        /// Compares two parking space admin status types.
        /// </summary>
        /// <param name="ParkingSpaceAdminStatusType">A parking space admin status type to compare with.</param>
        public Int32 CompareTo(ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType)

            => String.Compare(InternalId,
                              ParkingSpaceAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingSpaceAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking space admin status types for equality.
        /// </summary>
        /// <param name="Object">A parking space admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSpaceAdminStatusType parkingSpaceAdminStatusType &&
                   Equals(parkingSpaceAdminStatusType);

        #endregion

        #region Equals(ParkingSpaceAdminStatusType)

        /// <summary>
        /// Compares two parking space admin status types for equality.
        /// </summary>
        /// <param name="ParkingSpaceAdminStatusType">A parking space admin status type to compare with.</param>
        public Boolean Equals(ParkingSpaceAdminStatusType ParkingSpaceAdminStatusType)

            => String.Equals(InternalId,
                             ParkingSpaceAdminStatusType.InternalId,
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
