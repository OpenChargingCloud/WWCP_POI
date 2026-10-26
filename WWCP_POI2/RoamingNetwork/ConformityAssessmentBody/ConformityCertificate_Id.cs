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
    /// Extension methods for certificate identifications.
    /// </summary>
    public static class ConformityCertificateIdExtensions
    {

        /// <summary>
        /// Indicates whether this certificate identification is null or empty.
        /// </summary>
        /// <param name="ConformityCertificateId">A certificate identification.</param>
        public static Boolean IsNullOrEmpty(this ConformityCertificate_Id? ConformityCertificateId)
            => !ConformityCertificateId.HasValue || ConformityCertificateId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this certificate identification is NOT null or empty.
        /// </summary>
        /// <param name="ConformityCertificateId">A certificate identification.</param>
        public static Boolean IsNotNullOrEmpty(this ConformityCertificate_Id? ConformityCertificateId)
            => ConformityCertificateId.HasValue && ConformityCertificateId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a certificate,
    /// e.g. a model or version approval document:
    /// compared case-insensitively, surrounding whitespace trimmed away.
    /// </summary>
    public readonly struct ConformityCertificate_Id : IId<ConformityCertificate_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this certificate identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this certificate identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the certificate identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new certificate identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a certificate identification.</param>
        private ConformityCertificate_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) New

        /// <summary>
        /// Create a new unique certificate identification (UUIDv7).
        /// </summary>
        public static ConformityCertificate_Id New

            => new (UUIDv7.Generate().ToString());

        #endregion

        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random certificate identification.
        /// </summary>
        /// <param name="Length">The expected length of the certificate identification.</param>
        public static ConformityCertificate_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a certificate identification.
        /// </summary>
        /// <param name="Text">A text representation of a certificate identification.</param>
        public static ConformityCertificate_Id Parse(String Text)
        {

            if (TryParse(Text, out var certificateId))
                return certificateId;

            throw new ArgumentException($"Invalid text representation of a certificate identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a certificate identification.
        /// </summary>
        /// <param name="Text">A text representation of a certificate identification.</param>
        public static ConformityCertificate_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var transparencySoftware))
                return transparencySoftware;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ConformityCertificateId)

        /// <summary>
        /// Try to parse the given text as a certificate identification.
        /// </summary>
        /// <param name="Text">A text representation of a certificate identification.</param>
        /// <param name="ConformityCertificateId">The parsed certificate identification.</param>
        public static Boolean TryParse(String Text, out ConformityCertificate_Id ConformityCertificateId)
        {

            Text = Text?.Trim() ?? "";

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ConformityCertificateId = new ConformityCertificate_Id(Text);
                    return true;
                }
                catch
                { }
            }

            ConformityCertificateId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this certificate identification.
        /// </summary>
        public ConformityCertificate_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ConformityCertificateId1, ConformityCertificateId2)

        /// <summary>
        /// Compares two certificate identifications for equality.
        /// </summary>
        /// <param name="ConformityCertificateId1">A certificate identification.</param>
        /// <param name="ConformityCertificateId2">Another certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ConformityCertificate_Id ConformityCertificateId1,
                                           ConformityCertificate_Id ConformityCertificateId2)

            => ConformityCertificateId1.Equals(ConformityCertificateId2);

        #endregion

        #region Operator != (ConformityCertificateId1, ConformityCertificateId2)

        /// <summary>
        /// Compares two certificate identifications for inequality.
        /// </summary>
        /// <param name="ConformityCertificateId1">A certificate identification.</param>
        /// <param name="ConformityCertificateId2">Another certificate identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ConformityCertificate_Id ConformityCertificateId1,
                                           ConformityCertificate_Id ConformityCertificateId2)

            => !ConformityCertificateId1.Equals(ConformityCertificateId2);

        #endregion

        #region Operator <  (ConformityCertificateId1, ConformityCertificateId2)

        /// <summary>
        /// Compares two certificate identifications.
        /// </summary>
        /// <param name="ConformityCertificateId1">A certificate identification.</param>
        /// <param name="ConformityCertificateId2">Another certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ConformityCertificate_Id ConformityCertificateId1,
                                          ConformityCertificate_Id ConformityCertificateId2)

            => ConformityCertificateId1.CompareTo(ConformityCertificateId2) < 0;

        #endregion

        #region Operator <= (ConformityCertificateId1, ConformityCertificateId2)

        /// <summary>
        /// Compares two certificate identifications.
        /// </summary>
        /// <param name="ConformityCertificateId1">A certificate identification.</param>
        /// <param name="ConformityCertificateId2">Another certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ConformityCertificate_Id ConformityCertificateId1,
                                           ConformityCertificate_Id ConformityCertificateId2)

            => ConformityCertificateId1.CompareTo(ConformityCertificateId2) <= 0;

        #endregion

        #region Operator >  (ConformityCertificateId1, ConformityCertificateId2)

        /// <summary>
        /// Compares two certificate identifications.
        /// </summary>
        /// <param name="ConformityCertificateId1">A certificate identification.</param>
        /// <param name="ConformityCertificateId2">Another certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ConformityCertificate_Id ConformityCertificateId1,
                                          ConformityCertificate_Id ConformityCertificateId2)

            => ConformityCertificateId1.CompareTo(ConformityCertificateId2) > 0;

        #endregion

        #region Operator >= (ConformityCertificateId1, ConformityCertificateId2)

        /// <summary>
        /// Compares two certificate identifications.
        /// </summary>
        /// <param name="ConformityCertificateId1">A certificate identification.</param>
        /// <param name="ConformityCertificateId2">Another certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ConformityCertificate_Id ConformityCertificateId1,
                                           ConformityCertificate_Id ConformityCertificateId2)

            => ConformityCertificateId1.CompareTo(ConformityCertificateId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ConformityCertificateId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two certificate identifications.
        /// </summary>
        /// <param name="Object">A certificate identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ConformityCertificate_Id certificateId
                   ? CompareTo(certificateId)
                   : throw new ArgumentException("The given object is not a certificate identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ConformityCertificateId)

        /// <summary>
        /// Compares two certificate identifications.
        /// </summary>
        /// <param name="ConformityCertificateId">A certificate identification to compare with.</param>
        public Int32 CompareTo(ConformityCertificate_Id ConformityCertificateId)

            => String.Compare(InternalId,
                              ConformityCertificateId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ConformityCertificateId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two certificate identifications for equality.
        /// </summary>
        /// <param name="Object">A certificate identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ConformityCertificate_Id certificateId &&
                   Equals(certificateId);

        #endregion

        #region Equals(ConformityCertificateId)

        /// <summary>
        /// Compares two certificate identifications for equality.
        /// </summary>
        /// <param name="ConformityCertificateId">A certificate identification to compare with.</param>
        public Boolean Equals(ConformityCertificate_Id ConformityCertificateId)

            => String.Equals(InternalId,
                             ConformityCertificateId.InternalId,
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
