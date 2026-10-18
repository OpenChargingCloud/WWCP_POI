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
    /// Extension methods for charging tariff offer identifications.
    /// </summary>
    public static class ChargingTariffOfferIdExtensions
    {

        /// <summary>
        /// Indicates whether this charging tariff offer identification is null or empty.
        /// </summary>
        /// <param name="ChargingTariffOfferId">A charging tariff offer identification.</param>
        public static Boolean IsNullOrEmpty(this ChargingTariffOffer_Id? ChargingTariffOfferId)
            => !ChargingTariffOfferId.HasValue || ChargingTariffOfferId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this charging tariff offer identification is NOT null or empty.
        /// </summary>
        /// <param name="ChargingTariffOfferId">A charging tariff offer identification.</param>
        public static Boolean IsNotNullOrEmpty(this ChargingTariffOffer_Id? ChargingTariffOfferId)
            => ChargingTariffOfferId.HasValue && ChargingTariffOfferId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a charging tariff offer.
    /// </summary>
    public readonly struct ChargingTariffOffer_Id : IId<ChargingTariffOffer_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this charging tariff offer identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this charging tariff offer identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the charging tariff offer identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging tariff offer identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a charging tariff offer identification.</param>
        private ChargingTariffOffer_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) New

        /// <summary>
        /// Create a new unique charging tariff offer identification (UUIDv7).
        /// </summary>
        public static ChargingTariffOffer_Id New

            => new (UUIDv7.Generate().ToString());

        #endregion

        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random charging tariff offer identification.
        /// </summary>
        /// <param name="Length">The expected length of the charging tariff offer identification.</param>
        public static ChargingTariffOffer_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a charging tariff offer identification.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff offer identification.</param>
        public static ChargingTariffOffer_Id Parse(String Text)
        {

            if (TryParse(Text, out var chargingTariffOfferId))
                return chargingTariffOfferId;

            throw new ArgumentException($"Invalid text representation of a charging tariff offer identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a charging tariff offer identification.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff offer identification.</param>
        public static ChargingTariffOffer_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var chargingTariffOfferId))
                return chargingTariffOfferId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ChargingTariffOfferId)

        /// <summary>
        /// Try to parse the given text as a charging tariff offer identification.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff offer identification.</param>
        /// <param name="ChargingTariffOfferId">The parsed charging tariff offer identification.</param>
        public static Boolean TryParse(String Text, out ChargingTariffOffer_Id ChargingTariffOfferId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChargingTariffOfferId = new ChargingTariffOffer_Id(Text);
                    return true;
                }
                catch
                { }
            }

            ChargingTariffOfferId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging tariff offer identification.
        /// </summary>
        public ChargingTariffOffer_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ChargingTariffOfferId1, ChargingTariffOfferId2)

        /// <summary>
        /// Compares two charging tariff offer identifications for equality.
        /// </summary>
        /// <param name="ChargingTariffOfferId1">A charging tariff offer identification.</param>
        /// <param name="ChargingTariffOfferId2">Another charging tariff offer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingTariffOffer_Id ChargingTariffOfferId1,
                                           ChargingTariffOffer_Id ChargingTariffOfferId2)

            => ChargingTariffOfferId1.Equals(ChargingTariffOfferId2);

        #endregion

        #region Operator != (ChargingTariffOfferId1, ChargingTariffOfferId2)

        /// <summary>
        /// Compares two charging tariff offer identifications for inequality.
        /// </summary>
        /// <param name="ChargingTariffOfferId1">A charging tariff offer identification.</param>
        /// <param name="ChargingTariffOfferId2">Another charging tariff offer identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingTariffOffer_Id ChargingTariffOfferId1,
                                           ChargingTariffOffer_Id ChargingTariffOfferId2)

            => !ChargingTariffOfferId1.Equals(ChargingTariffOfferId2);

        #endregion

        #region Operator <  (ChargingTariffOfferId1, ChargingTariffOfferId2)

        /// <summary>
        /// Compares two charging tariff offer identifications.
        /// </summary>
        /// <param name="ChargingTariffOfferId1">A charging tariff offer identification.</param>
        /// <param name="ChargingTariffOfferId2">Another charging tariff offer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingTariffOffer_Id ChargingTariffOfferId1,
                                          ChargingTariffOffer_Id ChargingTariffOfferId2)

            => ChargingTariffOfferId1.CompareTo(ChargingTariffOfferId2) < 0;

        #endregion

        #region Operator <= (ChargingTariffOfferId1, ChargingTariffOfferId2)

        /// <summary>
        /// Compares two charging tariff offer identifications.
        /// </summary>
        /// <param name="ChargingTariffOfferId1">A charging tariff offer identification.</param>
        /// <param name="ChargingTariffOfferId2">Another charging tariff offer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingTariffOffer_Id ChargingTariffOfferId1,
                                           ChargingTariffOffer_Id ChargingTariffOfferId2)

            => ChargingTariffOfferId1.CompareTo(ChargingTariffOfferId2) <= 0;

        #endregion

        #region Operator >  (ChargingTariffOfferId1, ChargingTariffOfferId2)

        /// <summary>
        /// Compares two charging tariff offer identifications.
        /// </summary>
        /// <param name="ChargingTariffOfferId1">A charging tariff offer identification.</param>
        /// <param name="ChargingTariffOfferId2">Another charging tariff offer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingTariffOffer_Id ChargingTariffOfferId1,
                                          ChargingTariffOffer_Id ChargingTariffOfferId2)

            => ChargingTariffOfferId1.CompareTo(ChargingTariffOfferId2) > 0;

        #endregion

        #region Operator >= (ChargingTariffOfferId1, ChargingTariffOfferId2)

        /// <summary>
        /// Compares two charging tariff offer identifications.
        /// </summary>
        /// <param name="ChargingTariffOfferId1">A charging tariff offer identification.</param>
        /// <param name="ChargingTariffOfferId2">Another charging tariff offer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingTariffOffer_Id ChargingTariffOfferId1,
                                           ChargingTariffOffer_Id ChargingTariffOfferId2)

            => ChargingTariffOfferId1.CompareTo(ChargingTariffOfferId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingTariffOfferId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging tariff offer identifications.
        /// </summary>
        /// <param name="Object">A charging tariff offer identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingTariffOffer_Id chargingTariffOfferId
                   ? CompareTo(chargingTariffOfferId)
                   : throw new ArgumentException("The given object is not a charging tariff offer identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingTariffOfferId)

        /// <summary>
        /// Compares two charging tariff offer identifications.
        /// </summary>
        /// <param name="ChargingTariffOfferId">A charging tariff offer identification to compare with.</param>
        public Int32 CompareTo(ChargingTariffOffer_Id ChargingTariffOfferId)

            => String.Compare(InternalId,
                              ChargingTariffOfferId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChargingTariffOfferId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging tariff offer identifications for equality.
        /// </summary>
        /// <param name="Object">A charging tariff offer identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariffOffer_Id chargingTariffOfferId &&
                   Equals(chargingTariffOfferId);

        #endregion

        #region Equals(ChargingTariffOfferId)

        /// <summary>
        /// Compares two charging tariff offer identifications for equality.
        /// </summary>
        /// <param name="ChargingTariffOfferId">A charging tariff offer identification to compare with.</param>
        public Boolean Equals(ChargingTariffOffer_Id ChargingTariffOfferId)

            => String.Equals(InternalId,
                             ChargingTariffOfferId.InternalId,
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
