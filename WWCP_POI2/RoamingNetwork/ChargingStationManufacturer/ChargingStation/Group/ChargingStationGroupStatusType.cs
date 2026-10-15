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
    /// Extension methods for charging station group status types.
    /// </summary>
    public static class ChargingStationGroupStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this charging station group status types is null or empty.
        /// </summary>
        /// <param name="ChargingStationGroupStatusType">A charging station group status type.</param>
        public static Boolean IsNullOrEmpty(this ChargingStationGroupStatusType? ChargingStationGroupStatusType)
            => !ChargingStationGroupStatusType.HasValue || ChargingStationGroupStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this charging station group status types is null or empty.
        /// </summary>
        /// <param name="ChargingStationGroupStatusType">A charging station group status type.</param>
        public static Boolean IsNotNullOrEmpty(this ChargingStationGroupStatusType? ChargingStationGroupStatusType)
            => ChargingStationGroupStatusType.HasValue && ChargingStationGroupStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of a charging station group.
    /// </summary>
    public readonly struct ChargingStationGroupStatusType : IId,
                                                               IEquatable<ChargingStationGroupStatusType>,
                                                               IComparable<ChargingStationGroupStatusType>
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
        /// The length of the charging station group status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station group status type based on the given string.
        /// </summary>
        private ChargingStationGroupStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a charging station group status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging station group status type.</param>
        public static ChargingStationGroupStatusType Parse(String Text)
        {

            if (TryParse(Text, out ChargingStationGroupStatusType chargingStationGroupStatusType))
                return chargingStationGroupStatusType;

            throw new ArgumentException($"Invalid text representation of a charging station group status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a charging station group status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging station group status type.</param>
        public static ChargingStationGroupStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ChargingStationGroupStatusType chargingStationGroupStatusType))
                return chargingStationGroupStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ChargingStationGroupStatusType)

        /// <summary>
        /// Parse the given string as a charging station group status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging station group status type.</param>
        /// <param name="ChargingStationGroupStatusType">The parsed charging station group status type.</param>
        public static Boolean TryParse(String Text, out ChargingStationGroupStatusType ChargingStationGroupStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChargingStationGroupStatusType = new ChargingStationGroupStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ChargingStationGroupStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging station group status type.
        /// </summary>
        public ChargingStationGroupStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the charging station group.
        /// </summary>
        public static readonly ChargingStationGroupStatusType Unknown          = new ("unknown");

        /// <summary>
        /// Unclear status of the charging station group.
        /// </summary>
        public static readonly ChargingStationGroupStatusType Unspecified      = new ("unspecified");

        /// <summary>
        /// The charging station group is planned for the future.
        /// </summary>
        public static readonly ChargingStationGroupStatusType Planned          = new ("planned");

        /// <summary>
        /// The charging station group is currently in deployment.
        /// </summary>
        public static readonly ChargingStationGroupStatusType InDeployment     = new ("inDeployment");

        /// <summary>
        /// The charging station group is currently offline.
        /// </summary>
        public static readonly ChargingStationGroupStatusType Offline          = new ("offline");

        /// <summary>
        /// All members of the charging station group are available.
        /// </summary>
        public static readonly ChargingStationGroupStatusType Available        = new ("available");

        /// <summary>
        /// Some members of the charging station group are available.
        /// </summary>
        public static readonly ChargingStationGroupStatusType PartialAvailable = new ("partialAvailable");

        /// <summary>
        /// The members of the charging station group are reserved.
        /// </summary>
        public static readonly ChargingStationGroupStatusType Reserved         = new ("reserved");

        /// <summary>
        /// The members of the charging station group are charging.
        /// </summary>
        public static readonly ChargingStationGroupStatusType Charging         = new ("charging");

        /// <summary>
        /// The charging station group is out of service.
        /// </summary>
        public static readonly ChargingStationGroupStatusType OutOfService     = new ("outOfService");

        /// <summary>
        /// An error occurred within the charging station group.
        /// </summary>
        public static readonly ChargingStationGroupStatusType Error            = new ("error");

        #endregion


        #region Operator overloading

        #region Operator == (ChargingStationGroupStatusType1, ChargingStationGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationGroupStatusType1">A charging station group status type.</param>
        /// <param name="ChargingStationGroupStatusType2">Another charging station group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingStationGroupStatusType ChargingStationGroupStatusType1,
                                           ChargingStationGroupStatusType ChargingStationGroupStatusType2)

            => ChargingStationGroupStatusType1.Equals(ChargingStationGroupStatusType2);

        #endregion

        #region Operator != (ChargingStationGroupStatusType1, ChargingStationGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationGroupStatusType1">A charging station group status type.</param>
        /// <param name="ChargingStationGroupStatusType2">Another charging station group status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingStationGroupStatusType ChargingStationGroupStatusType1,
                                           ChargingStationGroupStatusType ChargingStationGroupStatusType2)

            => !ChargingStationGroupStatusType1.Equals(ChargingStationGroupStatusType2);

        #endregion

        #region Operator <  (ChargingStationGroupStatusType1, ChargingStationGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationGroupStatusType1">A charging station group status type.</param>
        /// <param name="ChargingStationGroupStatusType2">Another charging station group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingStationGroupStatusType ChargingStationGroupStatusType1,
                                          ChargingStationGroupStatusType ChargingStationGroupStatusType2)

            => ChargingStationGroupStatusType1.CompareTo(ChargingStationGroupStatusType2) < 0;

        #endregion

        #region Operator <= (ChargingStationGroupStatusType1, ChargingStationGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationGroupStatusType1">A charging station group status type.</param>
        /// <param name="ChargingStationGroupStatusType2">Another charging station group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingStationGroupStatusType ChargingStationGroupStatusType1,
                                           ChargingStationGroupStatusType ChargingStationGroupStatusType2)

            => ChargingStationGroupStatusType1.CompareTo(ChargingStationGroupStatusType2) <= 0;

        #endregion

        #region Operator >  (ChargingStationGroupStatusType1, ChargingStationGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationGroupStatusType1">A charging station group status type.</param>
        /// <param name="ChargingStationGroupStatusType2">Another charging station group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingStationGroupStatusType ChargingStationGroupStatusType1,
                                          ChargingStationGroupStatusType ChargingStationGroupStatusType2)

            => ChargingStationGroupStatusType1.CompareTo(ChargingStationGroupStatusType2) > 0;

        #endregion

        #region Operator >= (ChargingStationGroupStatusType1, ChargingStationGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationGroupStatusType1">A charging station group status type.</param>
        /// <param name="ChargingStationGroupStatusType2">Another charging station group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingStationGroupStatusType ChargingStationGroupStatusType1,
                                           ChargingStationGroupStatusType ChargingStationGroupStatusType2)

            => ChargingStationGroupStatusType1.CompareTo(ChargingStationGroupStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingStationGroupStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging station group status types.
        /// </summary>
        /// <param name="Object">A charging station group status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingStationGroupStatusType chargingStationGroupStatusType
                   ? CompareTo(chargingStationGroupStatusType)
                   : throw new ArgumentException("The given object is not a charging station group status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingStationGroupStatusType)

        /// <summary>
        /// Compares two charging station group status types.
        /// </summary>
        /// <param name="ChargingStationGroupStatusType">A charging station group status type to compare with.</param>
        public Int32 CompareTo(ChargingStationGroupStatusType ChargingStationGroupStatusType)

            => String.Compare(InternalId,
                              ChargingStationGroupStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChargingStationGroupStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging station group status types for equality.
        /// </summary>
        /// <param name="Object">A charging station group status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingStationGroupStatusType chargingStationGroupStatusType &&
                   Equals(chargingStationGroupStatusType);

        #endregion

        #region Equals(ChargingStationGroupStatusType)

        /// <summary>
        /// Compares two charging station group status types for equality.
        /// </summary>
        /// <param name="ChargingStationGroupStatusType">A charging station group status type to compare with.</param>
        public Boolean Equals(ChargingStationGroupStatusType ChargingStationGroupStatusType)

            => String.Equals(InternalId,
                             ChargingStationGroupStatusType.InternalId,
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
