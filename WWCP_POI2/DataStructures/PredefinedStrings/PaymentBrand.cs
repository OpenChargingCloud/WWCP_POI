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
    /// Extension methods for payment brands.
    /// </summary>
    public static class PaymentBrandExtensions
    {

        /// <summary>
        /// Indicates whether this payment brand is null or empty.
        /// </summary>
        /// <param name="PaymentBrand">A payment brand.</param>
        public static Boolean IsNullOrEmpty(this PaymentBrand? PaymentBrand)
            => !PaymentBrand.HasValue || PaymentBrand.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this payment brand is NOT null or empty.
        /// </summary>
        /// <param name="PaymentBrand">A payment brand.</param>
        public static Boolean IsNotNullOrEmpty(this PaymentBrand? PaymentBrand)
            => PaymentBrand.HasValue && PaymentBrand.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A payment brand, e.g. Visa; other texts, e.g. Google Pay, are read as well.
    /// </summary>
    public readonly struct PaymentBrand : IId<PaymentBrand>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this payment brand is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this payment brand is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the payment brand.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new payment brand based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a payment brand.</param>
        private PaymentBrand(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a payment brand.
        /// </summary>
        /// <param name="Text">A text representation of a payment brand.</param>
        public static PaymentBrand Parse(String Text)
        {

            if (TryParse(Text, out var paymentBrand))
                return paymentBrand;

            throw new ArgumentException($"Invalid text representation of a payment brand: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a payment brand.
        /// </summary>
        /// <param name="Text">A text representation of a payment brand.</param>
        public static PaymentBrand? TryParse(String Text)
        {

            if (TryParse(Text, out var paymentBrand))
                return paymentBrand;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out PaymentBrand)

        /// <summary>
        /// Try to parse the given text as a payment brand.
        /// </summary>
        /// <param name="Text">A text representation of a payment brand.</param>
        /// <param name="PaymentBrand">The parsed payment brand.</param>
        public static Boolean TryParse(String Text, out PaymentBrand PaymentBrand)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    PaymentBrand = new PaymentBrand(Text);
                    return true;
                }
                catch
                { }
            }

            PaymentBrand = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this payment brand.
        /// </summary>
        public PaymentBrand Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// American Express
        /// </summary>
        public static PaymentBrand AMERICAN_EXPRESS    { get; }
            = new ("AMERICAN_EXPRESS");

        /// <summary>
        /// Apple Pay
        /// </summary>
        public static PaymentBrand APPLE_PAY           { get; }
            = new ("APPLE_PAY");

        /// <summary>
        /// Cirrus
        /// </summary>
        public static PaymentBrand CIRRUS              { get; }
            = new ("CIRRUS");

        /// <summary>
        /// Diners Club
        /// </summary>
        public static PaymentBrand DINERS_CLUB         { get; }
            = new ("DINERS_CLUB");

        /// <summary>
        /// Discover
        /// </summary>
        public static PaymentBrand DISCOVER            { get; }
            = new ("DISCOVER");

        /// <summary>
        /// girocard
        /// </summary>
        public static PaymentBrand GIROCARD            { get; }
            = new ("GIROCARD");

        /// <summary>
        /// Maestro
        /// </summary>
        public static PaymentBrand MAESTRO             { get; }
            = new ("MAESTRO");

        /// <summary>
        /// Mastercard
        /// </summary>
        public static PaymentBrand MASTERCARD          { get; }
            = new ("MASTERCARD");

        /// <summary>
        /// Visa
        /// </summary>
        public static PaymentBrand VISA                { get; }
            = new ("VISA");

        /// <summary>
        /// V PAY
        /// </summary>
        public static PaymentBrand VPAY                { get; }
            = new ("VPAY");

        #endregion


        #region Operator overloading

        #region Operator == (PaymentBrand1, PaymentBrand2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentBrand1">A payment brand.</param>
        /// <param name="PaymentBrand2">Another payment brand.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (PaymentBrand PaymentBrand1,
                                           PaymentBrand PaymentBrand2)

            => PaymentBrand1.Equals(PaymentBrand2);

        #endregion

        #region Operator != (PaymentBrand1, PaymentBrand2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentBrand1">A payment brand.</param>
        /// <param name="PaymentBrand2">Another payment brand.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (PaymentBrand PaymentBrand1,
                                           PaymentBrand PaymentBrand2)

            => !PaymentBrand1.Equals(PaymentBrand2);

        #endregion

        #region Operator <  (PaymentBrand1, PaymentBrand2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentBrand1">A payment brand.</param>
        /// <param name="PaymentBrand2">Another payment brand.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (PaymentBrand PaymentBrand1,
                                          PaymentBrand PaymentBrand2)

            => PaymentBrand1.CompareTo(PaymentBrand2) < 0;

        #endregion

        #region Operator <= (PaymentBrand1, PaymentBrand2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentBrand1">A payment brand.</param>
        /// <param name="PaymentBrand2">Another payment brand.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (PaymentBrand PaymentBrand1,
                                           PaymentBrand PaymentBrand2)

            => PaymentBrand1.CompareTo(PaymentBrand2) <= 0;

        #endregion

        #region Operator >  (PaymentBrand1, PaymentBrand2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentBrand1">A payment brand.</param>
        /// <param name="PaymentBrand2">Another payment brand.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (PaymentBrand PaymentBrand1,
                                          PaymentBrand PaymentBrand2)

            => PaymentBrand1.CompareTo(PaymentBrand2) > 0;

        #endregion

        #region Operator >= (PaymentBrand1, PaymentBrand2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentBrand1">A payment brand.</param>
        /// <param name="PaymentBrand2">Another payment brand.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (PaymentBrand PaymentBrand1,
                                           PaymentBrand PaymentBrand2)

            => PaymentBrand1.CompareTo(PaymentBrand2) >= 0;

        #endregion

        #endregion

        #region IComparable<PaymentBrand> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two payment brands.
        /// </summary>
        /// <param name="Object">A payment brand to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is PaymentBrand paymentBrand
                   ? CompareTo(paymentBrand)
                   : throw new ArgumentException("The given object is not a payment brand!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(PaymentBrand)

        /// <summary>
        /// Compares two payment brands.
        /// </summary>
        /// <param name="PaymentBrand">A payment brand to compare with.</param>
        public Int32 CompareTo(PaymentBrand PaymentBrand)

            => String.Compare(InternalId,
                              PaymentBrand.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<PaymentBrand> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two payment brands for equality.
        /// </summary>
        /// <param name="Object">A payment brand to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is PaymentBrand paymentBrand &&
                   Equals(paymentBrand);

        #endregion

        #region Equals(PaymentBrand)

        /// <summary>
        /// Compares two payment brands for equality.
        /// </summary>
        /// <param name="PaymentBrand">A payment brand to compare with.</param>
        public Boolean Equals(PaymentBrand PaymentBrand)

            => String.Equals(InternalId,
                             PaymentBrand.InternalId,
                             StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => InternalId?.ToUpper().GetHashCode() ?? 0;

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
