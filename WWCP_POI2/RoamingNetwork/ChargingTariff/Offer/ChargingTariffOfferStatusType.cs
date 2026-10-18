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
    /// Extension methods for charging tariff offer status types.
    /// </summary>
    public static class ChargingTariffOfferStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this charging tariff offer status types is null or empty.
        /// </summary>
        /// <param name="ChargingTariffOfferStatusType">A charging tariff offer status type.</param>
        public static Boolean IsNullOrEmpty(this ChargingTariffOfferStatusType? ChargingTariffOfferStatusType)
            => !ChargingTariffOfferStatusType.HasValue || ChargingTariffOfferStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this charging tariff offer status types is null or empty.
        /// </summary>
        /// <param name="ChargingTariffOfferStatusType">A charging tariff offer status type.</param>
        public static Boolean IsNotNullOrEmpty(this ChargingTariffOfferStatusType? ChargingTariffOfferStatusType)
            => ChargingTariffOfferStatusType.HasValue && ChargingTariffOfferStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of a charging tariff offer.
    /// </summary>
    public readonly struct ChargingTariffOfferStatusType : IId,
                                                               IEquatable<ChargingTariffOfferStatusType>,
                                                               IComparable<ChargingTariffOfferStatusType>
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
        /// The length of the charging tariff offer status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging tariff offer status type based on the given string.
        /// </summary>
        private ChargingTariffOfferStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a charging tariff offer status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff offer status type.</param>
        public static ChargingTariffOfferStatusType Parse(String Text)
        {

            if (TryParse(Text, out ChargingTariffOfferStatusType chargingTariffOfferStatusType))
                return chargingTariffOfferStatusType;

            throw new ArgumentException($"Invalid text representation of a charging tariff offer status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a charging tariff offer status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff offer status type.</param>
        public static ChargingTariffOfferStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ChargingTariffOfferStatusType chargingTariffOfferStatusType))
                return chargingTariffOfferStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ChargingTariffOfferStatusType)

        /// <summary>
        /// Parse the given string as a charging tariff offer status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff offer status type.</param>
        /// <param name="ChargingTariffOfferStatusType">The parsed charging tariff offer status type.</param>
        public static Boolean TryParse(String Text, out ChargingTariffOfferStatusType ChargingTariffOfferStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChargingTariffOfferStatusType = new ChargingTariffOfferStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ChargingTariffOfferStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging tariff offer status type.
        /// </summary>
        public ChargingTariffOfferStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the charging tariff offer.
        /// </summary>
        public static readonly ChargingTariffOfferStatusType Unknown     = new ("unknown");

        /// <summary>
        /// Unclear status of the charging tariff offer.
        /// </summary>
        public static readonly ChargingTariffOfferStatusType Unspecified = new ("unspecified");

        /// <summary>
        /// The charging tariff offer applies from its notBefore on, which is still to come.
        /// </summary>
        public static readonly ChargingTariffOfferStatusType Planned     = new ("planned");

        /// <summary>
        /// The charging tariff offer applies, within its opening times.
        /// </summary>
        public static readonly ChargingTariffOfferStatusType Active      = new ("active");

        /// <summary>
        /// The charging tariff offer ended before its notAfter, as its owner set it, e.g. when its budget is used up.
        /// </summary>
        public static readonly ChargingTariffOfferStatusType Exhausted   = new ("exhausted");

        /// <summary>
        /// The charging tariff offer ended after its notAfter.
        /// </summary>
        public static readonly ChargingTariffOfferStatusType Expired     = new ("expired");

        #endregion


        #region Operator overloading

        #region Operator == (ChargingTariffOfferStatusType1, ChargingTariffOfferStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffOfferStatusType1">A charging tariff offer status type.</param>
        /// <param name="ChargingTariffOfferStatusType2">Another charging tariff offer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingTariffOfferStatusType ChargingTariffOfferStatusType1,
                                           ChargingTariffOfferStatusType ChargingTariffOfferStatusType2)

            => ChargingTariffOfferStatusType1.Equals(ChargingTariffOfferStatusType2);

        #endregion

        #region Operator != (ChargingTariffOfferStatusType1, ChargingTariffOfferStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffOfferStatusType1">A charging tariff offer status type.</param>
        /// <param name="ChargingTariffOfferStatusType2">Another charging tariff offer status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingTariffOfferStatusType ChargingTariffOfferStatusType1,
                                           ChargingTariffOfferStatusType ChargingTariffOfferStatusType2)

            => !ChargingTariffOfferStatusType1.Equals(ChargingTariffOfferStatusType2);

        #endregion

        #region Operator <  (ChargingTariffOfferStatusType1, ChargingTariffOfferStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffOfferStatusType1">A charging tariff offer status type.</param>
        /// <param name="ChargingTariffOfferStatusType2">Another charging tariff offer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingTariffOfferStatusType ChargingTariffOfferStatusType1,
                                          ChargingTariffOfferStatusType ChargingTariffOfferStatusType2)

            => ChargingTariffOfferStatusType1.CompareTo(ChargingTariffOfferStatusType2) < 0;

        #endregion

        #region Operator <= (ChargingTariffOfferStatusType1, ChargingTariffOfferStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffOfferStatusType1">A charging tariff offer status type.</param>
        /// <param name="ChargingTariffOfferStatusType2">Another charging tariff offer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingTariffOfferStatusType ChargingTariffOfferStatusType1,
                                           ChargingTariffOfferStatusType ChargingTariffOfferStatusType2)

            => ChargingTariffOfferStatusType1.CompareTo(ChargingTariffOfferStatusType2) <= 0;

        #endregion

        #region Operator >  (ChargingTariffOfferStatusType1, ChargingTariffOfferStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffOfferStatusType1">A charging tariff offer status type.</param>
        /// <param name="ChargingTariffOfferStatusType2">Another charging tariff offer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingTariffOfferStatusType ChargingTariffOfferStatusType1,
                                          ChargingTariffOfferStatusType ChargingTariffOfferStatusType2)

            => ChargingTariffOfferStatusType1.CompareTo(ChargingTariffOfferStatusType2) > 0;

        #endregion

        #region Operator >= (ChargingTariffOfferStatusType1, ChargingTariffOfferStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffOfferStatusType1">A charging tariff offer status type.</param>
        /// <param name="ChargingTariffOfferStatusType2">Another charging tariff offer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingTariffOfferStatusType ChargingTariffOfferStatusType1,
                                           ChargingTariffOfferStatusType ChargingTariffOfferStatusType2)

            => ChargingTariffOfferStatusType1.CompareTo(ChargingTariffOfferStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingTariffOfferStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging tariff offer status types.
        /// </summary>
        /// <param name="Object">A charging tariff offer status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingTariffOfferStatusType chargingTariffOfferStatusType
                   ? CompareTo(chargingTariffOfferStatusType)
                   : throw new ArgumentException("The given object is not a charging tariff offer status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingTariffOfferStatusType)

        /// <summary>
        /// Compares two charging tariff offer status types.
        /// </summary>
        /// <param name="ChargingTariffOfferStatusType">A charging tariff offer status type to compare with.</param>
        public Int32 CompareTo(ChargingTariffOfferStatusType ChargingTariffOfferStatusType)

            => String.Compare(InternalId,
                              ChargingTariffOfferStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChargingTariffOfferStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging tariff offer status types for equality.
        /// </summary>
        /// <param name="Object">A charging tariff offer status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariffOfferStatusType chargingTariffOfferStatusType &&
                   Equals(chargingTariffOfferStatusType);

        #endregion

        #region Equals(ChargingTariffOfferStatusType)

        /// <summary>
        /// Compares two charging tariff offer status types for equality.
        /// </summary>
        /// <param name="ChargingTariffOfferStatusType">A charging tariff offer status type to compare with.</param>
        public Boolean Equals(ChargingTariffOfferStatusType ChargingTariffOfferStatusType)

            => String.Equals(InternalId,
                             ChargingTariffOfferStatusType.InternalId,
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
