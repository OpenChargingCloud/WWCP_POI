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
    /// Extension methods for transparency software certificate identifications.
    /// </summary>
    public static class TransparencySoftwareCertificateIdExtensions
    {

        /// <summary>
        /// Indicates whether this transparency software certificate identification is null or empty.
        /// </summary>
        /// <param name="TransparencySoftwareCertificateId">A transparency software certificate identification.</param>
        public static Boolean IsNullOrEmpty(this TransparencySoftwareCertificate_Id? TransparencySoftwareCertificateId)
            => !TransparencySoftwareCertificateId.HasValue || TransparencySoftwareCertificateId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this transparency software certificate identification is NOT null or empty.
        /// </summary>
        /// <param name="TransparencySoftwareCertificateId">A transparency software certificate identification.</param>
        public static Boolean IsNotNullOrEmpty(this TransparencySoftwareCertificate_Id? TransparencySoftwareCertificateId)
            => TransparencySoftwareCertificateId.HasValue && TransparencySoftwareCertificateId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a transparency software certificate,
    /// e.g. a model or version approval document:
    /// compared case-insensitively, surrounding whitespace trimmed away.
    /// </summary>
    public readonly struct TransparencySoftwareCertificate_Id : IId<TransparencySoftwareCertificate_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this transparency software certificate identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this transparency software certificate identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the transparency software certificate identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new transparency software certificate identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a transparency software certificate identification.</param>
        private TransparencySoftwareCertificate_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) New

        /// <summary>
        /// Create a new unique transparency software certificate identification (UUIDv7).
        /// </summary>
        public static TransparencySoftwareCertificate_Id New

            => new (UUIDv7.Generate().ToString());

        #endregion

        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random transparency software certificate identification.
        /// </summary>
        /// <param name="Length">The expected length of the transparency software certificate identification.</param>
        public static TransparencySoftwareCertificate_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a transparency software certificate identification.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software certificate identification.</param>
        public static TransparencySoftwareCertificate_Id Parse(String Text)
        {

            if (TryParse(Text, out var transparencySoftwareCertificateId))
                return transparencySoftwareCertificateId;

            throw new ArgumentException($"Invalid text representation of a transparency software certificate identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a transparency software certificate identification.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software certificate identification.</param>
        public static TransparencySoftwareCertificate_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var transparencySoftware))
                return transparencySoftware;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out TransparencySoftwareCertificateId)

        /// <summary>
        /// Try to parse the given text as a transparency software certificate identification.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software certificate identification.</param>
        /// <param name="TransparencySoftwareCertificateId">The parsed transparency software certificate identification.</param>
        public static Boolean TryParse(String Text, out TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId)
        {

            Text = Text?.Trim() ?? "";

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    TransparencySoftwareCertificateId = new TransparencySoftwareCertificate_Id(Text);
                    return true;
                }
                catch
                { }
            }

            TransparencySoftwareCertificateId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this transparency software certificate identification.
        /// </summary>
        public TransparencySoftwareCertificate_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (TransparencySoftwareCertificateId1, TransparencySoftwareCertificateId2)

        /// <summary>
        /// Compares two transparency software certificate identifications for equality.
        /// </summary>
        /// <param name="TransparencySoftwareCertificateId1">A transparency software certificate identification.</param>
        /// <param name="TransparencySoftwareCertificateId2">Another transparency software certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId1,
                                           TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId2)

            => TransparencySoftwareCertificateId1.Equals(TransparencySoftwareCertificateId2);

        #endregion

        #region Operator != (TransparencySoftwareCertificateId1, TransparencySoftwareCertificateId2)

        /// <summary>
        /// Compares two transparency software certificate identifications for inequality.
        /// </summary>
        /// <param name="TransparencySoftwareCertificateId1">A transparency software certificate identification.</param>
        /// <param name="TransparencySoftwareCertificateId2">Another transparency software certificate identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId1,
                                           TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId2)

            => !TransparencySoftwareCertificateId1.Equals(TransparencySoftwareCertificateId2);

        #endregion

        #region Operator <  (TransparencySoftwareCertificateId1, TransparencySoftwareCertificateId2)

        /// <summary>
        /// Compares two transparency software certificate identifications.
        /// </summary>
        /// <param name="TransparencySoftwareCertificateId1">A transparency software certificate identification.</param>
        /// <param name="TransparencySoftwareCertificateId2">Another transparency software certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId1,
                                          TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId2)

            => TransparencySoftwareCertificateId1.CompareTo(TransparencySoftwareCertificateId2) < 0;

        #endregion

        #region Operator <= (TransparencySoftwareCertificateId1, TransparencySoftwareCertificateId2)

        /// <summary>
        /// Compares two transparency software certificate identifications.
        /// </summary>
        /// <param name="TransparencySoftwareCertificateId1">A transparency software certificate identification.</param>
        /// <param name="TransparencySoftwareCertificateId2">Another transparency software certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId1,
                                           TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId2)

            => TransparencySoftwareCertificateId1.CompareTo(TransparencySoftwareCertificateId2) <= 0;

        #endregion

        #region Operator >  (TransparencySoftwareCertificateId1, TransparencySoftwareCertificateId2)

        /// <summary>
        /// Compares two transparency software certificate identifications.
        /// </summary>
        /// <param name="TransparencySoftwareCertificateId1">A transparency software certificate identification.</param>
        /// <param name="TransparencySoftwareCertificateId2">Another transparency software certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId1,
                                          TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId2)

            => TransparencySoftwareCertificateId1.CompareTo(TransparencySoftwareCertificateId2) > 0;

        #endregion

        #region Operator >= (TransparencySoftwareCertificateId1, TransparencySoftwareCertificateId2)

        /// <summary>
        /// Compares two transparency software certificate identifications.
        /// </summary>
        /// <param name="TransparencySoftwareCertificateId1">A transparency software certificate identification.</param>
        /// <param name="TransparencySoftwareCertificateId2">Another transparency software certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId1,
                                           TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId2)

            => TransparencySoftwareCertificateId1.CompareTo(TransparencySoftwareCertificateId2) >= 0;

        #endregion

        #endregion

        #region IComparable<TransparencySoftwareCertificateId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two transparency software certificate identifications.
        /// </summary>
        /// <param name="Object">A transparency software certificate identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is TransparencySoftwareCertificate_Id transparencySoftwareCertificateId
                   ? CompareTo(transparencySoftwareCertificateId)
                   : throw new ArgumentException("The given object is not a transparency software certificate identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(TransparencySoftwareCertificateId)

        /// <summary>
        /// Compares two transparency software certificate identifications.
        /// </summary>
        /// <param name="TransparencySoftwareCertificateId">A transparency software certificate identification to compare with.</param>
        public Int32 CompareTo(TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId)

            => String.Compare(InternalId,
                              TransparencySoftwareCertificateId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<TransparencySoftwareCertificateId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two transparency software certificate identifications for equality.
        /// </summary>
        /// <param name="Object">A transparency software certificate identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is TransparencySoftwareCertificate_Id transparencySoftwareCertificateId &&
                   Equals(transparencySoftwareCertificateId);

        #endregion

        #region Equals(TransparencySoftwareCertificateId)

        /// <summary>
        /// Compares two transparency software certificate identifications for equality.
        /// </summary>
        /// <param name="TransparencySoftwareCertificateId">A transparency software certificate identification to compare with.</param>
        public Boolean Equals(TransparencySoftwareCertificate_Id TransparencySoftwareCertificateId)

            => String.Equals(InternalId,
                             TransparencySoftwareCertificateId.InternalId,
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
