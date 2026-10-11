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
    /// Extension methods for parking operator status types.
    /// </summary>
    public static class ParkingOperatorStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this parking operator status types is null or empty.
        /// </summary>
        /// <param name="ParkingOperatorStatusType">A parking operator status type.</param>
        public static Boolean IsNullOrEmpty(this ParkingOperatorStatusType? ParkingOperatorStatusType)
            => !ParkingOperatorStatusType.HasValue || ParkingOperatorStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking operator status types is null or empty.
        /// </summary>
        /// <param name="ParkingOperatorStatusType">A parking operator status type.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingOperatorStatusType? ParkingOperatorStatusType)
            => ParkingOperatorStatusType.HasValue && ParkingOperatorStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of a parking operator.
    /// </summary>
    public readonly struct ParkingOperatorStatusType : IId,
                                                               IEquatable<ParkingOperatorStatusType>,
                                                               IComparable<ParkingOperatorStatusType>
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
        /// The length of the parking operator status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking operator status type based on the given string.
        /// </summary>
        private ParkingOperatorStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a parking operator status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking operator status type.</param>
        public static ParkingOperatorStatusType Parse(String Text)
        {

            if (TryParse(Text, out ParkingOperatorStatusType parkingOperatorStatusType))
                return parkingOperatorStatusType;

            throw new ArgumentException($"Invalid text representation of a parking operator status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a parking operator status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking operator status type.</param>
        public static ParkingOperatorStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ParkingOperatorStatusType parkingOperatorStatusType))
                return parkingOperatorStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingOperatorStatusType)

        /// <summary>
        /// Parse the given string as a parking operator status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking operator status type.</param>
        /// <param name="ParkingOperatorStatusType">The parsed parking operator status type.</param>
        public static Boolean TryParse(String Text, out ParkingOperatorStatusType ParkingOperatorStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingOperatorStatusType = new ParkingOperatorStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingOperatorStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking operator status type.
        /// </summary>
        public ParkingOperatorStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the parking operator.
        /// </summary>
        public static readonly ParkingOperatorStatusType Unknown       = new("unknown");

        /// <summary>
        /// Unclear status of the parking operator.
        /// </summary>
        public static readonly ParkingOperatorStatusType Unspecified   = new("unspecified");

        /// <summary>
        /// The parking operator is currently offline.
        /// </summary>
        public static readonly ParkingOperatorStatusType Offline       = new("offline");

        /// <summary>
        /// The parking operator is not fully operational yet.
        /// </summary>
        public static readonly ParkingOperatorStatusType InDeployment  = new("inDeployment");

        /// <summary>
        /// The parking operator is available.
        /// </summary>
        public static readonly ParkingOperatorStatusType Available     = new("available");

        /// <summary>
        /// A fatal error has occurred within the parking operator.
        /// </summary>
        public static readonly ParkingOperatorStatusType Error         = new("error");

        #endregion


        #region Operator overloading

        #region Operator == (ParkingOperatorStatusType1, ParkingOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingOperatorStatusType1">A parking operator status type.</param>
        /// <param name="ParkingOperatorStatusType2">Another parking operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingOperatorStatusType ParkingOperatorStatusType1,
                                           ParkingOperatorStatusType ParkingOperatorStatusType2)

            => ParkingOperatorStatusType1.Equals(ParkingOperatorStatusType2);

        #endregion

        #region Operator != (ParkingOperatorStatusType1, ParkingOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingOperatorStatusType1">A parking operator status type.</param>
        /// <param name="ParkingOperatorStatusType2">Another parking operator status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingOperatorStatusType ParkingOperatorStatusType1,
                                           ParkingOperatorStatusType ParkingOperatorStatusType2)

            => !ParkingOperatorStatusType1.Equals(ParkingOperatorStatusType2);

        #endregion

        #region Operator <  (ParkingOperatorStatusType1, ParkingOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingOperatorStatusType1">A parking operator status type.</param>
        /// <param name="ParkingOperatorStatusType2">Another parking operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingOperatorStatusType ParkingOperatorStatusType1,
                                          ParkingOperatorStatusType ParkingOperatorStatusType2)

            => ParkingOperatorStatusType1.CompareTo(ParkingOperatorStatusType2) < 0;

        #endregion

        #region Operator <= (ParkingOperatorStatusType1, ParkingOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingOperatorStatusType1">A parking operator status type.</param>
        /// <param name="ParkingOperatorStatusType2">Another parking operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingOperatorStatusType ParkingOperatorStatusType1,
                                           ParkingOperatorStatusType ParkingOperatorStatusType2)

            => ParkingOperatorStatusType1.CompareTo(ParkingOperatorStatusType2) <= 0;

        #endregion

        #region Operator >  (ParkingOperatorStatusType1, ParkingOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingOperatorStatusType1">A parking operator status type.</param>
        /// <param name="ParkingOperatorStatusType2">Another parking operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingOperatorStatusType ParkingOperatorStatusType1,
                                          ParkingOperatorStatusType ParkingOperatorStatusType2)

            => ParkingOperatorStatusType1.CompareTo(ParkingOperatorStatusType2) > 0;

        #endregion

        #region Operator >= (ParkingOperatorStatusType1, ParkingOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingOperatorStatusType1">A parking operator status type.</param>
        /// <param name="ParkingOperatorStatusType2">Another parking operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingOperatorStatusType ParkingOperatorStatusType1,
                                           ParkingOperatorStatusType ParkingOperatorStatusType2)

            => ParkingOperatorStatusType1.CompareTo(ParkingOperatorStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingOperatorStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking operator status types.
        /// </summary>
        /// <param name="Object">A parking operator status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingOperatorStatusType parkingOperatorStatusType
                   ? CompareTo(parkingOperatorStatusType)
                   : throw new ArgumentException("The given object is not a parking operator status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingOperatorStatusType)

        /// <summary>
        /// Compares two parking operator status types.
        /// </summary>
        /// <param name="ParkingOperatorStatusType">A parking operator status type to compare with.</param>
        public Int32 CompareTo(ParkingOperatorStatusType ParkingOperatorStatusType)

            => String.Compare(InternalId,
                              ParkingOperatorStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingOperatorStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking operator status types for equality.
        /// </summary>
        /// <param name="Object">A parking operator status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingOperatorStatusType parkingOperatorStatusType &&
                   Equals(parkingOperatorStatusType);

        #endregion

        #region Equals(ParkingOperatorStatusType)

        /// <summary>
        /// Compares two parking operator status types for equality.
        /// </summary>
        /// <param name="ParkingOperatorStatusType">A parking operator status type to compare with.</param>
        public Boolean Equals(ParkingOperatorStatusType ParkingOperatorStatusType)

            => String.Equals(InternalId,
                             ParkingOperatorStatusType.InternalId,
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
