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
    /// Extension methods for parking garage status types.
    /// </summary>
    public static class ParkingGarageStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this parking garage status types is null or empty.
        /// </summary>
        /// <param name="ParkingGarageStatusType">A parking garage status type.</param>
        public static Boolean IsNullOrEmpty(this ParkingGarageStatusType? ParkingGarageStatusType)
            => !ParkingGarageStatusType.HasValue || ParkingGarageStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking garage status types is null or empty.
        /// </summary>
        /// <param name="ParkingGarageStatusType">A parking garage status type.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingGarageStatusType? ParkingGarageStatusType)
            => ParkingGarageStatusType.HasValue && ParkingGarageStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of a parking garage.
    /// </summary>
    public readonly struct ParkingGarageStatusType : IId,
                                                               IEquatable<ParkingGarageStatusType>,
                                                               IComparable<ParkingGarageStatusType>
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
        /// The length of the parking garage status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking garage status type based on the given string.
        /// </summary>
        private ParkingGarageStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a parking garage status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking garage status type.</param>
        public static ParkingGarageStatusType Parse(String Text)
        {

            if (TryParse(Text, out ParkingGarageStatusType parkingGarageStatusType))
                return parkingGarageStatusType;

            throw new ArgumentException($"Invalid text representation of a parking garage status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a parking garage status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking garage status type.</param>
        public static ParkingGarageStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ParkingGarageStatusType parkingGarageStatusType))
                return parkingGarageStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingGarageStatusType)

        /// <summary>
        /// Parse the given string as a parking garage status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking garage status type.</param>
        /// <param name="ParkingGarageStatusType">The parsed parking garage status type.</param>
        public static Boolean TryParse(String Text, out ParkingGarageStatusType ParkingGarageStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingGarageStatusType = new ParkingGarageStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingGarageStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking garage status type.
        /// </summary>
        public ParkingGarageStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the parking garage.
        /// </summary>
        public static readonly ParkingGarageStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear status of the parking garage.
        /// </summary>
        public static readonly ParkingGarageStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The parking garage is planned for the future.
        /// </summary>
        public static readonly ParkingGarageStatusType Planned      = new ("planned");

        /// <summary>
        /// The parking garage is currently in deployment.
        /// </summary>
        public static readonly ParkingGarageStatusType InDeployment = new ("inDeployment");

        /// <summary>
        /// The parking garage is currently blocked.
        /// </summary>
        public static readonly ParkingGarageStatusType Blocked      = new ("blocked");

        /// <summary>
        /// The parking garage is available.
        /// </summary>
        public static readonly ParkingGarageStatusType Available    = new ("available");

        #endregion


        #region Operator overloading

        #region Operator == (ParkingGarageStatusType1, ParkingGarageStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingGarageStatusType1">A parking garage status type.</param>
        /// <param name="ParkingGarageStatusType2">Another parking garage status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingGarageStatusType ParkingGarageStatusType1,
                                           ParkingGarageStatusType ParkingGarageStatusType2)

            => ParkingGarageStatusType1.Equals(ParkingGarageStatusType2);

        #endregion

        #region Operator != (ParkingGarageStatusType1, ParkingGarageStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingGarageStatusType1">A parking garage status type.</param>
        /// <param name="ParkingGarageStatusType2">Another parking garage status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingGarageStatusType ParkingGarageStatusType1,
                                           ParkingGarageStatusType ParkingGarageStatusType2)

            => !ParkingGarageStatusType1.Equals(ParkingGarageStatusType2);

        #endregion

        #region Operator <  (ParkingGarageStatusType1, ParkingGarageStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingGarageStatusType1">A parking garage status type.</param>
        /// <param name="ParkingGarageStatusType2">Another parking garage status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingGarageStatusType ParkingGarageStatusType1,
                                          ParkingGarageStatusType ParkingGarageStatusType2)

            => ParkingGarageStatusType1.CompareTo(ParkingGarageStatusType2) < 0;

        #endregion

        #region Operator <= (ParkingGarageStatusType1, ParkingGarageStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingGarageStatusType1">A parking garage status type.</param>
        /// <param name="ParkingGarageStatusType2">Another parking garage status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingGarageStatusType ParkingGarageStatusType1,
                                           ParkingGarageStatusType ParkingGarageStatusType2)

            => ParkingGarageStatusType1.CompareTo(ParkingGarageStatusType2) <= 0;

        #endregion

        #region Operator >  (ParkingGarageStatusType1, ParkingGarageStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingGarageStatusType1">A parking garage status type.</param>
        /// <param name="ParkingGarageStatusType2">Another parking garage status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingGarageStatusType ParkingGarageStatusType1,
                                          ParkingGarageStatusType ParkingGarageStatusType2)

            => ParkingGarageStatusType1.CompareTo(ParkingGarageStatusType2) > 0;

        #endregion

        #region Operator >= (ParkingGarageStatusType1, ParkingGarageStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingGarageStatusType1">A parking garage status type.</param>
        /// <param name="ParkingGarageStatusType2">Another parking garage status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingGarageStatusType ParkingGarageStatusType1,
                                           ParkingGarageStatusType ParkingGarageStatusType2)

            => ParkingGarageStatusType1.CompareTo(ParkingGarageStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingGarageStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking garage status types.
        /// </summary>
        /// <param name="Object">A parking garage status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingGarageStatusType parkingGarageStatusType
                   ? CompareTo(parkingGarageStatusType)
                   : throw new ArgumentException("The given object is not a parking garage status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingGarageStatusType)

        /// <summary>
        /// Compares two parking garage status types.
        /// </summary>
        /// <param name="ParkingGarageStatusType">A parking garage status type to compare with.</param>
        public Int32 CompareTo(ParkingGarageStatusType ParkingGarageStatusType)

            => String.Compare(InternalId,
                              ParkingGarageStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingGarageStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking garage status types for equality.
        /// </summary>
        /// <param name="Object">A parking garage status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingGarageStatusType parkingGarageStatusType &&
                   Equals(parkingGarageStatusType);

        #endregion

        #region Equals(ParkingGarageStatusType)

        /// <summary>
        /// Compares two parking garage status types for equality.
        /// </summary>
        /// <param name="ParkingGarageStatusType">A parking garage status type to compare with.</param>
        public Boolean Equals(ParkingGarageStatusType ParkingGarageStatusType)

            => String.Equals(InternalId,
                             ParkingGarageStatusType.InternalId,
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
