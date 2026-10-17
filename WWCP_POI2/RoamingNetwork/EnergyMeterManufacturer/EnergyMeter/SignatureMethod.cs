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
    /// Extension methods for signature methods.
    /// </summary>
    public static class SignatureMethodExtensions
    {

        /// <summary>
        /// Indicates whether this signature method is null or empty.
        /// </summary>
        /// <param name="SignatureMethod">A signature method.</param>
        public static Boolean IsNullOrEmpty(this SignatureMethod? SignatureMethod)
            => !SignatureMethod.HasValue || SignatureMethod.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this signature method is NOT null or empty.
        /// </summary>
        /// <param name="SignatureMethod">A signature method.</param>
        public static Boolean IsNotNullOrEmpty(this SignatureMethod? SignatureMethod)
            => SignatureMethod.HasValue && SignatureMethod.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// How an energy meter signs its measurements: the format of its signed meter values and how they are
    /// signed, as a transparency software verifies them, e.g. OCMF. The algorithm and the curve are those of
    /// the public key of the meter.
    /// </summary>
    /// <remarks>
    /// The predefined signature methods are the energy meter signature formats Chargy verifies
    /// (https://github.com/OpenChargingCloud/ChargyCore.NET, "EnergyMeterSignatureFormats").
    /// </remarks>
    public readonly struct SignatureMethod : IId<SignatureMethod>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this signature method is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this signature method is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the signature method.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new signature method based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a signature method.</param>
        private SignatureMethod(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a signature method.
        /// </summary>
        /// <param name="Text">A text representation of a signature method.</param>
        public static SignatureMethod Parse(String Text)
        {

            if (TryParse(Text, out var signatureMethod))
                return signatureMethod;

            throw new ArgumentException($"Invalid text representation of a signature method: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a signature method.
        /// </summary>
        /// <param name="Text">A text representation of a signature method.</param>
        public static SignatureMethod? TryParse(String Text)
        {

            if (TryParse(Text, out var signatureMethod))
                return signatureMethod;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out SignatureMethod)

        /// <summary>
        /// Try to parse the given text as a signature method.
        /// </summary>
        /// <param name="Text">A text representation of a signature method.</param>
        /// <param name="SignatureMethod">The parsed signature method.</param>
        public static Boolean TryParse(String Text, out SignatureMethod SignatureMethod)
        {

            Text = Text?.Trim() ?? "";

            // A predefined signature method in any case is the predefined one, so that the content,
            // and its ETags, do not depend on how it was written.
            foreach (var predefined in Predefined)
                if (String.Equals(predefined.InternalId, Text, StringComparison.OrdinalIgnoreCase))
                {
                    SignatureMethod = predefined;
                    return true;
                }

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    SignatureMethod = new SignatureMethod(Text);
                    return true;
                }
                catch
                { }
            }

            SignatureMethod = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this signature method.
        /// </summary>
        public SignatureMethod Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static Definitions

        /// <summary>
        /// The Open Charge Metering Format (OCMF) of S.A.F.E. e.V.
        /// </summary>
        public static SignatureMethod OCMF
            => new ("OCMF");

        /// <summary>
        /// The signed SML messages of EDL40 meters.
        /// </summary>
        public static SignatureMethod EDL40
            => new ("EDL40");

        /// <summary>
        /// The first signature format of Alfen.
        /// </summary>
        public static SignatureMethod AlfenCrypt01
            => new ("AlfenCrypt01");

        /// <summary>
        /// The third signature format of Alfen.
        /// </summary>
        public static SignatureMethod AlfenCrypt03
            => new ("AlfenCrypt03");

        /// <summary>
        /// The first signature format of ChargePoint.
        /// </summary>
        public static SignatureMethod ChargePointCrypt01
            => new ("ChargePointCrypt01");

        /// <summary>
        /// The first signature format of EMH.
        /// </summary>
        public static SignatureMethod EMHCrypt01
            => new ("EMHCrypt01");

        /// <summary>
        /// The first signature format of Mennekes.
        /// </summary>
        public static SignatureMethod MennekesCrypt01
            => new ("MennekesCrypt01");

        /// <summary>
        /// The PCDF signature format.
        /// </summary>
        public static SignatureMethod PCDF
            => new ("PCDF");

        /// <summary>
        /// All predefined signature methods.
        /// </summary>
        private static readonly SignatureMethod[] Predefined = [ OCMF, EDL40, AlfenCrypt01, AlfenCrypt03, ChargePointCrypt01, EMHCrypt01, MennekesCrypt01, PCDF ];

        #endregion


        #region Operator overloading

        #region Operator == (SignatureMethod1, SignatureMethod2)

        /// <summary>
        /// Compares two signature methods for equality.
        /// </summary>
        /// <param name="SignatureMethod1">A signature method.</param>
        /// <param name="SignatureMethod2">Another signature method.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (SignatureMethod SignatureMethod1,
                                           SignatureMethod SignatureMethod2)

            => SignatureMethod1.Equals(SignatureMethod2);

        #endregion

        #region Operator != (SignatureMethod1, SignatureMethod2)

        /// <summary>
        /// Compares two signature methods for inequality.
        /// </summary>
        /// <param name="SignatureMethod1">A signature method.</param>
        /// <param name="SignatureMethod2">Another signature method.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (SignatureMethod SignatureMethod1,
                                           SignatureMethod SignatureMethod2)

            => !SignatureMethod1.Equals(SignatureMethod2);

        #endregion

        #region Operator <  (SignatureMethod1, SignatureMethod2)

        /// <summary>
        /// Compares two signature methods.
        /// </summary>
        /// <param name="SignatureMethod1">A signature method.</param>
        /// <param name="SignatureMethod2">Another signature method.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (SignatureMethod SignatureMethod1,
                                          SignatureMethod SignatureMethod2)

            => SignatureMethod1.CompareTo(SignatureMethod2) < 0;

        #endregion

        #region Operator <= (SignatureMethod1, SignatureMethod2)

        /// <summary>
        /// Compares two signature methods.
        /// </summary>
        /// <param name="SignatureMethod1">A signature method.</param>
        /// <param name="SignatureMethod2">Another signature method.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (SignatureMethod SignatureMethod1,
                                           SignatureMethod SignatureMethod2)

            => SignatureMethod1.CompareTo(SignatureMethod2) <= 0;

        #endregion

        #region Operator >  (SignatureMethod1, SignatureMethod2)

        /// <summary>
        /// Compares two signature methods.
        /// </summary>
        /// <param name="SignatureMethod1">A signature method.</param>
        /// <param name="SignatureMethod2">Another signature method.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (SignatureMethod SignatureMethod1,
                                          SignatureMethod SignatureMethod2)

            => SignatureMethod1.CompareTo(SignatureMethod2) > 0;

        #endregion

        #region Operator >= (SignatureMethod1, SignatureMethod2)

        /// <summary>
        /// Compares two signature methods.
        /// </summary>
        /// <param name="SignatureMethod1">A signature method.</param>
        /// <param name="SignatureMethod2">Another signature method.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (SignatureMethod SignatureMethod1,
                                           SignatureMethod SignatureMethod2)

            => SignatureMethod1.CompareTo(SignatureMethod2) >= 0;

        #endregion

        #endregion

        #region IComparable<SignatureMethod> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two signature methods.
        /// </summary>
        /// <param name="Object">A signature method to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is SignatureMethod signatureMethod
                   ? CompareTo(signatureMethod)
                   : throw new ArgumentException("The given object is not a signature method!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(SignatureMethod)

        /// <summary>
        /// Compares two signature methods.
        /// </summary>
        /// <param name="SignatureMethod">A signature method to compare with.</param>
        public Int32 CompareTo(SignatureMethod SignatureMethod)

            => String.Compare(InternalId,
                              SignatureMethod.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<SignatureMethod> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two signature methods for equality.
        /// </summary>
        /// <param name="Object">A signature method to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is SignatureMethod signatureMethod &&
                   Equals(signatureMethod);

        #endregion

        #region Equals(SignatureMethod)

        /// <summary>
        /// Compares two signature methods for equality.
        /// </summary>
        /// <param name="SignatureMethod">A signature method to compare with.</param>
        public Boolean Equals(SignatureMethod SignatureMethod)

            => String.Equals(InternalId,
                             SignatureMethod.InternalId,
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
