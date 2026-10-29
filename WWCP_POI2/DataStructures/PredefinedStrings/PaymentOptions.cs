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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// WWCP JSON I/O.
    /// </summary>
    public static partial class JSON_IO
    {

        #region ToJSON(this PaymentOptions)

        public static JArray? ToJSON(this IEnumerable<PaymentOptions> PaymentOptions)

            => PaymentOptions is not null
                   ? new JArray(PaymentOptions.Select(paymentOption => paymentOption.ToString()))
                   : null;

        #endregion

    }


    /// <summary>
    /// Extension methods for payment options.
    /// </summary>
    public static class PaymentOptionsExtensions
    {

        /// <summary>
        /// Indicates whether this payment option is null or empty.
        /// </summary>
        /// <param name="PaymentOption">A payment option.</param>
        public static Boolean IsNullOrEmpty(this PaymentOptions? PaymentOption)
            => !PaymentOption.HasValue || PaymentOption.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this payment option is NOT null or empty.
        /// </summary>
        /// <param name="PaymentOption">A payment option.</param>
        public static Boolean IsNotNullOrEmpty(this PaymentOptions? PaymentOption)
            => PaymentOption.HasValue && PaymentOption.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// How charging can be paid, e.g. with a contract or a credit card; other texts are read as well,
    /// so that a new option does not lock out readers that do not know it yet.
    /// </summary>
    public readonly struct PaymentOptions : IId<PaymentOptions>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this payment option is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this payment option is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the payment option.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new payment option based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a payment option.</param>
        private PaymentOptions(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a payment option.
        /// </summary>
        /// <param name="Text">A text representation of a payment option.</param>
        public static PaymentOptions Parse(String Text)
        {

            if (TryParse(Text, out var paymentOption))
                return paymentOption;

            throw new ArgumentException($"Invalid text representation of a payment option: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a payment option.
        /// </summary>
        /// <param name="Text">A text representation of a payment option.</param>
        public static PaymentOptions? TryParse(String Text)
        {

            if (TryParse(Text, out var paymentOption))
                return paymentOption;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out PaymentOption)

        /// <summary>
        /// Try to parse the given text as a payment option.
        /// </summary>
        /// <param name="Text">A text representation of a payment option.</param>
        /// <param name="PaymentOption">The parsed payment option.</param>
        public static Boolean TryParse(String Text, out PaymentOptions PaymentOption)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    PaymentOption = new PaymentOptions(Text);
                    return true;
                }
                catch
                { }
            }

            PaymentOption = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this payment option.
        /// </summary>
        public PaymentOptions Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// Free of charge
        /// </summary>
        public static PaymentOptions Free           { get; }
            = new ("Free");

        /// <summary>
        /// With a contract, e.g. of an e-mobility provider
        /// </summary>
        public static PaymentOptions Contract       { get; }
            = new ("Contract");

        /// <summary>
        /// Paid directly, without a contract
        /// </summary>
        public static PaymentOptions Direct         { get; }
            = new ("Direct");

        /// <summary>
        /// Cash
        /// </summary>
        public static PaymentOptions Cash           { get; }
            = new ("Cash");

        /// <summary>
        /// A credit card
        /// </summary>
        public static PaymentOptions CreditCard     { get; }
            = new ("CreditCard");

        /// <summary>
        /// A debit card
        /// </summary>
        public static PaymentOptions DebitCard      { get; }
            = new ("DebitCard");

        /// <summary>
        /// A prepaid card
        /// </summary>
        public static PaymentOptions PrepaidCard    { get; }
            = new ("PrepaidCard");

        /// <summary>
        /// A text message
        /// </summary>
        public static PaymentOptions SMS            { get; }
            = new ("SMS");

        /// <summary>
        /// GNU Taler, step by step, with the exchanges the charging station operator names in its EV Taler merchant
        /// (concepts/EVTaler.md)
        /// </summary>
        public static PaymentOptions EVTaler        { get; }
            = new ("EVTaler");

        #endregion


        #region Operator overloading

        #region Operator == (PaymentOption1, PaymentOption2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentOption1">A payment option.</param>
        /// <param name="PaymentOption2">Another payment option.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (PaymentOptions PaymentOption1,
                                           PaymentOptions PaymentOption2)

            => PaymentOption1.Equals(PaymentOption2);

        #endregion

        #region Operator != (PaymentOption1, PaymentOption2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentOption1">A payment option.</param>
        /// <param name="PaymentOption2">Another payment option.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (PaymentOptions PaymentOption1,
                                           PaymentOptions PaymentOption2)

            => !PaymentOption1.Equals(PaymentOption2);

        #endregion

        #region Operator <  (PaymentOption1, PaymentOption2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentOption1">A payment option.</param>
        /// <param name="PaymentOption2">Another payment option.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (PaymentOptions PaymentOption1,
                                          PaymentOptions PaymentOption2)

            => PaymentOption1.CompareTo(PaymentOption2) < 0;

        #endregion

        #region Operator <= (PaymentOption1, PaymentOption2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentOption1">A payment option.</param>
        /// <param name="PaymentOption2">Another payment option.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (PaymentOptions PaymentOption1,
                                           PaymentOptions PaymentOption2)

            => PaymentOption1.CompareTo(PaymentOption2) <= 0;

        #endregion

        #region Operator >  (PaymentOption1, PaymentOption2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentOption1">A payment option.</param>
        /// <param name="PaymentOption2">Another payment option.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (PaymentOptions PaymentOption1,
                                          PaymentOptions PaymentOption2)

            => PaymentOption1.CompareTo(PaymentOption2) > 0;

        #endregion

        #region Operator >= (PaymentOption1, PaymentOption2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PaymentOption1">A payment option.</param>
        /// <param name="PaymentOption2">Another payment option.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (PaymentOptions PaymentOption1,
                                           PaymentOptions PaymentOption2)

            => PaymentOption1.CompareTo(PaymentOption2) >= 0;

        #endregion

        #endregion

        #region IComparable<PaymentOptions> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two payment options.
        /// </summary>
        /// <param name="Object">A payment option to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is PaymentOptions paymentOption
                   ? CompareTo(paymentOption)
                   : throw new ArgumentException("The given object is not a payment option!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(PaymentOption)

        /// <summary>
        /// Compares two payment options.
        /// </summary>
        /// <param name="PaymentOption">A payment option to compare with.</param>
        public Int32 CompareTo(PaymentOptions PaymentOption)

            => String.Compare(InternalId,
                              PaymentOption.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<PaymentOptions> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two payment options for equality.
        /// </summary>
        /// <param name="Object">A payment option to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is PaymentOptions paymentOption &&
                   Equals(paymentOption);

        #endregion

        #region Equals(PaymentOption)

        /// <summary>
        /// Compares two payment options for equality.
        /// </summary>
        /// <param name="PaymentOption">A payment option to compare with.</param>
        public Boolean Equals(PaymentOptions PaymentOption)

            => String.Equals(InternalId,
                             PaymentOption.InternalId,
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
