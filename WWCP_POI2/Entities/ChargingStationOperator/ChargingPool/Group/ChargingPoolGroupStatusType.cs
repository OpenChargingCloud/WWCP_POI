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
    /// Extension methods for charging pool group status types.
    /// </summary>
    public static class ChargingPoolGroupStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this charging pool group status types is null or empty.
        /// </summary>
        /// <param name="ChargingPoolGroupStatusType">A charging pool group status type.</param>
        public static Boolean IsNullOrEmpty(this ChargingPoolGroupStatusType? ChargingPoolGroupStatusType)
            => !ChargingPoolGroupStatusType.HasValue || ChargingPoolGroupStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this charging pool group status types is null or empty.
        /// </summary>
        /// <param name="ChargingPoolGroupStatusType">A charging pool group status type.</param>
        public static Boolean IsNotNullOrEmpty(this ChargingPoolGroupStatusType? ChargingPoolGroupStatusType)
            => ChargingPoolGroupStatusType.HasValue && ChargingPoolGroupStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of a charging pool group.
    /// </summary>
    public readonly struct ChargingPoolGroupStatusType : IId,
                                                               IEquatable<ChargingPoolGroupStatusType>,
                                                               IComparable<ChargingPoolGroupStatusType>
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
        /// The length of the charging pool group status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging pool group status type based on the given string.
        /// </summary>
        private ChargingPoolGroupStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a charging pool group status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging pool group status type.</param>
        public static ChargingPoolGroupStatusType Parse(String Text)
        {

            if (TryParse(Text, out ChargingPoolGroupStatusType chargingPoolGroupStatusType))
                return chargingPoolGroupStatusType;

            throw new ArgumentException($"Invalid text representation of a charging pool group status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a charging pool group status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging pool group status type.</param>
        public static ChargingPoolGroupStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ChargingPoolGroupStatusType chargingPoolGroupStatusType))
                return chargingPoolGroupStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ChargingPoolGroupStatusType)

        /// <summary>
        /// Parse the given string as a charging pool group status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging pool group status type.</param>
        /// <param name="ChargingPoolGroupStatusType">The parsed charging pool group status type.</param>
        public static Boolean TryParse(String Text, out ChargingPoolGroupStatusType ChargingPoolGroupStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChargingPoolGroupStatusType = new ChargingPoolGroupStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ChargingPoolGroupStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging pool group status type.
        /// </summary>
        public ChargingPoolGroupStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the charging pool group.
        /// </summary>
        public static readonly ChargingPoolGroupStatusType Unknown          = new ("unknown");

        /// <summary>
        /// Unclear status of the charging pool group.
        /// </summary>
        public static readonly ChargingPoolGroupStatusType Unspecified      = new ("unspecified");

        /// <summary>
        /// The charging pool group is planned for the future.
        /// </summary>
        public static readonly ChargingPoolGroupStatusType Planned          = new ("planned");

        /// <summary>
        /// The charging pool group is currently in deployment.
        /// </summary>
        public static readonly ChargingPoolGroupStatusType InDeployment     = new ("inDeployment");

        /// <summary>
        /// The charging pool group is out of service.
        /// </summary>
        public static readonly ChargingPoolGroupStatusType OutOfService     = new ("outOfService");

        /// <summary>
        /// The charging pool group is currently offline.
        /// </summary>
        public static readonly ChargingPoolGroupStatusType Offline          = new ("offline");

        /// <summary>
        /// All members of the charging pool group are available.
        /// </summary>
        public static readonly ChargingPoolGroupStatusType Available        = new ("available");

        /// <summary>
        /// Some members of the charging pool group are available.
        /// </summary>
        public static readonly ChargingPoolGroupStatusType PartialAvailable = new ("partialAvailable");

        /// <summary>
        /// The members of the charging pool group are reserved.
        /// </summary>
        public static readonly ChargingPoolGroupStatusType Reserved         = new ("reserved");

        /// <summary>
        /// The members of the charging pool group are charging.
        /// </summary>
        public static readonly ChargingPoolGroupStatusType Charging         = new ("charging");

        /// <summary>
        /// The members of the charging pool group are faulted.
        /// </summary>
        public static readonly ChargingPoolGroupStatusType Faulted          = new ("faulted");

        /// <summary>
        /// Another status of the charging pool group.
        /// </summary>
        public static readonly ChargingPoolGroupStatusType Other            = new ("other");

        #endregion


        #region Operator overloading

        #region Operator == (ChargingPoolGroupStatusType1, ChargingPoolGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupStatusType1">A charging pool group status type.</param>
        /// <param name="ChargingPoolGroupStatusType2">Another charging pool group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingPoolGroupStatusType ChargingPoolGroupStatusType1,
                                           ChargingPoolGroupStatusType ChargingPoolGroupStatusType2)

            => ChargingPoolGroupStatusType1.Equals(ChargingPoolGroupStatusType2);

        #endregion

        #region Operator != (ChargingPoolGroupStatusType1, ChargingPoolGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupStatusType1">A charging pool group status type.</param>
        /// <param name="ChargingPoolGroupStatusType2">Another charging pool group status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingPoolGroupStatusType ChargingPoolGroupStatusType1,
                                           ChargingPoolGroupStatusType ChargingPoolGroupStatusType2)

            => !ChargingPoolGroupStatusType1.Equals(ChargingPoolGroupStatusType2);

        #endregion

        #region Operator <  (ChargingPoolGroupStatusType1, ChargingPoolGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupStatusType1">A charging pool group status type.</param>
        /// <param name="ChargingPoolGroupStatusType2">Another charging pool group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingPoolGroupStatusType ChargingPoolGroupStatusType1,
                                          ChargingPoolGroupStatusType ChargingPoolGroupStatusType2)

            => ChargingPoolGroupStatusType1.CompareTo(ChargingPoolGroupStatusType2) < 0;

        #endregion

        #region Operator <= (ChargingPoolGroupStatusType1, ChargingPoolGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupStatusType1">A charging pool group status type.</param>
        /// <param name="ChargingPoolGroupStatusType2">Another charging pool group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingPoolGroupStatusType ChargingPoolGroupStatusType1,
                                           ChargingPoolGroupStatusType ChargingPoolGroupStatusType2)

            => ChargingPoolGroupStatusType1.CompareTo(ChargingPoolGroupStatusType2) <= 0;

        #endregion

        #region Operator >  (ChargingPoolGroupStatusType1, ChargingPoolGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupStatusType1">A charging pool group status type.</param>
        /// <param name="ChargingPoolGroupStatusType2">Another charging pool group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingPoolGroupStatusType ChargingPoolGroupStatusType1,
                                          ChargingPoolGroupStatusType ChargingPoolGroupStatusType2)

            => ChargingPoolGroupStatusType1.CompareTo(ChargingPoolGroupStatusType2) > 0;

        #endregion

        #region Operator >= (ChargingPoolGroupStatusType1, ChargingPoolGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupStatusType1">A charging pool group status type.</param>
        /// <param name="ChargingPoolGroupStatusType2">Another charging pool group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingPoolGroupStatusType ChargingPoolGroupStatusType1,
                                           ChargingPoolGroupStatusType ChargingPoolGroupStatusType2)

            => ChargingPoolGroupStatusType1.CompareTo(ChargingPoolGroupStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingPoolGroupStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging pool group status types.
        /// </summary>
        /// <param name="Object">A charging pool group status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingPoolGroupStatusType chargingPoolGroupStatusType
                   ? CompareTo(chargingPoolGroupStatusType)
                   : throw new ArgumentException("The given object is not a charging pool group status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingPoolGroupStatusType)

        /// <summary>
        /// Compares two charging pool group status types.
        /// </summary>
        /// <param name="ChargingPoolGroupStatusType">A charging pool group status type to compare with.</param>
        public Int32 CompareTo(ChargingPoolGroupStatusType ChargingPoolGroupStatusType)

            => String.Compare(InternalId,
                              ChargingPoolGroupStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChargingPoolGroupStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging pool group status types for equality.
        /// </summary>
        /// <param name="Object">A charging pool group status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingPoolGroupStatusType chargingPoolGroupStatusType &&
                   Equals(chargingPoolGroupStatusType);

        #endregion

        #region Equals(ChargingPoolGroupStatusType)

        /// <summary>
        /// Compares two charging pool group status types for equality.
        /// </summary>
        /// <param name="ChargingPoolGroupStatusType">A charging pool group status type to compare with.</param>
        public Boolean Equals(ChargingPoolGroupStatusType ChargingPoolGroupStatusType)

            => String.Equals(InternalId,
                             ChargingPoolGroupStatusType.InternalId,
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
