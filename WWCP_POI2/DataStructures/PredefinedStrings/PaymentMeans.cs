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
    /// Extension methods for payment means.
    /// </summary>
    public static class PaymentMeansExtensions
    {

        /// <summary>
        /// Indicates whether this payment means is null or empty.
        /// </summary>
        /// <param name="PaymentMeans">A payment means.</param>
        public static Boolean IsNullOrEmpty(this PaymentMeans? PaymentMeans)
            => !PaymentMeans.HasValue || PaymentMeans.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this payment means is NOT null or empty.
        /// </summary>
        /// <param name="PaymentMeans">A payment means.</param>
        public static Boolean IsNotNullOrEmpty(this PaymentMeans? PaymentMeans)
            => PaymentMeans.HasValue && PaymentMeans.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A means of payment, e.g. a credit card or a QR code; other texts are read as well.
    /// </summary>
    public readonly struct PaymentMeans : IId<PaymentMeans>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this payment means is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this payment means is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the payment means.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new payment means based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a payment means.</param>
        private PaymentMeans(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a payment means.
        /// </summary>
        /// <param name="Text">A text representation of a payment means.</param>
        public static PaymentMeans Parse(String Text)
        {

            if (TryParse(Text, out var paymentMeans))
                return paymentMeans;

            throw new ArgumentException($"Invalid text representation of a payment means: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a payment means.
        /// </summary>
        /// <param name="Text">A text representation of a payment means.</param>
        public static PaymentMeans? TryParse(String Text)
        {

            if (TryParse(Text, out var paymentMeans))
                return paymentMeans;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out PaymentMeans)

        /// <summary>
        /// Try to parse the given text as a payment means.
        /// </summary>
        /// <param name="Text">A text representation of a payment means.</param>
        /// <param name="PaymentMeans">The parsed payment means.</param>
        public static Boolean TryParse(String Text, out PaymentMeans PaymentMeans)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    PaymentMeans = new PaymentMeans(Text);
                    return true;
                }
                catch
                { }
            }

            PaymentMeans = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this payment means.
        /// </summary>
        public PaymentMeans Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// A credit card
        /// </summary>
        public static PaymentMeans CREDIT_CARD       { get; }
            = new ("CREDIT_CARD");

        /// <summary>
        /// A debit card
        /// </summary>
        public static PaymentMeans DEBIT_CARD        { get; }
            = new ("DEBIT_CARD");

        /// <summary>
        /// A prepaid value card
        /// </summary>
        public static PaymentMeans VALUE_CARD        { get; }
            = new ("VALUE_CARD");

        /// <summary>
        /// Cash: coins and bills
        /// </summary>
        public static PaymentMeans CASH              { get; }
            = new ("CASH");

        /// <summary>
        /// Coins only
        /// </summary>
        public static PaymentMeans CASH_COINS        { get; }
            = new ("CASH_COINS");

        /// <summary>
        /// Bills only
        /// </summary>
        public static PaymentMeans CASH_BILLS        { get; }
            = new ("CASH_BILLS");

        /// <summary>
        /// A mobile account, e.g. of a mobile phone
        /// </summary>
        public static PaymentMeans MOBILE_ACCOUNT    { get; }
            = new ("MOBILE_ACCOUNT");

        /// <summary>
        /// A toll tag
        /// </summary>
        public static PaymentMeans TOLL_TAG          { get; }
            = new ("TOLL_TAG");

        /// <summary>
        /// Paid in advance
        /// </summary>
        public static PaymentMeans PREPAY            { get; }
            = new ("PREPAY");

        /// <summary>
        /// Contactless, by near field communication
        /// </summary>
        public static PaymentMeans NFC               { get; }
            = new ("NFC");

        /// <summary>
        /// A payment card with a chip (EMV)
        /// </summary>
        public static PaymentMeans EMV               { get; }
            = new ("EMV");

        /// <summary>
        /// A QR code, e.g. leading to a payment page
        /// </summary>
        public static PaymentMeans QR_CODE           { get; }
            = new ("QR_CODE");

        /// <summary>
        /// A web page
        /// </summary>
        public static PaymentMeans WEBSITE           { get; }
            = new ("WEBSITE");

        #endregion


        #region Operator overloading

        #region Operator == (PaymentMeans1, PaymentMeans2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentMeans1">A payment means.</param>
        /// <param name="PaymentMeans2">Another payment means.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (PaymentMeans PaymentMeans1,
                                           PaymentMeans PaymentMeans2)

            => PaymentMeans1.Equals(PaymentMeans2);

        #endregion

        #region Operator != (PaymentMeans1, PaymentMeans2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentMeans1">A payment means.</param>
        /// <param name="PaymentMeans2">Another payment means.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (PaymentMeans PaymentMeans1,
                                           PaymentMeans PaymentMeans2)

            => !PaymentMeans1.Equals(PaymentMeans2);

        #endregion

        #region Operator <  (PaymentMeans1, PaymentMeans2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentMeans1">A payment means.</param>
        /// <param name="PaymentMeans2">Another payment means.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (PaymentMeans PaymentMeans1,
                                          PaymentMeans PaymentMeans2)

            => PaymentMeans1.CompareTo(PaymentMeans2) < 0;

        #endregion

        #region Operator <= (PaymentMeans1, PaymentMeans2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentMeans1">A payment means.</param>
        /// <param name="PaymentMeans2">Another payment means.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (PaymentMeans PaymentMeans1,
                                           PaymentMeans PaymentMeans2)

            => PaymentMeans1.CompareTo(PaymentMeans2) <= 0;

        #endregion

        #region Operator >  (PaymentMeans1, PaymentMeans2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentMeans1">A payment means.</param>
        /// <param name="PaymentMeans2">Another payment means.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (PaymentMeans PaymentMeans1,
                                          PaymentMeans PaymentMeans2)

            => PaymentMeans1.CompareTo(PaymentMeans2) > 0;

        #endregion

        #region Operator >= (PaymentMeans1, PaymentMeans2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentMeans1">A payment means.</param>
        /// <param name="PaymentMeans2">Another payment means.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (PaymentMeans PaymentMeans1,
                                           PaymentMeans PaymentMeans2)

            => PaymentMeans1.CompareTo(PaymentMeans2) >= 0;

        #endregion

        #endregion

        #region IComparable<PaymentMeans> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two payment means.
        /// </summary>
        /// <param name="Object">A payment means to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is PaymentMeans paymentMeans
                   ? CompareTo(paymentMeans)
                   : throw new ArgumentException("The given object is not a payment means!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(PaymentMeans)

        /// <summary>
        /// Compares two payment means.
        /// </summary>
        /// <param name="PaymentMeans">A payment means to compare with.</param>
        public Int32 CompareTo(PaymentMeans PaymentMeans)

            => String.Compare(InternalId,
                              PaymentMeans.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<PaymentMeans> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two payment means for equality.
        /// </summary>
        /// <param name="Object">A payment means to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is PaymentMeans paymentMeans &&
                   Equals(paymentMeans);

        #endregion

        #region Equals(PaymentMeans)

        /// <summary>
        /// Compares two payment means for equality.
        /// </summary>
        /// <param name="PaymentMeans">A payment means to compare with.</param>
        public Boolean Equals(PaymentMeans PaymentMeans)

            => String.Equals(InternalId,
                             PaymentMeans.InternalId,
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
