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
    /// Extension methods for parking operator admin status types.
    /// </summary>
    public static class ParkingOperatorAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this parking operator admin status types is null or empty.
        /// </summary>
        /// <param name="ParkingOperatorAdminStatusType">A parking operator admin status type.</param>
        public static Boolean IsNullOrEmpty(this ParkingOperatorAdminStatusType? ParkingOperatorAdminStatusType)
            => !ParkingOperatorAdminStatusType.HasValue || ParkingOperatorAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking operator admin status types is null or empty.
        /// </summary>
        /// <param name="ParkingOperatorAdminStatusType">A parking operator admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingOperatorAdminStatusType? ParkingOperatorAdminStatusType)
            => ParkingOperatorAdminStatusType.HasValue && ParkingOperatorAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a parking operator.
    /// </summary>
    public readonly struct ParkingOperatorAdminStatusType : IId,
                                                                    IEquatable<ParkingOperatorAdminStatusType>,
                                                                    IComparable<ParkingOperatorAdminStatusType>
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
        /// The length of the parking operator admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking operator admin status type based on the given string.
        /// </summary>
        private ParkingOperatorAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a parking operator admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking operator admin status type.</param>
        public static ParkingOperatorAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out ParkingOperatorAdminStatusType parkingOperatorAdminStatusType))
                return parkingOperatorAdminStatusType;

            throw new ArgumentException($"Invalid text representation of a parking operator admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a parking operator admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking operator admin status type.</param>
        public static ParkingOperatorAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ParkingOperatorAdminStatusType parkingOperatorAdminStatusType))
                return parkingOperatorAdminStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingOperatorAdminStatusType)

        /// <summary>
        /// Parse the given string as a parking operator admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking operator admin status type.</param>
        /// <param name="ParkingOperatorAdminStatusType">The parsed parking operator admin status type.</param>
        public static Boolean TryParse(String Text, out ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingOperatorAdminStatusType = new ParkingOperatorAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingOperatorAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking operator admin status type.
        /// </summary>
        public ParkingOperatorAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the parking operator.
        /// </summary>
        public static readonly ParkingOperatorAdminStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear admin status of the parking operator.
        /// </summary>
        public static readonly ParkingOperatorAdminStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The parking operator is planned for the future.
        /// </summary>
        public static readonly ParkingOperatorAdminStatusType Planned      = new ("planned");

        /// <summary>
        /// The parking operator is being deployed, not fully operational yet.
        /// </summary>
        public static readonly ParkingOperatorAdminStatusType InDeployment = new ("inDeployment");

        /// <summary>
        /// The parking operator is operational.
        /// </summary>
        public static readonly ParkingOperatorAdminStatusType Operational  = new ("operational");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly ParkingOperatorAdminStatusType InternalUse  = new ("internalUse");

        /// <summary>
        /// The parking operator is under maintenance.
        /// </summary>
        public static readonly ParkingOperatorAdminStatusType OutOfService = new ("outOfService");

        #endregion


        #region Operator overloading

        #region Operator == (ParkingOperatorAdminStatusType1, ParkingOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingOperatorAdminStatusType1">A parking operator admin status type.</param>
        /// <param name="ParkingOperatorAdminStatusType2">Another parking operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType1,
                                           ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType2)

            => ParkingOperatorAdminStatusType1.Equals(ParkingOperatorAdminStatusType2);

        #endregion

        #region Operator != (ParkingOperatorAdminStatusType1, ParkingOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingOperatorAdminStatusType1">A parking operator admin status type.</param>
        /// <param name="ParkingOperatorAdminStatusType2">Another parking operator admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType1,
                                           ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType2)

            => !ParkingOperatorAdminStatusType1.Equals(ParkingOperatorAdminStatusType2);

        #endregion

        #region Operator <  (ParkingOperatorAdminStatusType1, ParkingOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingOperatorAdminStatusType1">A parking operator admin status type.</param>
        /// <param name="ParkingOperatorAdminStatusType2">Another parking operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType1,
                                          ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType2)

            => ParkingOperatorAdminStatusType1.CompareTo(ParkingOperatorAdminStatusType2) < 0;

        #endregion

        #region Operator <= (ParkingOperatorAdminStatusType1, ParkingOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingOperatorAdminStatusType1">A parking operator admin status type.</param>
        /// <param name="ParkingOperatorAdminStatusType2">Another parking operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType1,
                                           ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType2)

            => ParkingOperatorAdminStatusType1.CompareTo(ParkingOperatorAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (ParkingOperatorAdminStatusType1, ParkingOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingOperatorAdminStatusType1">A parking operator admin status type.</param>
        /// <param name="ParkingOperatorAdminStatusType2">Another parking operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType1,
                                          ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType2)

            => ParkingOperatorAdminStatusType1.CompareTo(ParkingOperatorAdminStatusType2) > 0;

        #endregion

        #region Operator >= (ParkingOperatorAdminStatusType1, ParkingOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingOperatorAdminStatusType1">A parking operator admin status type.</param>
        /// <param name="ParkingOperatorAdminStatusType2">Another parking operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType1,
                                           ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType2)

            => ParkingOperatorAdminStatusType1.CompareTo(ParkingOperatorAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingOperatorAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking operator admin status types.
        /// </summary>
        /// <param name="Object">A parking operator admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingOperatorAdminStatusType parkingOperatorAdminStatusType
                   ? CompareTo(parkingOperatorAdminStatusType)
                   : throw new ArgumentException("The given object is not a parking operator admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingOperatorAdminStatusType)

        /// <summary>
        /// Compares two parking operator admin status types.
        /// </summary>
        /// <param name="ParkingOperatorAdminStatusType">A parking operator admin status type to compare with.</param>
        public Int32 CompareTo(ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType)

            => String.Compare(InternalId,
                              ParkingOperatorAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingOperatorAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking operator admin status types for equality.
        /// </summary>
        /// <param name="Object">A parking operator admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingOperatorAdminStatusType parkingOperatorAdminStatusType &&
                   Equals(parkingOperatorAdminStatusType);

        #endregion

        #region Equals(ParkingOperatorAdminStatusType)

        /// <summary>
        /// Compares two parking operator admin status types for equality.
        /// </summary>
        /// <param name="ParkingOperatorAdminStatusType">A parking operator admin status type to compare with.</param>
        public Boolean Equals(ParkingOperatorAdminStatusType ParkingOperatorAdminStatusType)

            => String.Equals(InternalId,
                             ParkingOperatorAdminStatusType.InternalId,
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
