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
    /// Extension methods for transparency software identifications.
    /// </summary>
    public static class TransparencySoftwareIdExtensions
    {

        /// <summary>
        /// Indicates whether this transparency software identification is null or empty.
        /// </summary>
        /// <param name="TransparencySoftwareId">A transparency software identification.</param>
        public static Boolean IsNullOrEmpty(this TransparencySoftware_Id? TransparencySoftwareId)
            => !TransparencySoftwareId.HasValue || TransparencySoftwareId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this transparency software identification is NOT null or empty.
        /// </summary>
        /// <param name="TransparencySoftwareId">A transparency software identification.</param>
        public static Boolean IsNotNullOrEmpty(this TransparencySoftware_Id? TransparencySoftwareId)
            => TransparencySoftwareId.HasValue && TransparencySoftwareId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a transparency software release:
    /// opaque and case-sensitive, without surrounding whitespace.
    /// </summary>
    public readonly struct TransparencySoftware_Id : IId<TransparencySoftware_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this transparency software identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this transparency software identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the transparency software identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new transparency software identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a transparency software identification.</param>
        private TransparencySoftware_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random transparency software identification.
        /// </summary>
        /// <param name="Length">The expected length of the transparency software identification.</param>
        public static TransparencySoftware_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a transparency software identification.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software identification.</param>
        public static TransparencySoftware_Id Parse(String Text)
        {

            if (TryParse(Text, out var transparencySoftwareId))
                return transparencySoftwareId;

            throw new ArgumentException($"Invalid text representation of a transparency software identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a transparency software identification.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software identification.</param>
        public static TransparencySoftware_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var transparencySoftware))
                return transparencySoftware;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out TransparencySoftwareId)

        /// <summary>
        /// Try to parse the given text as a transparency software identification.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software identification.</param>
        /// <param name="TransparencySoftwareId">The parsed transparency software identification.</param>
        public static Boolean TryParse(String Text, out TransparencySoftware_Id TransparencySoftwareId)
        {

            // Opaque and case-sensitive: surrounding whitespace is an error, not trimmed away.
            if (!String.IsNullOrWhiteSpace(Text) && Text == Text.Trim())
            {
                try
                {
                    TransparencySoftwareId = new TransparencySoftware_Id(Text);
                    return true;
                }
                catch
                { }
            }

            TransparencySoftwareId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this transparency software identification.
        /// </summary>
        public TransparencySoftware_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (TransparencySoftwareId1, TransparencySoftwareId2)

        /// <summary>
        /// Compares two transparency software identifications for equality.
        /// </summary>
        /// <param name="TransparencySoftwareId1">A transparency software identification.</param>
        /// <param name="TransparencySoftwareId2">Another transparency software identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (TransparencySoftware_Id TransparencySoftwareId1,
                                           TransparencySoftware_Id TransparencySoftwareId2)

            => TransparencySoftwareId1.Equals(TransparencySoftwareId2);

        #endregion

        #region Operator != (TransparencySoftwareId1, TransparencySoftwareId2)

        /// <summary>
        /// Compares two transparency software identifications for inequality.
        /// </summary>
        /// <param name="TransparencySoftwareId1">A transparency software identification.</param>
        /// <param name="TransparencySoftwareId2">Another transparency software identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (TransparencySoftware_Id TransparencySoftwareId1,
                                           TransparencySoftware_Id TransparencySoftwareId2)

            => !TransparencySoftwareId1.Equals(TransparencySoftwareId2);

        #endregion

        #region Operator <  (TransparencySoftwareId1, TransparencySoftwareId2)

        /// <summary>
        /// Compares two transparency software identifications.
        /// </summary>
        /// <param name="TransparencySoftwareId1">A transparency software identification.</param>
        /// <param name="TransparencySoftwareId2">Another transparency software identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (TransparencySoftware_Id TransparencySoftwareId1,
                                          TransparencySoftware_Id TransparencySoftwareId2)

            => TransparencySoftwareId1.CompareTo(TransparencySoftwareId2) < 0;

        #endregion

        #region Operator <= (TransparencySoftwareId1, TransparencySoftwareId2)

        /// <summary>
        /// Compares two transparency software identifications.
        /// </summary>
        /// <param name="TransparencySoftwareId1">A transparency software identification.</param>
        /// <param name="TransparencySoftwareId2">Another transparency software identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (TransparencySoftware_Id TransparencySoftwareId1,
                                           TransparencySoftware_Id TransparencySoftwareId2)

            => TransparencySoftwareId1.CompareTo(TransparencySoftwareId2) <= 0;

        #endregion

        #region Operator >  (TransparencySoftwareId1, TransparencySoftwareId2)

        /// <summary>
        /// Compares two transparency software identifications.
        /// </summary>
        /// <param name="TransparencySoftwareId1">A transparency software identification.</param>
        /// <param name="TransparencySoftwareId2">Another transparency software identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (TransparencySoftware_Id TransparencySoftwareId1,
                                          TransparencySoftware_Id TransparencySoftwareId2)

            => TransparencySoftwareId1.CompareTo(TransparencySoftwareId2) > 0;

        #endregion

        #region Operator >= (TransparencySoftwareId1, TransparencySoftwareId2)

        /// <summary>
        /// Compares two transparency software identifications.
        /// </summary>
        /// <param name="TransparencySoftwareId1">A transparency software identification.</param>
        /// <param name="TransparencySoftwareId2">Another transparency software identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (TransparencySoftware_Id TransparencySoftwareId1,
                                           TransparencySoftware_Id TransparencySoftwareId2)

            => TransparencySoftwareId1.CompareTo(TransparencySoftwareId2) >= 0;

        #endregion

        #endregion

        #region IComparable<TransparencySoftwareId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two transparency software identifications.
        /// </summary>
        /// <param name="Object">A transparency software identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is TransparencySoftware_Id transparencySoftwareId
                   ? CompareTo(transparencySoftwareId)
                   : throw new ArgumentException("The given object is not a transparency software identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(TransparencySoftwareId)

        /// <summary>
        /// Compares two transparency software identifications.
        /// </summary>
        /// <param name="TransparencySoftwareId">A transparency software identification to compare with.</param>
        public Int32 CompareTo(TransparencySoftware_Id TransparencySoftwareId)

            => String.Compare(InternalId,
                              TransparencySoftwareId.InternalId,
                              StringComparison.Ordinal);

        #endregion

        #endregion

        #region IEquatable<TransparencySoftwareId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two transparency software identifications for equality.
        /// </summary>
        /// <param name="Object">A transparency software identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is TransparencySoftware_Id transparencySoftwareId &&
                   Equals(transparencySoftwareId);

        #endregion

        #region Equals(TransparencySoftwareId)

        /// <summary>
        /// Compares two transparency software identifications for equality.
        /// </summary>
        /// <param name="TransparencySoftwareId">A transparency software identification to compare with.</param>
        public Boolean Equals(TransparencySoftware_Id TransparencySoftwareId)

            => String.Equals(InternalId,
                             TransparencySoftwareId.InternalId,
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
