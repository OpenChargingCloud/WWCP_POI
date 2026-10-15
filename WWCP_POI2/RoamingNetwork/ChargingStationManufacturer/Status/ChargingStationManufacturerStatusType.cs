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
    /// Extension methods for charging station manufacturer status typess.
    /// </summary>
    public static class ChargingStationManufacturerStatusTypesExtensions
    {

        /// <summary>
        /// Indicates whether this charging station manufacturer status type is null or empty.
        /// </summary>
        /// <param name="ChargingStationManufacturerStatusType">A charging station manufacturer status type.</param>
        public static Boolean IsNullOrEmpty(this ChargingStationManufacturerStatusType? ChargingStationManufacturerStatusType)
            => !ChargingStationManufacturerStatusType.HasValue || ChargingStationManufacturerStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this charging station manufacturer status type is null or empty.
        /// </summary>
        /// <param name="ChargingStationManufacturerStatusType">A charging station manufacturer status type.</param>
        public static Boolean IsNotNullOrEmpty(this ChargingStationManufacturerStatusType? ChargingStationManufacturerStatusType)
            => ChargingStationManufacturerStatusType.HasValue && ChargingStationManufacturerStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status type of a charging station manufacturer.
    /// </summary>
    public readonly struct ChargingStationManufacturerStatusType : IId,
                                                                   IEquatable<ChargingStationManufacturerStatusType>,
                                                                   IComparable<ChargingStationManufacturerStatusType>
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
        /// The length of the charging station manufacturer status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station manufacturer status types based on the given string.
        /// </summary>
        private ChargingStationManufacturerStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a charging station manufacturer status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging station manufacturer status type.</param>
        public static ChargingStationManufacturerStatusType Parse(String Text)
        {

            if (TryParse(Text, out ChargingStationManufacturerStatusType chargingStationManufacturerStatusTypes))
                return chargingStationManufacturerStatusTypes;

            throw new ArgumentException($"Invalid text representation of a charging station manufacturer status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a charging station manufacturer status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging station manufacturer status type.</param>
        public static ChargingStationManufacturerStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ChargingStationManufacturerStatusType chargingStationManufacturerStatusTypes))
                return chargingStationManufacturerStatusTypes;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ChargingStationManufacturerStatusType)

        /// <summary>
        /// Parse the given string as a charging station manufacturer status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging station manufacturer status type.</param>
        /// <param name="ChargingStationManufacturerStatusType">The parsed charging station manufacturer status type.</param>
        public static Boolean TryParse(String Text, out ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChargingStationManufacturerStatusType = new ChargingStationManufacturerStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ChargingStationManufacturerStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging station manufacturer status type.
        /// </summary>
        public ChargingStationManufacturerStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the charging station manufacturer.
        /// </summary>
        public static ChargingStationManufacturerStatusType  Unknown             { get; }
            = new ("unknown");

        /// <summary>
        /// Unclear status of the charging station manufacturer.
        /// </summary>
        public static ChargingStationManufacturerStatusType  Unspecified         { get; }
            = new ("unspecified");

        /// <summary>
        /// Currently no communication with the charging station manufacturer is possible.
        /// </summary>
        public static ChargingStationManufacturerStatusType  Offline             { get; }
            = new ("offline");

        /// <summary>
        /// The charging station manufacturer is being deployed, not fully operational yet.
        /// </summary>
        public static ChargingStationManufacturerStatusType  InDeployment        { get; }
            = new ("inDeployment");

        /// <summary>
        /// The charging station manufacturer is available.
        /// </summary>
        public static ChargingStationManufacturerStatusType  Available           { get; }
            = new ("available");

        /// <summary>
        /// An error occurred within the charging station manufacturer.
        /// </summary>
        public static ChargingStationManufacturerStatusType  Error               { get; }
            = new ("error");

        #endregion


        #region Operator overloading

        #region Operator == (ChargingStationManufacturerStatusType1, ChargingStationManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationManufacturerStatusType1">A charging station manufacturer status type.</param>
        /// <param name="ChargingStationManufacturerStatusType2">Another charging station manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType1,
                                           ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType2)

            => ChargingStationManufacturerStatusType1.Equals(ChargingStationManufacturerStatusType2);

        #endregion

        #region Operator != (ChargingStationManufacturerStatusType1, ChargingStationManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationManufacturerStatusType1">A charging station manufacturer status type.</param>
        /// <param name="ChargingStationManufacturerStatusType2">Another charging station manufacturer status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType1,
                                           ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType2)

            => !ChargingStationManufacturerStatusType1.Equals(ChargingStationManufacturerStatusType2);

        #endregion

        #region Operator <  (ChargingStationManufacturerStatusType1, ChargingStationManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationManufacturerStatusType1">A charging station manufacturer status type.</param>
        /// <param name="ChargingStationManufacturerStatusType2">Another charging station manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType1,
                                          ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType2)

            => ChargingStationManufacturerStatusType1.CompareTo(ChargingStationManufacturerStatusType2) < 0;

        #endregion

        #region Operator <= (ChargingStationManufacturerStatusType1, ChargingStationManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationManufacturerStatusType1">A charging station manufacturer status type.</param>
        /// <param name="ChargingStationManufacturerStatusType2">Another charging station manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType1,
                                           ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType2)

            => ChargingStationManufacturerStatusType1.CompareTo(ChargingStationManufacturerStatusType2) <= 0;

        #endregion

        #region Operator >  (ChargingStationManufacturerStatusType1, ChargingStationManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationManufacturerStatusType1">A charging station manufacturer status type.</param>
        /// <param name="ChargingStationManufacturerStatusType2">Another charging station manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType1,
                                          ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType2)

            => ChargingStationManufacturerStatusType1.CompareTo(ChargingStationManufacturerStatusType2) > 0;

        #endregion

        #region Operator >= (ChargingStationManufacturerStatusType1, ChargingStationManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationManufacturerStatusType1">A charging station manufacturer status type.</param>
        /// <param name="ChargingStationManufacturerStatusType2">Another charging station manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType1,
                                           ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType2)

            => ChargingStationManufacturerStatusType1.CompareTo(ChargingStationManufacturerStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingStationManufacturerStatusTypes> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging station manufacturer status types.
        /// </summary>
        /// <param name="Object">A charging station manufacturer status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingStationManufacturerStatusType chargingStationManufacturerStatusTypes
                   ? CompareTo(chargingStationManufacturerStatusTypes)
                   : throw new ArgumentException("The given object is not a charging station manufacturer status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingStationManufacturerStatusType)

        /// <summary>
        /// Compares two charging station manufacturer status types.
        /// </summary>
        /// <param name="ChargingStationManufacturerStatusType">A charging station manufacturer status type to compare with.</param>
        public Int32 CompareTo(ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType)

            => String.Compare(InternalId,
                              ChargingStationManufacturerStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChargingStationManufacturerStatusTypes> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging station manufacturer status types for equality.
        /// </summary>
        /// <param name="Object">A charging station manufacturer status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingStationManufacturerStatusType chargingStationManufacturerStatusTypes &&
                   Equals(chargingStationManufacturerStatusTypes);

        #endregion

        #region Equals(ChargingStationManufacturerStatusType)

        /// <summary>
        /// Compares two charging station manufacturer status types for equality.
        /// </summary>
        /// <param name="ChargingStationManufacturerStatusType">A charging station manufacturer status type to compare with.</param>
        public Boolean Equals(ChargingStationManufacturerStatusType ChargingStationManufacturerStatusType)

            => String.Equals(InternalId,
                             ChargingStationManufacturerStatusType.InternalId,
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
