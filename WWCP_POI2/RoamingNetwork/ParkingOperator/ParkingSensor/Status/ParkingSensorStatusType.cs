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
    /// Extension methods for parking sensor status types.
    /// </summary>
    public static class ParkingSensorStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this parking sensor status types is null or empty.
        /// </summary>
        /// <param name="ParkingSensorStatusType">A parking sensor status type.</param>
        public static Boolean IsNullOrEmpty(this ParkingSensorStatusType? ParkingSensorStatusType)
            => !ParkingSensorStatusType.HasValue || ParkingSensorStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking sensor status types is null or empty.
        /// </summary>
        /// <param name="ParkingSensorStatusType">A parking sensor status type.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingSensorStatusType? ParkingSensorStatusType)
            => ParkingSensorStatusType.HasValue && ParkingSensorStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of a parking sensor.
    /// </summary>
    public readonly struct ParkingSensorStatusType : IId,
                                                               IEquatable<ParkingSensorStatusType>,
                                                               IComparable<ParkingSensorStatusType>
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
        /// The length of the parking sensor status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking sensor status type based on the given string.
        /// </summary>
        private ParkingSensorStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a parking sensor status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking sensor status type.</param>
        public static ParkingSensorStatusType Parse(String Text)
        {

            if (TryParse(Text, out ParkingSensorStatusType parkingSensorStatusType))
                return parkingSensorStatusType;

            throw new ArgumentException($"Invalid text representation of a parking sensor status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a parking sensor status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking sensor status type.</param>
        public static ParkingSensorStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ParkingSensorStatusType parkingSensorStatusType))
                return parkingSensorStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingSensorStatusType)

        /// <summary>
        /// Parse the given string as a parking sensor status type.
        /// </summary>
        /// <param name="Text">A text representation of a parking sensor status type.</param>
        /// <param name="ParkingSensorStatusType">The parsed parking sensor status type.</param>
        public static Boolean TryParse(String Text, out ParkingSensorStatusType ParkingSensorStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingSensorStatusType = new ParkingSensorStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingSensorStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking sensor status type.
        /// </summary>
        public ParkingSensorStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the parking sensor.
        /// </summary>
        public static readonly ParkingSensorStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear status of the parking sensor.
        /// </summary>
        public static readonly ParkingSensorStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The parking sensor is planned for the future.
        /// </summary>
        public static readonly ParkingSensorStatusType Planned      = new ("planned");

        /// <summary>
        /// The parking sensor is currently in deployment.
        /// </summary>
        public static readonly ParkingSensorStatusType InDeployment = new ("inDeployment");

        /// <summary>
        /// The parking sensor is currently blocked.
        /// </summary>
        public static readonly ParkingSensorStatusType Blocked      = new ("blocked");

        /// <summary>
        /// The parking sensor is available.
        /// </summary>
        public static readonly ParkingSensorStatusType Available    = new ("available");

        #endregion


        #region Operator overloading

        #region Operator == (ParkingSensorStatusType1, ParkingSensorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSensorStatusType1">A parking sensor status type.</param>
        /// <param name="ParkingSensorStatusType2">Another parking sensor status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingSensorStatusType ParkingSensorStatusType1,
                                           ParkingSensorStatusType ParkingSensorStatusType2)

            => ParkingSensorStatusType1.Equals(ParkingSensorStatusType2);

        #endregion

        #region Operator != (ParkingSensorStatusType1, ParkingSensorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSensorStatusType1">A parking sensor status type.</param>
        /// <param name="ParkingSensorStatusType2">Another parking sensor status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingSensorStatusType ParkingSensorStatusType1,
                                           ParkingSensorStatusType ParkingSensorStatusType2)

            => !ParkingSensorStatusType1.Equals(ParkingSensorStatusType2);

        #endregion

        #region Operator <  (ParkingSensorStatusType1, ParkingSensorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSensorStatusType1">A parking sensor status type.</param>
        /// <param name="ParkingSensorStatusType2">Another parking sensor status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingSensorStatusType ParkingSensorStatusType1,
                                          ParkingSensorStatusType ParkingSensorStatusType2)

            => ParkingSensorStatusType1.CompareTo(ParkingSensorStatusType2) < 0;

        #endregion

        #region Operator <= (ParkingSensorStatusType1, ParkingSensorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSensorStatusType1">A parking sensor status type.</param>
        /// <param name="ParkingSensorStatusType2">Another parking sensor status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingSensorStatusType ParkingSensorStatusType1,
                                           ParkingSensorStatusType ParkingSensorStatusType2)

            => ParkingSensorStatusType1.CompareTo(ParkingSensorStatusType2) <= 0;

        #endregion

        #region Operator >  (ParkingSensorStatusType1, ParkingSensorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSensorStatusType1">A parking sensor status type.</param>
        /// <param name="ParkingSensorStatusType2">Another parking sensor status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingSensorStatusType ParkingSensorStatusType1,
                                          ParkingSensorStatusType ParkingSensorStatusType2)

            => ParkingSensorStatusType1.CompareTo(ParkingSensorStatusType2) > 0;

        #endregion

        #region Operator >= (ParkingSensorStatusType1, ParkingSensorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSensorStatusType1">A parking sensor status type.</param>
        /// <param name="ParkingSensorStatusType2">Another parking sensor status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingSensorStatusType ParkingSensorStatusType1,
                                           ParkingSensorStatusType ParkingSensorStatusType2)

            => ParkingSensorStatusType1.CompareTo(ParkingSensorStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingSensorStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking sensor status types.
        /// </summary>
        /// <param name="Object">A parking sensor status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingSensorStatusType parkingSensorStatusType
                   ? CompareTo(parkingSensorStatusType)
                   : throw new ArgumentException("The given object is not a parking sensor status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingSensorStatusType)

        /// <summary>
        /// Compares two parking sensor status types.
        /// </summary>
        /// <param name="ParkingSensorStatusType">A parking sensor status type to compare with.</param>
        public Int32 CompareTo(ParkingSensorStatusType ParkingSensorStatusType)

            => String.Compare(InternalId,
                              ParkingSensorStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingSensorStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking sensor status types for equality.
        /// </summary>
        /// <param name="Object">A parking sensor status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSensorStatusType parkingSensorStatusType &&
                   Equals(parkingSensorStatusType);

        #endregion

        #region Equals(ParkingSensorStatusType)

        /// <summary>
        /// Compares two parking sensor status types for equality.
        /// </summary>
        /// <param name="ParkingSensorStatusType">A parking sensor status type to compare with.</param>
        public Boolean Equals(ParkingSensorStatusType ParkingSensorStatusType)

            => String.Equals(InternalId,
                             ParkingSensorStatusType.InternalId,
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
