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

using System.Text.RegularExpressions;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Extension methods for NUTS codes.
    /// </summary>
    public static class NUTSCodeExtensions
    {

        /// <summary>
        /// Indicates whether this NUTS code is null or empty.
        /// </summary>
        /// <param name="NUTSCode">A NUTS code.</param>
        public static Boolean IsNullOrEmpty(this NUTSCode? NUTSCode)
            => !NUTSCode.HasValue || NUTSCode.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this NUTS code is NOT null or empty.
        /// </summary>
        /// <param name="NUTSCode">A NUTS code.</param>
        public static Boolean IsNotNullOrEmpty(this NUTSCode? NUTSCode)
            => NUTSCode.HasValue && NUTSCode.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A code of the Nomenclature of Territorial Units for Statistics (NUTS) of the EU: two letters of the country
    /// and up to three letters or digits of the region, e.g. DEG (NUTS 1, Thüringen) or DEG03 (NUTS 3, Jena).
    /// Its level is the number of letters or digits after the country, 0 to 3. Read case-insensitively, written in
    /// upper case.
    /// </summary>
    public readonly struct NUTSCode : IId<NUTSCode>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        /// <summary>
        /// Two letters of the country, and up to three letters or digits of the region.
        /// </summary>
        private static readonly Regex NUTSCodeRegex = new ("^[A-Z]{2}[A-Z0-9]{0,3}$");

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this NUTS code is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this NUTS code is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the NUTS code.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        /// <summary>
        /// The country of the NUTS code, e.g. DE.
        /// </summary>
        public String CountryCode
            => InternalId?[..2] ?? "";

        /// <summary>
        /// The NUTS level, 0 (the country) to 3: the number of letters or digits after the country.
        /// </summary>
        public Byte Level
            => (Byte) Math.Max(0, (InternalId?.Length ?? 2) - 2);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new NUTS code based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a NUTS code.</param>
        private NUTSCode(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a NUTS code.
        /// </summary>
        /// <param name="Text">A text representation of a NUTS code.</param>
        public static NUTSCode Parse(String Text)
        {

            if (TryParse(Text, out var nutsCode))
                return nutsCode;

            throw new ArgumentException($"Invalid text representation of a NUTS code: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a NUTS code.
        /// </summary>
        /// <param name="Text">A text representation of a NUTS code.</param>
        public static NUTSCode? TryParse(String Text)
        {

            if (TryParse(Text, out var nutsCode))
                return nutsCode;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out NUTSCode)

        /// <summary>
        /// Try to parse the given text as a NUTS code.
        /// </summary>
        /// <param name="Text">A text representation of a NUTS code.</param>
        /// <param name="NUTSCode">The parsed NUTS code.</param>
        public static Boolean TryParse(String Text, out NUTSCode NUTSCode)
        {

            Text = Text?.Trim().ToUpperInvariant() ?? "";

            if (NUTSCodeRegex.IsMatch(Text))
            {
                NUTSCode = new NUTSCode(Text);
                return true;
            }

            NUTSCode = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this NUTS code.
        /// </summary>
        public NUTSCode Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (NUTSCode1, NUTSCode2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="NUTSCode1">A NUTS code.</param>
        /// <param name="NUTSCode2">Another NUTS code.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (NUTSCode NUTSCode1,
                                           NUTSCode NUTSCode2)

            => NUTSCode1.Equals(NUTSCode2);

        #endregion

        #region Operator != (NUTSCode1, NUTSCode2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="NUTSCode1">A NUTS code.</param>
        /// <param name="NUTSCode2">Another NUTS code.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (NUTSCode NUTSCode1,
                                           NUTSCode NUTSCode2)

            => !NUTSCode1.Equals(NUTSCode2);

        #endregion

        #region Operator <  (NUTSCode1, NUTSCode2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="NUTSCode1">A NUTS code.</param>
        /// <param name="NUTSCode2">Another NUTS code.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (NUTSCode NUTSCode1,
                                          NUTSCode NUTSCode2)

            => NUTSCode1.CompareTo(NUTSCode2) < 0;

        #endregion

        #region Operator <= (NUTSCode1, NUTSCode2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="NUTSCode1">A NUTS code.</param>
        /// <param name="NUTSCode2">Another NUTS code.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (NUTSCode NUTSCode1,
                                           NUTSCode NUTSCode2)

            => NUTSCode1.CompareTo(NUTSCode2) <= 0;

        #endregion

        #region Operator >  (NUTSCode1, NUTSCode2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="NUTSCode1">A NUTS code.</param>
        /// <param name="NUTSCode2">Another NUTS code.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (NUTSCode NUTSCode1,
                                          NUTSCode NUTSCode2)

            => NUTSCode1.CompareTo(NUTSCode2) > 0;

        #endregion

        #region Operator >= (NUTSCode1, NUTSCode2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="NUTSCode1">A NUTS code.</param>
        /// <param name="NUTSCode2">Another NUTS code.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (NUTSCode NUTSCode1,
                                           NUTSCode NUTSCode2)

            => NUTSCode1.CompareTo(NUTSCode2) >= 0;

        #endregion

        #endregion

        #region IComparable<NUTSCode> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two NUTS codes.
        /// </summary>
        /// <param name="Object">A NUTS code to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is NUTSCode nutsCode
                   ? CompareTo(nutsCode)
                   : throw new ArgumentException("The given object is not a NUTS code!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(NUTSCode)

        /// <summary>
        /// Compares two NUTS codes.
        /// </summary>
        /// <param name="NUTSCode">A NUTS code to compare with.</param>
        public Int32 CompareTo(NUTSCode NUTSCode)

            => String.Compare(InternalId,
                              NUTSCode.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<NUTSCode> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two NUTS codes for equality.
        /// </summary>
        /// <param name="Object">A NUTS code to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is NUTSCode nutsCode &&
                   Equals(nutsCode);

        #endregion

        #region Equals(NUTSCode)

        /// <summary>
        /// Compares two NUTS codes for equality.
        /// </summary>
        /// <param name="NUTSCode">A NUTS code to compare with.</param>
        public Boolean Equals(NUTSCode NUTSCode)

            => String.Equals(InternalId,
                             NUTSCode.InternalId,
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
