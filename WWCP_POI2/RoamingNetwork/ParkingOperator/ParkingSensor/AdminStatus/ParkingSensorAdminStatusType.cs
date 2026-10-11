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
    /// Extension methods for parking sensor admin status types.
    /// </summary>
    public static class ParkingSensorAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this parking sensor admin status types is null or empty.
        /// </summary>
        /// <param name="ParkingSensorAdminStatusType">A parking sensor admin status type.</param>
        public static Boolean IsNullOrEmpty(this ParkingSensorAdminStatusType? ParkingSensorAdminStatusType)
            => !ParkingSensorAdminStatusType.HasValue || ParkingSensorAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking sensor admin status types is null or empty.
        /// </summary>
        /// <param name="ParkingSensorAdminStatusType">A parking sensor admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingSensorAdminStatusType? ParkingSensorAdminStatusType)
            => ParkingSensorAdminStatusType.HasValue && ParkingSensorAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a parking sensor.
    /// </summary>
    public readonly struct ParkingSensorAdminStatusType : IId,
                                                                    IEquatable<ParkingSensorAdminStatusType>,
                                                                    IComparable<ParkingSensorAdminStatusType>
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
        /// The length of the parking sensor admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking sensor admin status type based on the given string.
        /// </summary>
        private ParkingSensorAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a parking sensor admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking sensor admin status type.</param>
        public static ParkingSensorAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out ParkingSensorAdminStatusType parkingSensorAdminStatusType))
                return parkingSensorAdminStatusType;

            throw new ArgumentException($"Invalid text representation of a parking sensor admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a parking sensor admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking sensor admin status type.</param>
        public static ParkingSensorAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ParkingSensorAdminStatusType parkingSensorAdminStatusType))
                return parkingSensorAdminStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingSensorAdminStatusType)

        /// <summary>
        /// Parse the given string as a parking sensor admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking sensor admin status type.</param>
        /// <param name="ParkingSensorAdminStatusType">The parsed parking sensor admin status type.</param>
        public static Boolean TryParse(String Text, out ParkingSensorAdminStatusType ParkingSensorAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingSensorAdminStatusType = new ParkingSensorAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingSensorAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking sensor admin status type.
        /// </summary>
        public ParkingSensorAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the parking sensor.
        /// </summary>
        public static readonly ParkingSensorAdminStatusType Unknown       = new ("unknown");

        /// <summary>
        /// Unclear admin status of the parking sensor.
        /// </summary>
        public static readonly ParkingSensorAdminStatusType Unspecified   = new ("unspecified");

        /// <summary>
        /// The parking sensor is under maintenance.
        /// </summary>
        public static readonly ParkingSensorAdminStatusType OutOfService  = new ("outOfService");

        /// <summary>
        /// The parking sensor is operational.
        /// </summary>
        public static readonly ParkingSensorAdminStatusType Operational   = new ("operational");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly ParkingSensorAdminStatusType InternalUse   = new ("internalUse");

        #endregion


        #region Operator overloading

        #region Operator == (ParkingSensorAdminStatusType1, ParkingSensorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSensorAdminStatusType1">A parking sensor admin status type.</param>
        /// <param name="ParkingSensorAdminStatusType2">Another parking sensor admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingSensorAdminStatusType ParkingSensorAdminStatusType1,
                                           ParkingSensorAdminStatusType ParkingSensorAdminStatusType2)

            => ParkingSensorAdminStatusType1.Equals(ParkingSensorAdminStatusType2);

        #endregion

        #region Operator != (ParkingSensorAdminStatusType1, ParkingSensorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSensorAdminStatusType1">A parking sensor admin status type.</param>
        /// <param name="ParkingSensorAdminStatusType2">Another parking sensor admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingSensorAdminStatusType ParkingSensorAdminStatusType1,
                                           ParkingSensorAdminStatusType ParkingSensorAdminStatusType2)

            => !ParkingSensorAdminStatusType1.Equals(ParkingSensorAdminStatusType2);

        #endregion

        #region Operator <  (ParkingSensorAdminStatusType1, ParkingSensorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSensorAdminStatusType1">A parking sensor admin status type.</param>
        /// <param name="ParkingSensorAdminStatusType2">Another parking sensor admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingSensorAdminStatusType ParkingSensorAdminStatusType1,
                                          ParkingSensorAdminStatusType ParkingSensorAdminStatusType2)

            => ParkingSensorAdminStatusType1.CompareTo(ParkingSensorAdminStatusType2) < 0;

        #endregion

        #region Operator <= (ParkingSensorAdminStatusType1, ParkingSensorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSensorAdminStatusType1">A parking sensor admin status type.</param>
        /// <param name="ParkingSensorAdminStatusType2">Another parking sensor admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingSensorAdminStatusType ParkingSensorAdminStatusType1,
                                           ParkingSensorAdminStatusType ParkingSensorAdminStatusType2)

            => ParkingSensorAdminStatusType1.CompareTo(ParkingSensorAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (ParkingSensorAdminStatusType1, ParkingSensorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSensorAdminStatusType1">A parking sensor admin status type.</param>
        /// <param name="ParkingSensorAdminStatusType2">Another parking sensor admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingSensorAdminStatusType ParkingSensorAdminStatusType1,
                                          ParkingSensorAdminStatusType ParkingSensorAdminStatusType2)

            => ParkingSensorAdminStatusType1.CompareTo(ParkingSensorAdminStatusType2) > 0;

        #endregion

        #region Operator >= (ParkingSensorAdminStatusType1, ParkingSensorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSensorAdminStatusType1">A parking sensor admin status type.</param>
        /// <param name="ParkingSensorAdminStatusType2">Another parking sensor admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingSensorAdminStatusType ParkingSensorAdminStatusType1,
                                           ParkingSensorAdminStatusType ParkingSensorAdminStatusType2)

            => ParkingSensorAdminStatusType1.CompareTo(ParkingSensorAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingSensorAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking sensor admin status types.
        /// </summary>
        /// <param name="Object">A parking sensor admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingSensorAdminStatusType parkingSensorAdminStatusType
                   ? CompareTo(parkingSensorAdminStatusType)
                   : throw new ArgumentException("The given object is not a parking sensor admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingSensorAdminStatusType)

        /// <summary>
        /// Compares two parking sensor admin status types.
        /// </summary>
        /// <param name="ParkingSensorAdminStatusType">A parking sensor admin status type to compare with.</param>
        public Int32 CompareTo(ParkingSensorAdminStatusType ParkingSensorAdminStatusType)

            => String.Compare(InternalId,
                              ParkingSensorAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingSensorAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking sensor admin status types for equality.
        /// </summary>
        /// <param name="Object">A parking sensor admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSensorAdminStatusType parkingSensorAdminStatusType &&
                   Equals(parkingSensorAdminStatusType);

        #endregion

        #region Equals(ParkingSensorAdminStatusType)

        /// <summary>
        /// Compares two parking sensor admin status types for equality.
        /// </summary>
        /// <param name="ParkingSensorAdminStatusType">A parking sensor admin status type to compare with.</param>
        public Boolean Equals(ParkingSensorAdminStatusType ParkingSensorAdminStatusType)

            => String.Equals(InternalId,
                             ParkingSensorAdminStatusType.InternalId,
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
