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
    /// Extension methods for serial numbers.
    /// </summary>
    public static class SerialNumberExtensions
    {

        /// <summary>
        /// Indicates whether this serial number is null or empty.
        /// </summary>
        /// <param name="SerialNumber">A serial number.</param>
        public static Boolean IsNullOrEmpty(this SerialNumber? SerialNumber)
            => !SerialNumber.HasValue || SerialNumber.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this serial number is NOT null or empty.
        /// </summary>
        /// <param name="SerialNumber">A serial number.</param>
        public static Boolean IsNotNullOrEmpty(this SerialNumber? SerialNumber)
            => SerialNumber.HasValue && SerialNumber.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A serial number of a charging station or an energy meter, e.g. "1ESY1161229886", as its manufacturer marked it.
    /// </summary>
    /// <remarks>
    /// Unlike an identification it is taken exactly as it is given: not trimmed, and compared ordinally, case included, as
    /// the energy meter and a QI-Digital document of a charging station or an energy meter name it and sign it.
    /// </remarks>
    public readonly struct SerialNumber : IId<SerialNumber>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this serial number is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this serial number is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the serial number.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new serial number based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a serial number.</param>
        private SerialNumber(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a serial number.
        /// </summary>
        /// <param name="Text">A text representation of a serial number.</param>
        public static SerialNumber Parse(String Text)
        {

            if (TryParse(Text, out var serialNumber))
                return serialNumber;

            throw new ArgumentException($"Invalid text representation of a serial number: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a serial number.
        /// </summary>
        /// <param name="Text">A text representation of a serial number.</param>
        public static SerialNumber? TryParse(String Text)
        {

            if (TryParse(Text, out var serialNumber))
                return serialNumber;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out SerialNumber)

        /// <summary>
        /// Try to parse the given text as a serial number: not empty, and neither beginning nor ending with whitespace.
        /// </summary>
        /// <param name="Text">A text representation of a serial number.</param>
        /// <param name="SerialNumber">The parsed serial number.</param>
        public static Boolean TryParse(String Text, out SerialNumber SerialNumber)
        {

            if (!String.IsNullOrWhiteSpace(Text) &&
                Text == Text.Trim())
            {
                SerialNumber = new SerialNumber(Text);
                return true;
            }

            SerialNumber = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this serial number.
        /// </summary>
        public SerialNumber Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (SerialNumber1, SerialNumber2)

        /// <summary>
        /// Compares two serial numbers for equality.
        /// </summary>
        /// <param name="SerialNumber1">A serial number.</param>
        /// <param name="SerialNumber2">Another serial number.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (SerialNumber SerialNumber1,
                                           SerialNumber SerialNumber2)

            => SerialNumber1.Equals(SerialNumber2);

        #endregion

        #region Operator != (SerialNumber1, SerialNumber2)

        /// <summary>
        /// Compares two serial numbers for inequality.
        /// </summary>
        /// <param name="SerialNumber1">A serial number.</param>
        /// <param name="SerialNumber2">Another serial number.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (SerialNumber SerialNumber1,
                                           SerialNumber SerialNumber2)

            => !SerialNumber1.Equals(SerialNumber2);

        #endregion

        #region Operator <  (SerialNumber1, SerialNumber2)

        /// <summary>
        /// Compares two serial numbers.
        /// </summary>
        /// <param name="SerialNumber1">A serial number.</param>
        /// <param name="SerialNumber2">Another serial number.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (SerialNumber SerialNumber1,
                                          SerialNumber SerialNumber2)

            => SerialNumber1.CompareTo(SerialNumber2) < 0;

        #endregion

        #region Operator <= (SerialNumber1, SerialNumber2)

        /// <summary>
        /// Compares two serial numbers.
        /// </summary>
        /// <param name="SerialNumber1">A serial number.</param>
        /// <param name="SerialNumber2">Another serial number.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (SerialNumber SerialNumber1,
                                           SerialNumber SerialNumber2)

            => SerialNumber1.CompareTo(SerialNumber2) <= 0;

        #endregion

        #region Operator >  (SerialNumber1, SerialNumber2)

        /// <summary>
        /// Compares two serial numbers.
        /// </summary>
        /// <param name="SerialNumber1">A serial number.</param>
        /// <param name="SerialNumber2">Another serial number.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (SerialNumber SerialNumber1,
                                          SerialNumber SerialNumber2)

            => SerialNumber1.CompareTo(SerialNumber2) > 0;

        #endregion

        #region Operator >= (SerialNumber1, SerialNumber2)

        /// <summary>
        /// Compares two serial numbers.
        /// </summary>
        /// <param name="SerialNumber1">A serial number.</param>
        /// <param name="SerialNumber2">Another serial number.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (SerialNumber SerialNumber1,
                                           SerialNumber SerialNumber2)

            => SerialNumber1.CompareTo(SerialNumber2) >= 0;

        #endregion

        #endregion

        #region IComparable<SerialNumber> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two serial numbers.
        /// </summary>
        /// <param name="Object">A serial number to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is SerialNumber serialNumber
                   ? CompareTo(serialNumber)
                   : throw new ArgumentException("The given object is not a serial number!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(SerialNumber)

        /// <summary>
        /// Compares two serial numbers.
        /// </summary>
        /// <param name="SerialNumber">A serial number to compare with.</param>
        public Int32 CompareTo(SerialNumber SerialNumber)

            => String.Compare(InternalId,
                              SerialNumber.InternalId,
                              StringComparison.Ordinal);

        #endregion

        #endregion

        #region IEquatable<SerialNumber> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two serial numbers for equality.
        /// </summary>
        /// <param name="Object">A serial number to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is SerialNumber serialNumber &&
                   Equals(serialNumber);

        #endregion

        #region Equals(SerialNumber)

        /// <summary>
        /// Compares two serial numbers for equality.
        /// </summary>
        /// <param name="SerialNumber">A serial number to compare with.</param>
        public Boolean Equals(SerialNumber SerialNumber)

            => String.Equals(InternalId,
                             SerialNumber.InternalId,
                             StringComparison.Ordinal);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => InternalId is null ? 0 : StringComparer.Ordinal.GetHashCode(InternalId);

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
