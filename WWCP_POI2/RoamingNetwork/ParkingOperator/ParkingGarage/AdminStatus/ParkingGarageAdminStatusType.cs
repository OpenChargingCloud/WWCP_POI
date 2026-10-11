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
    /// Extension methods for parking garage admin status types.
    /// </summary>
    public static class ParkingGarageAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this parking garage admin status types is null or empty.
        /// </summary>
        /// <param name="ParkingGarageAdminStatusType">A parking garage admin status type.</param>
        public static Boolean IsNullOrEmpty(this ParkingGarageAdminStatusType? ParkingGarageAdminStatusType)
            => !ParkingGarageAdminStatusType.HasValue || ParkingGarageAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking garage admin status types is null or empty.
        /// </summary>
        /// <param name="ParkingGarageAdminStatusType">A parking garage admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingGarageAdminStatusType? ParkingGarageAdminStatusType)
            => ParkingGarageAdminStatusType.HasValue && ParkingGarageAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a parking garage.
    /// </summary>
    public readonly struct ParkingGarageAdminStatusType : IId,
                                                                    IEquatable<ParkingGarageAdminStatusType>,
                                                                    IComparable<ParkingGarageAdminStatusType>
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
        /// The length of the parking garage admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking garage admin status type based on the given string.
        /// </summary>
        private ParkingGarageAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a parking garage admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking garage admin status type.</param>
        public static ParkingGarageAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out ParkingGarageAdminStatusType parkingGarageAdminStatusType))
                return parkingGarageAdminStatusType;

            throw new ArgumentException($"Invalid text representation of a parking garage admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a parking garage admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking garage admin status type.</param>
        public static ParkingGarageAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ParkingGarageAdminStatusType parkingGarageAdminStatusType))
                return parkingGarageAdminStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingGarageAdminStatusType)

        /// <summary>
        /// Parse the given string as a parking garage admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking garage admin status type.</param>
        /// <param name="ParkingGarageAdminStatusType">The parsed parking garage admin status type.</param>
        public static Boolean TryParse(String Text, out ParkingGarageAdminStatusType ParkingGarageAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingGarageAdminStatusType = new ParkingGarageAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingGarageAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking garage admin status type.
        /// </summary>
        public ParkingGarageAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the parking garage.
        /// </summary>
        public static readonly ParkingGarageAdminStatusType Unknown       = new ("unknown");

        /// <summary>
        /// Unclear admin status of the parking garage.
        /// </summary>
        public static readonly ParkingGarageAdminStatusType Unspecified   = new ("unspecified");

        /// <summary>
        /// The parking garage is under maintenance.
        /// </summary>
        public static readonly ParkingGarageAdminStatusType OutOfService  = new ("outOfService");

        /// <summary>
        /// The parking garage is operational.
        /// </summary>
        public static readonly ParkingGarageAdminStatusType Operational   = new ("operational");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly ParkingGarageAdminStatusType InternalUse   = new ("internalUse");

        #endregion


        #region Operator overloading

        #region Operator == (ParkingGarageAdminStatusType1, ParkingGarageAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingGarageAdminStatusType1">A parking garage admin status type.</param>
        /// <param name="ParkingGarageAdminStatusType2">Another parking garage admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingGarageAdminStatusType ParkingGarageAdminStatusType1,
                                           ParkingGarageAdminStatusType ParkingGarageAdminStatusType2)

            => ParkingGarageAdminStatusType1.Equals(ParkingGarageAdminStatusType2);

        #endregion

        #region Operator != (ParkingGarageAdminStatusType1, ParkingGarageAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingGarageAdminStatusType1">A parking garage admin status type.</param>
        /// <param name="ParkingGarageAdminStatusType2">Another parking garage admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingGarageAdminStatusType ParkingGarageAdminStatusType1,
                                           ParkingGarageAdminStatusType ParkingGarageAdminStatusType2)

            => !ParkingGarageAdminStatusType1.Equals(ParkingGarageAdminStatusType2);

        #endregion

        #region Operator <  (ParkingGarageAdminStatusType1, ParkingGarageAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingGarageAdminStatusType1">A parking garage admin status type.</param>
        /// <param name="ParkingGarageAdminStatusType2">Another parking garage admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingGarageAdminStatusType ParkingGarageAdminStatusType1,
                                          ParkingGarageAdminStatusType ParkingGarageAdminStatusType2)

            => ParkingGarageAdminStatusType1.CompareTo(ParkingGarageAdminStatusType2) < 0;

        #endregion

        #region Operator <= (ParkingGarageAdminStatusType1, ParkingGarageAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingGarageAdminStatusType1">A parking garage admin status type.</param>
        /// <param name="ParkingGarageAdminStatusType2">Another parking garage admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingGarageAdminStatusType ParkingGarageAdminStatusType1,
                                           ParkingGarageAdminStatusType ParkingGarageAdminStatusType2)

            => ParkingGarageAdminStatusType1.CompareTo(ParkingGarageAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (ParkingGarageAdminStatusType1, ParkingGarageAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingGarageAdminStatusType1">A parking garage admin status type.</param>
        /// <param name="ParkingGarageAdminStatusType2">Another parking garage admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingGarageAdminStatusType ParkingGarageAdminStatusType1,
                                          ParkingGarageAdminStatusType ParkingGarageAdminStatusType2)

            => ParkingGarageAdminStatusType1.CompareTo(ParkingGarageAdminStatusType2) > 0;

        #endregion

        #region Operator >= (ParkingGarageAdminStatusType1, ParkingGarageAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingGarageAdminStatusType1">A parking garage admin status type.</param>
        /// <param name="ParkingGarageAdminStatusType2">Another parking garage admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingGarageAdminStatusType ParkingGarageAdminStatusType1,
                                           ParkingGarageAdminStatusType ParkingGarageAdminStatusType2)

            => ParkingGarageAdminStatusType1.CompareTo(ParkingGarageAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingGarageAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking garage admin status types.
        /// </summary>
        /// <param name="Object">A parking garage admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingGarageAdminStatusType parkingGarageAdminStatusType
                   ? CompareTo(parkingGarageAdminStatusType)
                   : throw new ArgumentException("The given object is not a parking garage admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingGarageAdminStatusType)

        /// <summary>
        /// Compares two parking garage admin status types.
        /// </summary>
        /// <param name="ParkingGarageAdminStatusType">A parking garage admin status type to compare with.</param>
        public Int32 CompareTo(ParkingGarageAdminStatusType ParkingGarageAdminStatusType)

            => String.Compare(InternalId,
                              ParkingGarageAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingGarageAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking garage admin status types for equality.
        /// </summary>
        /// <param name="Object">A parking garage admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingGarageAdminStatusType parkingGarageAdminStatusType &&
                   Equals(parkingGarageAdminStatusType);

        #endregion

        #region Equals(ParkingGarageAdminStatusType)

        /// <summary>
        /// Compares two parking garage admin status types for equality.
        /// </summary>
        /// <param name="ParkingGarageAdminStatusType">A parking garage admin status type to compare with.</param>
        public Boolean Equals(ParkingGarageAdminStatusType ParkingGarageAdminStatusType)

            => String.Equals(InternalId,
                             ParkingGarageAdminStatusType.InternalId,
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
