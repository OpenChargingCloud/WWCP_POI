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
    /// Extension methods for charging tariff group status types.
    /// </summary>
    public static class ChargingTariffGroupStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this charging tariff group status types is null or empty.
        /// </summary>
        /// <param name="ChargingTariffGroupStatusType">A charging tariff group status type.</param>
        public static Boolean IsNullOrEmpty(this ChargingTariffGroupStatusType? ChargingTariffGroupStatusType)
            => !ChargingTariffGroupStatusType.HasValue || ChargingTariffGroupStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this charging tariff group status types is null or empty.
        /// </summary>
        /// <param name="ChargingTariffGroupStatusType">A charging tariff group status type.</param>
        public static Boolean IsNotNullOrEmpty(this ChargingTariffGroupStatusType? ChargingTariffGroupStatusType)
            => ChargingTariffGroupStatusType.HasValue && ChargingTariffGroupStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of a charging tariff group.
    /// </summary>
    public readonly struct ChargingTariffGroupStatusType : IId,
                                                               IEquatable<ChargingTariffGroupStatusType>,
                                                               IComparable<ChargingTariffGroupStatusType>
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
        /// The length of the charging tariff group status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging tariff group status type based on the given string.
        /// </summary>
        private ChargingTariffGroupStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a charging tariff group status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff group status type.</param>
        public static ChargingTariffGroupStatusType Parse(String Text)
        {

            if (TryParse(Text, out ChargingTariffGroupStatusType chargingTariffGroupStatusType))
                return chargingTariffGroupStatusType;

            throw new ArgumentException($"Invalid text representation of a charging tariff group status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a charging tariff group status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff group status type.</param>
        public static ChargingTariffGroupStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ChargingTariffGroupStatusType chargingTariffGroupStatusType))
                return chargingTariffGroupStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ChargingTariffGroupStatusType)

        /// <summary>
        /// Parse the given string as a charging tariff group status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff group status type.</param>
        /// <param name="ChargingTariffGroupStatusType">The parsed charging tariff group status type.</param>
        public static Boolean TryParse(String Text, out ChargingTariffGroupStatusType ChargingTariffGroupStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChargingTariffGroupStatusType = new ChargingTariffGroupStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ChargingTariffGroupStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging tariff group status type.
        /// </summary>
        public ChargingTariffGroupStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the charging tariff group.
        /// </summary>
        public static readonly ChargingTariffGroupStatusType Unknown     = new ("unknown");

        /// <summary>
        /// Unclear status of the charging tariff group.
        /// </summary>
        public static readonly ChargingTariffGroupStatusType Unspecified = new ("unspecified");

        /// <summary>
        /// The charging tariff group is planned for the future.
        /// </summary>
        public static readonly ChargingTariffGroupStatusType Planned     = new ("planned");

        /// <summary>
        /// The charging tariff group is currently in use.
        /// </summary>
        public static readonly ChargingTariffGroupStatusType Active      = new ("active");

        /// <summary>
        /// The charging tariff group is no longer in use.
        /// </summary>
        public static readonly ChargingTariffGroupStatusType Expired     = new ("expired");

        /// <summary>
        /// Another status of the charging tariff group.
        /// </summary>
        public static readonly ChargingTariffGroupStatusType Other       = new ("other");

        #endregion


        #region Operator overloading

        #region Operator == (ChargingTariffGroupStatusType1, ChargingTariffGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupStatusType1">A charging tariff group status type.</param>
        /// <param name="ChargingTariffGroupStatusType2">Another charging tariff group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingTariffGroupStatusType ChargingTariffGroupStatusType1,
                                           ChargingTariffGroupStatusType ChargingTariffGroupStatusType2)

            => ChargingTariffGroupStatusType1.Equals(ChargingTariffGroupStatusType2);

        #endregion

        #region Operator != (ChargingTariffGroupStatusType1, ChargingTariffGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupStatusType1">A charging tariff group status type.</param>
        /// <param name="ChargingTariffGroupStatusType2">Another charging tariff group status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingTariffGroupStatusType ChargingTariffGroupStatusType1,
                                           ChargingTariffGroupStatusType ChargingTariffGroupStatusType2)

            => !ChargingTariffGroupStatusType1.Equals(ChargingTariffGroupStatusType2);

        #endregion

        #region Operator <  (ChargingTariffGroupStatusType1, ChargingTariffGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupStatusType1">A charging tariff group status type.</param>
        /// <param name="ChargingTariffGroupStatusType2">Another charging tariff group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingTariffGroupStatusType ChargingTariffGroupStatusType1,
                                          ChargingTariffGroupStatusType ChargingTariffGroupStatusType2)

            => ChargingTariffGroupStatusType1.CompareTo(ChargingTariffGroupStatusType2) < 0;

        #endregion

        #region Operator <= (ChargingTariffGroupStatusType1, ChargingTariffGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupStatusType1">A charging tariff group status type.</param>
        /// <param name="ChargingTariffGroupStatusType2">Another charging tariff group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingTariffGroupStatusType ChargingTariffGroupStatusType1,
                                           ChargingTariffGroupStatusType ChargingTariffGroupStatusType2)

            => ChargingTariffGroupStatusType1.CompareTo(ChargingTariffGroupStatusType2) <= 0;

        #endregion

        #region Operator >  (ChargingTariffGroupStatusType1, ChargingTariffGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupStatusType1">A charging tariff group status type.</param>
        /// <param name="ChargingTariffGroupStatusType2">Another charging tariff group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingTariffGroupStatusType ChargingTariffGroupStatusType1,
                                          ChargingTariffGroupStatusType ChargingTariffGroupStatusType2)

            => ChargingTariffGroupStatusType1.CompareTo(ChargingTariffGroupStatusType2) > 0;

        #endregion

        #region Operator >= (ChargingTariffGroupStatusType1, ChargingTariffGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupStatusType1">A charging tariff group status type.</param>
        /// <param name="ChargingTariffGroupStatusType2">Another charging tariff group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingTariffGroupStatusType ChargingTariffGroupStatusType1,
                                           ChargingTariffGroupStatusType ChargingTariffGroupStatusType2)

            => ChargingTariffGroupStatusType1.CompareTo(ChargingTariffGroupStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingTariffGroupStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging tariff group status types.
        /// </summary>
        /// <param name="Object">A charging tariff group status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingTariffGroupStatusType chargingTariffGroupStatusType
                   ? CompareTo(chargingTariffGroupStatusType)
                   : throw new ArgumentException("The given object is not a charging tariff group status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingTariffGroupStatusType)

        /// <summary>
        /// Compares two charging tariff group status types.
        /// </summary>
        /// <param name="ChargingTariffGroupStatusType">A charging tariff group status type to compare with.</param>
        public Int32 CompareTo(ChargingTariffGroupStatusType ChargingTariffGroupStatusType)

            => String.Compare(InternalId,
                              ChargingTariffGroupStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChargingTariffGroupStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging tariff group status types for equality.
        /// </summary>
        /// <param name="Object">A charging tariff group status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariffGroupStatusType chargingTariffGroupStatusType &&
                   Equals(chargingTariffGroupStatusType);

        #endregion

        #region Equals(ChargingTariffGroupStatusType)

        /// <summary>
        /// Compares two charging tariff group status types for equality.
        /// </summary>
        /// <param name="ChargingTariffGroupStatusType">A charging tariff group status type to compare with.</param>
        public Boolean Equals(ChargingTariffGroupStatusType ChargingTariffGroupStatusType)

            => String.Equals(InternalId,
                             ChargingTariffGroupStatusType.InternalId,
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
