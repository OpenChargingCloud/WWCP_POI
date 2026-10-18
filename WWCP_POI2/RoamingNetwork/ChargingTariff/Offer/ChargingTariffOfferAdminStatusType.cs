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
    /// Extension methods for charging tariff offer admin status types.
    /// </summary>
    public static class ChargingTariffOfferAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this charging tariff offer admin status types is null or empty.
        /// </summary>
        /// <param name="ChargingTariffOfferAdminStatusType">A charging tariff offer admin status type.</param>
        public static Boolean IsNullOrEmpty(this ChargingTariffOfferAdminStatusType? ChargingTariffOfferAdminStatusType)
            => !ChargingTariffOfferAdminStatusType.HasValue || ChargingTariffOfferAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this charging tariff offer admin status types is null or empty.
        /// </summary>
        /// <param name="ChargingTariffOfferAdminStatusType">A charging tariff offer admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this ChargingTariffOfferAdminStatusType? ChargingTariffOfferAdminStatusType)
            => ChargingTariffOfferAdminStatusType.HasValue && ChargingTariffOfferAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a charging tariff offer.
    /// </summary>
    public readonly struct ChargingTariffOfferAdminStatusType : IId,
                                                                    IEquatable<ChargingTariffOfferAdminStatusType>,
                                                                    IComparable<ChargingTariffOfferAdminStatusType>
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
        /// The length of the charging tariff offer admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging tariff offer admin status type based on the given string.
        /// </summary>
        private ChargingTariffOfferAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a charging tariff offer admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff offer admin status type.</param>
        public static ChargingTariffOfferAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out ChargingTariffOfferAdminStatusType chargingTariffOfferAdminStatusType))
                return chargingTariffOfferAdminStatusType;

            throw new ArgumentException($"Invalid text representation of a charging tariff offer admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a charging tariff offer admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff offer admin status type.</param>
        public static ChargingTariffOfferAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ChargingTariffOfferAdminStatusType chargingTariffOfferAdminStatusType))
                return chargingTariffOfferAdminStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ChargingTariffOfferAdminStatusType)

        /// <summary>
        /// Parse the given string as a charging tariff offer admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff offer admin status type.</param>
        /// <param name="ChargingTariffOfferAdminStatusType">The parsed charging tariff offer admin status type.</param>
        public static Boolean TryParse(String Text, out ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChargingTariffOfferAdminStatusType = new ChargingTariffOfferAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ChargingTariffOfferAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging tariff offer admin status type.
        /// </summary>
        public ChargingTariffOfferAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the charging tariff offer.
        /// </summary>
        public static readonly ChargingTariffOfferAdminStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear admin status of the charging tariff offer.
        /// </summary>
        public static readonly ChargingTariffOfferAdminStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The charging tariff offer is planned for the future.
        /// </summary>
        public static readonly ChargingTariffOfferAdminStatusType Planned      = new ("planned");

        /// <summary>
        /// The charging tariff offer is being deployed, not fully operational yet.
        /// </summary>
        public static readonly ChargingTariffOfferAdminStatusType InDeployment = new ("inDeployment");

        /// <summary>
        /// The charging tariff offer is operational.
        /// </summary>
        public static readonly ChargingTariffOfferAdminStatusType Operational  = new ("operational");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly ChargingTariffOfferAdminStatusType InternalUse  = new ("internalUse");

        /// <summary>
        /// The charging tariff offer is under maintenance.
        /// </summary>
        public static readonly ChargingTariffOfferAdminStatusType OutOfService = new ("outOfService");

        #endregion


        #region Operator overloading

        #region Operator == (ChargingTariffOfferAdminStatusType1, ChargingTariffOfferAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffOfferAdminStatusType1">A charging tariff offer admin status type.</param>
        /// <param name="ChargingTariffOfferAdminStatusType2">Another charging tariff offer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType1,
                                           ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType2)

            => ChargingTariffOfferAdminStatusType1.Equals(ChargingTariffOfferAdminStatusType2);

        #endregion

        #region Operator != (ChargingTariffOfferAdminStatusType1, ChargingTariffOfferAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffOfferAdminStatusType1">A charging tariff offer admin status type.</param>
        /// <param name="ChargingTariffOfferAdminStatusType2">Another charging tariff offer admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType1,
                                           ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType2)

            => !ChargingTariffOfferAdminStatusType1.Equals(ChargingTariffOfferAdminStatusType2);

        #endregion

        #region Operator <  (ChargingTariffOfferAdminStatusType1, ChargingTariffOfferAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffOfferAdminStatusType1">A charging tariff offer admin status type.</param>
        /// <param name="ChargingTariffOfferAdminStatusType2">Another charging tariff offer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType1,
                                          ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType2)

            => ChargingTariffOfferAdminStatusType1.CompareTo(ChargingTariffOfferAdminStatusType2) < 0;

        #endregion

        #region Operator <= (ChargingTariffOfferAdminStatusType1, ChargingTariffOfferAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffOfferAdminStatusType1">A charging tariff offer admin status type.</param>
        /// <param name="ChargingTariffOfferAdminStatusType2">Another charging tariff offer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType1,
                                           ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType2)

            => ChargingTariffOfferAdminStatusType1.CompareTo(ChargingTariffOfferAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (ChargingTariffOfferAdminStatusType1, ChargingTariffOfferAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffOfferAdminStatusType1">A charging tariff offer admin status type.</param>
        /// <param name="ChargingTariffOfferAdminStatusType2">Another charging tariff offer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType1,
                                          ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType2)

            => ChargingTariffOfferAdminStatusType1.CompareTo(ChargingTariffOfferAdminStatusType2) > 0;

        #endregion

        #region Operator >= (ChargingTariffOfferAdminStatusType1, ChargingTariffOfferAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffOfferAdminStatusType1">A charging tariff offer admin status type.</param>
        /// <param name="ChargingTariffOfferAdminStatusType2">Another charging tariff offer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType1,
                                           ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType2)

            => ChargingTariffOfferAdminStatusType1.CompareTo(ChargingTariffOfferAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingTariffOfferAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging tariff offer admin status types.
        /// </summary>
        /// <param name="Object">A charging tariff offer admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingTariffOfferAdminStatusType chargingTariffOfferAdminStatusType
                   ? CompareTo(chargingTariffOfferAdminStatusType)
                   : throw new ArgumentException("The given object is not a charging tariff offer admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingTariffOfferAdminStatusType)

        /// <summary>
        /// Compares two charging tariff offer admin status types.
        /// </summary>
        /// <param name="ChargingTariffOfferAdminStatusType">A charging tariff offer admin status type to compare with.</param>
        public Int32 CompareTo(ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType)

            => String.Compare(InternalId,
                              ChargingTariffOfferAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChargingTariffOfferAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging tariff offer admin status types for equality.
        /// </summary>
        /// <param name="Object">A charging tariff offer admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariffOfferAdminStatusType chargingTariffOfferAdminStatusType &&
                   Equals(chargingTariffOfferAdminStatusType);

        #endregion

        #region Equals(ChargingTariffOfferAdminStatusType)

        /// <summary>
        /// Compares two charging tariff offer admin status types for equality.
        /// </summary>
        /// <param name="ChargingTariffOfferAdminStatusType">A charging tariff offer admin status type to compare with.</param>
        public Boolean Equals(ChargingTariffOfferAdminStatusType ChargingTariffOfferAdminStatusType)

            => String.Equals(InternalId,
                             ChargingTariffOfferAdminStatusType.InternalId,
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
