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
    /// Extension methods for parking space group status types.
    /// </summary>
    public static class ParkingSpaceGroupStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this parking space group status types is null or empty.
        /// </summary>
        /// <param name="ParkingSpaceGroupStatusType">A parking space group status type.</param>
        public static Boolean IsNullOrEmpty(this ParkingSpaceGroupStatusType? ParkingSpaceGroupStatusType)
            => !ParkingSpaceGroupStatusType.HasValue || ParkingSpaceGroupStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking space group status types is null or empty.
        /// </summary>
        /// <param name="ParkingSpaceGroupStatusType">A parking space group status type.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingSpaceGroupStatusType? ParkingSpaceGroupStatusType)
            => ParkingSpaceGroupStatusType.HasValue && ParkingSpaceGroupStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of a parking space group.
    /// </summary>
    public readonly struct ParkingSpaceGroupStatusType : IId,
                                                               IEquatable<ParkingSpaceGroupStatusType>,
                                                               IComparable<ParkingSpaceGroupStatusType>
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
        /// The length of the parking space group status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking space group status type based on the given string.
        /// </summary>
        private ParkingSpaceGroupStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a parking space group status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking space group status type.</param>
        public static ParkingSpaceGroupStatusType Parse(String Text)
        {

            if (TryParse(Text, out ParkingSpaceGroupStatusType parkingSpaceGroupStatusType))
                return parkingSpaceGroupStatusType;

            throw new ArgumentException($"Invalid text representation of a parking space group status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a parking space group status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking space group status type.</param>
        public static ParkingSpaceGroupStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ParkingSpaceGroupStatusType parkingSpaceGroupStatusType))
                return parkingSpaceGroupStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingSpaceGroupStatusType)

        /// <summary>
        /// Parse the given string as a parking space group status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking space group status type.</param>
        /// <param name="ParkingSpaceGroupStatusType">The parsed parking space group status type.</param>
        public static Boolean TryParse(String Text, out ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingSpaceGroupStatusType = new ParkingSpaceGroupStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingSpaceGroupStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking space group status type.
        /// </summary>
        public ParkingSpaceGroupStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the parking space group.
        /// </summary>
        public static readonly ParkingSpaceGroupStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear status of the parking space group.
        /// </summary>
        public static readonly ParkingSpaceGroupStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The parking space group is planned for the future.
        /// </summary>
        public static readonly ParkingSpaceGroupStatusType Planned      = new ("planned");

        /// <summary>
        /// The parking space group is currently in deployment.
        /// </summary>
        public static readonly ParkingSpaceGroupStatusType InDeployment = new ("inDeployment");

        /// <summary>
        /// The parking space group is currently blocked.
        /// </summary>
        public static readonly ParkingSpaceGroupStatusType Blocked      = new ("blocked");

        /// <summary>
        /// The parking space group is available.
        /// </summary>
        public static readonly ParkingSpaceGroupStatusType Available    = new ("available");

        /// <summary>
        /// The parking space group is reserved.
        /// </summary>
        public static readonly ParkingSpaceGroupStatusType Reserved     = new ("reserved");

        /// <summary>
        /// The parking space group is in use.
        /// </summary>
        public static readonly ParkingSpaceGroupStatusType InUse        = new ("inUse");

        /// <summary>
        /// Another status of the parking space group.
        /// </summary>
        public static readonly ParkingSpaceGroupStatusType Other        = new ("other");

        #endregion


        #region Operator overloading

        #region Operator == (ParkingSpaceGroupStatusType1, ParkingSpaceGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceGroupStatusType1">A parking space group status type.</param>
        /// <param name="ParkingSpaceGroupStatusType2">Another parking space group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType1,
                                           ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType2)

            => ParkingSpaceGroupStatusType1.Equals(ParkingSpaceGroupStatusType2);

        #endregion

        #region Operator != (ParkingSpaceGroupStatusType1, ParkingSpaceGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceGroupStatusType1">A parking space group status type.</param>
        /// <param name="ParkingSpaceGroupStatusType2">Another parking space group status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType1,
                                           ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType2)

            => !ParkingSpaceGroupStatusType1.Equals(ParkingSpaceGroupStatusType2);

        #endregion

        #region Operator <  (ParkingSpaceGroupStatusType1, ParkingSpaceGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceGroupStatusType1">A parking space group status type.</param>
        /// <param name="ParkingSpaceGroupStatusType2">Another parking space group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType1,
                                          ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType2)

            => ParkingSpaceGroupStatusType1.CompareTo(ParkingSpaceGroupStatusType2) < 0;

        #endregion

        #region Operator <= (ParkingSpaceGroupStatusType1, ParkingSpaceGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceGroupStatusType1">A parking space group status type.</param>
        /// <param name="ParkingSpaceGroupStatusType2">Another parking space group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType1,
                                           ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType2)

            => ParkingSpaceGroupStatusType1.CompareTo(ParkingSpaceGroupStatusType2) <= 0;

        #endregion

        #region Operator >  (ParkingSpaceGroupStatusType1, ParkingSpaceGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceGroupStatusType1">A parking space group status type.</param>
        /// <param name="ParkingSpaceGroupStatusType2">Another parking space group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType1,
                                          ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType2)

            => ParkingSpaceGroupStatusType1.CompareTo(ParkingSpaceGroupStatusType2) > 0;

        #endregion

        #region Operator >= (ParkingSpaceGroupStatusType1, ParkingSpaceGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpaceGroupStatusType1">A parking space group status type.</param>
        /// <param name="ParkingSpaceGroupStatusType2">Another parking space group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType1,
                                           ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType2)

            => ParkingSpaceGroupStatusType1.CompareTo(ParkingSpaceGroupStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingSpaceGroupStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking space group status types.
        /// </summary>
        /// <param name="Object">A parking space group status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingSpaceGroupStatusType parkingSpaceGroupStatusType
                   ? CompareTo(parkingSpaceGroupStatusType)
                   : throw new ArgumentException("The given object is not a parking space group status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingSpaceGroupStatusType)

        /// <summary>
        /// Compares two parking space group status types.
        /// </summary>
        /// <param name="ParkingSpaceGroupStatusType">A parking space group status type to compare with.</param>
        public Int32 CompareTo(ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType)

            => String.Compare(InternalId,
                              ParkingSpaceGroupStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingSpaceGroupStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking space group status types for equality.
        /// </summary>
        /// <param name="Object">A parking space group status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSpaceGroupStatusType parkingSpaceGroupStatusType &&
                   Equals(parkingSpaceGroupStatusType);

        #endregion

        #region Equals(ParkingSpaceGroupStatusType)

        /// <summary>
        /// Compares two parking space group status types for equality.
        /// </summary>
        /// <param name="ParkingSpaceGroupStatusType">A parking space group status type to compare with.</param>
        public Boolean Equals(ParkingSpaceGroupStatusType ParkingSpaceGroupStatusType)

            => String.Equals(InternalId,
                             ParkingSpaceGroupStatusType.InternalId,
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
