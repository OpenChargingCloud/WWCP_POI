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
    /// Extension methods for charging tariff status types.
    /// </summary>
    public static class ChargingTariffStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this charging tariff status types is null or empty.
        /// </summary>
        /// <param name="ChargingTariffStatusType">A charging tariff status type.</param>
        public static Boolean IsNullOrEmpty(this ChargingTariffStatusType? ChargingTariffStatusType)
            => !ChargingTariffStatusType.HasValue || ChargingTariffStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this charging tariff status types is null or empty.
        /// </summary>
        /// <param name="ChargingTariffStatusType">A charging tariff status type.</param>
        public static Boolean IsNotNullOrEmpty(this ChargingTariffStatusType? ChargingTariffStatusType)
            => ChargingTariffStatusType.HasValue && ChargingTariffStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of a charging tariff.
    /// </summary>
    public readonly struct ChargingTariffStatusType : IId,
                                                               IEquatable<ChargingTariffStatusType>,
                                                               IComparable<ChargingTariffStatusType>
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
        /// The length of the charging tariff status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging tariff status type based on the given string.
        /// </summary>
        private ChargingTariffStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a charging tariff status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff status type.</param>
        public static ChargingTariffStatusType Parse(String Text)
        {

            if (TryParse(Text, out ChargingTariffStatusType chargingTariffStatusType))
                return chargingTariffStatusType;

            throw new ArgumentException($"Invalid text representation of a charging tariff status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a charging tariff status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff status type.</param>
        public static ChargingTariffStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ChargingTariffStatusType chargingTariffStatusType))
                return chargingTariffStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ChargingTariffStatusType)

        /// <summary>
        /// Parse the given string as a charging tariff status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff status type.</param>
        /// <param name="ChargingTariffStatusType">The parsed charging tariff status type.</param>
        public static Boolean TryParse(String Text, out ChargingTariffStatusType ChargingTariffStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChargingTariffStatusType = new ChargingTariffStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ChargingTariffStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging tariff status type.
        /// </summary>
        public ChargingTariffStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the charging tariff.
        /// </summary>
        public static readonly ChargingTariffStatusType Unknown     = new ("unknown");

        /// <summary>
        /// Unclear status of the charging tariff.
        /// </summary>
        public static readonly ChargingTariffStatusType Unspecified = new ("unspecified");

        /// <summary>
        /// The charging tariff is planned for the future.
        /// </summary>
        public static readonly ChargingTariffStatusType Planned     = new ("planned");

        /// <summary>
        /// The charging tariff is currently in use.
        /// </summary>
        public static readonly ChargingTariffStatusType Active      = new ("active");

        /// <summary>
        /// The charging tariff is no longer in use.
        /// </summary>
        public static readonly ChargingTariffStatusType Expired     = new ("expired");

        /// <summary>
        /// Another status of the charging tariff.
        /// </summary>
        public static readonly ChargingTariffStatusType Other       = new ("other");

        #endregion


        #region Operator overloading

        #region Operator == (ChargingTariffStatusType1, ChargingTariffStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffStatusType1">A charging tariff status type.</param>
        /// <param name="ChargingTariffStatusType2">Another charging tariff status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingTariffStatusType ChargingTariffStatusType1,
                                           ChargingTariffStatusType ChargingTariffStatusType2)

            => ChargingTariffStatusType1.Equals(ChargingTariffStatusType2);

        #endregion

        #region Operator != (ChargingTariffStatusType1, ChargingTariffStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffStatusType1">A charging tariff status type.</param>
        /// <param name="ChargingTariffStatusType2">Another charging tariff status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingTariffStatusType ChargingTariffStatusType1,
                                           ChargingTariffStatusType ChargingTariffStatusType2)

            => !ChargingTariffStatusType1.Equals(ChargingTariffStatusType2);

        #endregion

        #region Operator <  (ChargingTariffStatusType1, ChargingTariffStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffStatusType1">A charging tariff status type.</param>
        /// <param name="ChargingTariffStatusType2">Another charging tariff status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingTariffStatusType ChargingTariffStatusType1,
                                          ChargingTariffStatusType ChargingTariffStatusType2)

            => ChargingTariffStatusType1.CompareTo(ChargingTariffStatusType2) < 0;

        #endregion

        #region Operator <= (ChargingTariffStatusType1, ChargingTariffStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffStatusType1">A charging tariff status type.</param>
        /// <param name="ChargingTariffStatusType2">Another charging tariff status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingTariffStatusType ChargingTariffStatusType1,
                                           ChargingTariffStatusType ChargingTariffStatusType2)

            => ChargingTariffStatusType1.CompareTo(ChargingTariffStatusType2) <= 0;

        #endregion

        #region Operator >  (ChargingTariffStatusType1, ChargingTariffStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffStatusType1">A charging tariff status type.</param>
        /// <param name="ChargingTariffStatusType2">Another charging tariff status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingTariffStatusType ChargingTariffStatusType1,
                                          ChargingTariffStatusType ChargingTariffStatusType2)

            => ChargingTariffStatusType1.CompareTo(ChargingTariffStatusType2) > 0;

        #endregion

        #region Operator >= (ChargingTariffStatusType1, ChargingTariffStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffStatusType1">A charging tariff status type.</param>
        /// <param name="ChargingTariffStatusType2">Another charging tariff status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingTariffStatusType ChargingTariffStatusType1,
                                           ChargingTariffStatusType ChargingTariffStatusType2)

            => ChargingTariffStatusType1.CompareTo(ChargingTariffStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingTariffStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging tariff status types.
        /// </summary>
        /// <param name="Object">A charging tariff status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingTariffStatusType chargingTariffStatusType
                   ? CompareTo(chargingTariffStatusType)
                   : throw new ArgumentException("The given object is not a charging tariff status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingTariffStatusType)

        /// <summary>
        /// Compares two charging tariff status types.
        /// </summary>
        /// <param name="ChargingTariffStatusType">A charging tariff status type to compare with.</param>
        public Int32 CompareTo(ChargingTariffStatusType ChargingTariffStatusType)

            => String.Compare(InternalId,
                              ChargingTariffStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChargingTariffStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging tariff status types for equality.
        /// </summary>
        /// <param name="Object">A charging tariff status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariffStatusType chargingTariffStatusType &&
                   Equals(chargingTariffStatusType);

        #endregion

        #region Equals(ChargingTariffStatusType)

        /// <summary>
        /// Compares two charging tariff status types for equality.
        /// </summary>
        /// <param name="ChargingTariffStatusType">A charging tariff status type to compare with.</param>
        public Boolean Equals(ChargingTariffStatusType ChargingTariffStatusType)

            => String.Equals(InternalId,
                             ChargingTariffStatusType.InternalId,
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
