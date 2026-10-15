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
    /// Extension methods for charging station manufacturer admin status types.
    /// </summary>
    public static class ChargingStationManufacturerAdminStatusTypesExtensions
    {

        /// <summary>
        /// Indicates whether this charging station manufacturer admin status type is null or empty.
        /// </summary>
        /// <param name="ChargingStationManufacturerAdminStatusType">A charging station manufacturer admin status type.</param>
        public static Boolean IsNullOrEmpty(this ChargingStationManufacturerAdminStatusType? ChargingStationManufacturerAdminStatusType)
            => !ChargingStationManufacturerAdminStatusType.HasValue || ChargingStationManufacturerAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this charging station manufacturer admin status type is null or empty.
        /// </summary>
        /// <param name="ChargingStationManufacturerAdminStatusType">A charging station manufacturer admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this ChargingStationManufacturerAdminStatusType? ChargingStationManufacturerAdminStatusType)
            => ChargingStationManufacturerAdminStatusType.HasValue && ChargingStationManufacturerAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a charging station manufacturer.
    /// </summary>
    public readonly struct ChargingStationManufacturerAdminStatusType : IId,
                                                                        IEquatable<ChargingStationManufacturerAdminStatusType>,
                                                                        IComparable<ChargingStationManufacturerAdminStatusType>
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
        /// The length of the charging station manufacturer admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station manufacturer admin status type based on the given string.
        /// </summary>
        private ChargingStationManufacturerAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a charging station manufacturer admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging station manufacturer admin status type.</param>
        public static ChargingStationManufacturerAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out ChargingStationManufacturerAdminStatusType chargingStationManufacturerAdminStatusTypes))
                return chargingStationManufacturerAdminStatusTypes;

            throw new ArgumentException($"Invalid text representation of a charging station manufacturer admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a charging station manufacturer admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging station manufacturer admin status type.</param>
        public static ChargingStationManufacturerAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ChargingStationManufacturerAdminStatusType chargingStationManufacturerAdminStatusTypes))
                return chargingStationManufacturerAdminStatusTypes;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ChargingStationManufacturerAdminStatusType)

        /// <summary>
        /// Parse the given string as a charging station manufacturer admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging station manufacturer admin status type.</param>
        /// <param name="ChargingStationManufacturerAdminStatusType">The parsed charging station manufacturer admin status type.</param>
        public static Boolean TryParse(String Text, out ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChargingStationManufacturerAdminStatusType = new ChargingStationManufacturerAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ChargingStationManufacturerAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging station manufacturer admin status type.
        /// </summary>
        public ChargingStationManufacturerAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the charging station manufacturer.
        /// </summary>
        public static ChargingStationManufacturerAdminStatusType  Unknown             { get; }
            = new ("unknown");

        /// <summary>
        /// Unclear status of the charging station manufacturer.
        /// </summary>
        public static ChargingStationManufacturerAdminStatusType  Unspecified         { get; }
            = new ("unspecified");

        /// <summary>
        /// The charging station manufacturer is planned for the future.
        /// </summary>
        public static ChargingStationManufacturerAdminStatusType  Planned             { get; }
            = new ("planned");

        /// <summary>
        /// The charging station manufacturer is being deployed, not fully operational yet.
        /// </summary>
        public static ChargingStationManufacturerAdminStatusType  InDeployment        { get; }
            = new ("inDeployment");

        /// <summary>
        /// The charging station manufacturer is operational.
        /// </summary>
        public static ChargingStationManufacturerAdminStatusType  Operational         { get; }
            = new ("operational");

        /// <summary>
        /// The charging station manufacturer is for internal use only, e.g. for tests.
        /// </summary>
        public static ChargingStationManufacturerAdminStatusType  InternalUse         { get; }
            = new ("internalUse");

        /// <summary>
        /// The charging station manufacturer is under maintenance.
        /// </summary>
        public static ChargingStationManufacturerAdminStatusType  OutOfService        { get; }
            = new ("outOfService");

        #endregion


        #region Operator overloading

        #region Operator == (ChargingStationManufacturerAdminStatusType1, ChargingStationManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationManufacturerAdminStatusType1">A charging station manufacturer admin status type.</param>
        /// <param name="ChargingStationManufacturerAdminStatusType2">Another charging station manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType1,
                                           ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType2)

            => ChargingStationManufacturerAdminStatusType1.Equals(ChargingStationManufacturerAdminStatusType2);

        #endregion

        #region Operator != (ChargingStationManufacturerAdminStatusType1, ChargingStationManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationManufacturerAdminStatusType1">A charging station manufacturer admin status type.</param>
        /// <param name="ChargingStationManufacturerAdminStatusType2">Another charging station manufacturer admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType1,
                                           ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType2)

            => !ChargingStationManufacturerAdminStatusType1.Equals(ChargingStationManufacturerAdminStatusType2);

        #endregion

        #region Operator <  (ChargingStationManufacturerAdminStatusType1, ChargingStationManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationManufacturerAdminStatusType1">A charging station manufacturer admin status type.</param>
        /// <param name="ChargingStationManufacturerAdminStatusType2">Another charging station manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType1,
                                          ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType2)

            => ChargingStationManufacturerAdminStatusType1.CompareTo(ChargingStationManufacturerAdminStatusType2) < 0;

        #endregion

        #region Operator <= (ChargingStationManufacturerAdminStatusType1, ChargingStationManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationManufacturerAdminStatusType1">A charging station manufacturer admin status type.</param>
        /// <param name="ChargingStationManufacturerAdminStatusType2">Another charging station manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType1,
                                           ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType2)

            => ChargingStationManufacturerAdminStatusType1.CompareTo(ChargingStationManufacturerAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (ChargingStationManufacturerAdminStatusType1, ChargingStationManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationManufacturerAdminStatusType1">A charging station manufacturer admin status type.</param>
        /// <param name="ChargingStationManufacturerAdminStatusType2">Another charging station manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType1,
                                          ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType2)

            => ChargingStationManufacturerAdminStatusType1.CompareTo(ChargingStationManufacturerAdminStatusType2) > 0;

        #endregion

        #region Operator >= (ChargingStationManufacturerAdminStatusType1, ChargingStationManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationManufacturerAdminStatusType1">A charging station manufacturer admin status type.</param>
        /// <param name="ChargingStationManufacturerAdminStatusType2">Another charging station manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType1,
                                           ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType2)

            => ChargingStationManufacturerAdminStatusType1.CompareTo(ChargingStationManufacturerAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingStationManufacturerAdminStatusTypes> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging station manufacturer admin status types.
        /// </summary>
        /// <param name="Object">A charging station manufacturer admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingStationManufacturerAdminStatusType chargingStationManufacturerAdminStatusTypes
                   ? CompareTo(chargingStationManufacturerAdminStatusTypes)
                   : throw new ArgumentException("The given object is not a charging station manufacturer admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingStationManufacturerAdminStatusType)

        /// <summary>
        /// Compares two charging station manufacturer admin status types.
        /// </summary>
        /// <param name="ChargingStationManufacturerAdminStatusType">A charging station manufacturer admin status type to compare with.</param>
        public Int32 CompareTo(ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType)

            => String.Compare(InternalId,
                              ChargingStationManufacturerAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChargingStationManufacturerAdminStatusTypes> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging station manufacturer admin status types for equality.
        /// </summary>
        /// <param name="Object">A charging station manufacturer admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingStationManufacturerAdminStatusType chargingStationManufacturerAdminStatusTypes &&
                   Equals(chargingStationManufacturerAdminStatusTypes);

        #endregion

        #region Equals(ChargingStationManufacturerAdminStatusType)

        /// <summary>
        /// Compares two charging station manufacturer admin status types for equality.
        /// </summary>
        /// <param name="ChargingStationManufacturerAdminStatusType">A charging station manufacturer admin status type to compare with.</param>
        public Boolean Equals(ChargingStationManufacturerAdminStatusType ChargingStationManufacturerAdminStatusType)

            => String.Equals(InternalId,
                             ChargingStationManufacturerAdminStatusType.InternalId,
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
