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
    /// Extension methods for parking space status types.
    /// </summary>
    public static class ParkingSpaceStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this parking space status types is null or empty.
        /// </summary>
        /// <param name="ParkingSpaceStatusType">A parking space status type.</param>
        public static Boolean IsNullOrEmpty(this ParkingSpaceStatusType? ParkingSpaceStatusType)
            => !ParkingSpaceStatusType.HasValue || ParkingSpaceStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking space status types is null or empty.
        /// </summary>
        /// <param name="ParkingSpaceStatusType">A parking space status type.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingSpaceStatusType? ParkingSpaceStatusType)
            => ParkingSpaceStatusType.HasValue && ParkingSpaceStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of a parking space.
    /// </summary>
    public readonly struct ParkingSpaceStatusType : IId,
                                                               IEquatable<ParkingSpaceStatusType>,
                                                               IComparable<ParkingSpaceStatusType>
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
        /// The length of the parking space status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking space status type based on the given string.
        /// </summary>
        private ParkingSpaceStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a parking space status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking space status type.</param>
        public static ParkingSpaceStatusType Parse(String Text)
        {

            if (TryParse(Text, out ParkingSpaceStatusType parkingSpaceStatusType))
                return parkingSpaceStatusType;

            throw new ArgumentException($"Invalid text representation of a parking space status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a parking space status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking space status type.</param>
        public static ParkingSpaceStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ParkingSpaceStatusType parkingSpaceStatusType))
                return parkingSpaceStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingSpaceStatusType)

        /// <summary>
        /// Parse the given string as a parking space status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking space status type.</param>
        /// <param name="ParkingSpaceStatusType">The parsed parking space status type.</param>
        public static Boolean TryParse(String Text, out ParkingSpaceStatusType ParkingSpaceStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingSpaceStatusType = new ParkingSpaceStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingSpaceStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking space status type.
        /// </summary>
        public ParkingSpaceStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the parking space.
        /// </summary>
        public static readonly ParkingSpaceStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear status of the parking space.
        /// </summary>
        public static readonly ParkingSpaceStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The parking space is planned for the future.
        /// </summary>
        public static readonly ParkingSpaceStatusType Planned      = new ("planned");

        /// <summary>
        /// The parking space is currently in deployment.
        /// </summary>
        public static readonly ParkingSpaceStatusType InDeployment = new ("inDeployment");

        /// <summary>
        /// The parking space is currently blocked.
        /// </summary>
        public static readonly ParkingSpaceStatusType Blocked      = new ("blocked");

        /// <summary>
        /// The parking space is available.
        /// </summary>
        public static readonly ParkingSpaceStatusType Available    = new ("available");

        /// <summary>
        /// The parking space is reserved.
        /// </summary>
        public static readonly ParkingSpaceStatusType Reserved     = new ("reserved");

        /// <summary>
        /// The parking space is in use.
        /// </summary>
        public static readonly ParkingSpaceStatusType InUse        = new ("inUse");

        /// <summary>
        /// Another status of the parking space.
        /// </summary>
        public static readonly ParkingSpaceStatusType Other        = new ("other");

        #endregion


        #region Operator overloading

        #region Operator == (ParkingSpaceStatusType1, ParkingSpaceStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceStatusType1">A parking space status type.</param>
        /// <param name="ParkingSpaceStatusType2">Another parking space status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingSpaceStatusType ParkingSpaceStatusType1,
                                           ParkingSpaceStatusType ParkingSpaceStatusType2)

            => ParkingSpaceStatusType1.Equals(ParkingSpaceStatusType2);

        #endregion

        #region Operator != (ParkingSpaceStatusType1, ParkingSpaceStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceStatusType1">A parking space status type.</param>
        /// <param name="ParkingSpaceStatusType2">Another parking space status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingSpaceStatusType ParkingSpaceStatusType1,
                                           ParkingSpaceStatusType ParkingSpaceStatusType2)

            => !ParkingSpaceStatusType1.Equals(ParkingSpaceStatusType2);

        #endregion

        #region Operator <  (ParkingSpaceStatusType1, ParkingSpaceStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceStatusType1">A parking space status type.</param>
        /// <param name="ParkingSpaceStatusType2">Another parking space status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingSpaceStatusType ParkingSpaceStatusType1,
                                          ParkingSpaceStatusType ParkingSpaceStatusType2)

            => ParkingSpaceStatusType1.CompareTo(ParkingSpaceStatusType2) < 0;

        #endregion

        #region Operator <= (ParkingSpaceStatusType1, ParkingSpaceStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceStatusType1">A parking space status type.</param>
        /// <param name="ParkingSpaceStatusType2">Another parking space status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingSpaceStatusType ParkingSpaceStatusType1,
                                           ParkingSpaceStatusType ParkingSpaceStatusType2)

            => ParkingSpaceStatusType1.CompareTo(ParkingSpaceStatusType2) <= 0;

        #endregion

        #region Operator >  (ParkingSpaceStatusType1, ParkingSpaceStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceStatusType1">A parking space status type.</param>
        /// <param name="ParkingSpaceStatusType2">Another parking space status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingSpaceStatusType ParkingSpaceStatusType1,
                                          ParkingSpaceStatusType ParkingSpaceStatusType2)

            => ParkingSpaceStatusType1.CompareTo(ParkingSpaceStatusType2) > 0;

        #endregion

        #region Operator >= (ParkingSpaceStatusType1, ParkingSpaceStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceStatusType1">A parking space status type.</param>
        /// <param name="ParkingSpaceStatusType2">Another parking space status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingSpaceStatusType ParkingSpaceStatusType1,
                                           ParkingSpaceStatusType ParkingSpaceStatusType2)

            => ParkingSpaceStatusType1.CompareTo(ParkingSpaceStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingSpaceStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking space status types.
        /// </summary>
        /// <param name="Object">A parking space status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingSpaceStatusType parkingSpaceStatusType
                   ? CompareTo(parkingSpaceStatusType)
                   : throw new ArgumentException("The given object is not a parking space status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingSpaceStatusType)

        /// <summary>
        /// Compares two parking space status types.
        /// </summary>
        /// <param name="ParkingSpaceStatusType">A parking space status type to compare with.</param>
        public Int32 CompareTo(ParkingSpaceStatusType ParkingSpaceStatusType)

            => String.Compare(InternalId,
                              ParkingSpaceStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingSpaceStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking space status types for equality.
        /// </summary>
        /// <param name="Object">A parking space status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSpaceStatusType parkingSpaceStatusType &&
                   Equals(parkingSpaceStatusType);

        #endregion

        #region Equals(ParkingSpaceStatusType)

        /// <summary>
        /// Compares two parking space status types for equality.
        /// </summary>
        /// <param name="ParkingSpaceStatusType">A parking space status type to compare with.</param>
        public Boolean Equals(ParkingSpaceStatusType ParkingSpaceStatusType)

            => String.Equals(InternalId,
                             ParkingSpaceStatusType.InternalId,
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
