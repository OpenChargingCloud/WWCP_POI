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
    /// Extension methods for conformity assessment body identifications.
    /// </summary>
    public static class ConformityAssessmentBodyIdExtensions
    {

        /// <summary>
        /// Indicates whether this conformity assessment body identification is null or empty.
        /// </summary>
        /// <param name="ConformityAssessmentBodyId">A conformity assessment body identification.</param>
        public static Boolean IsNullOrEmpty(this ConformityAssessmentBody_Id? ConformityAssessmentBodyId)
            => !ConformityAssessmentBodyId.HasValue || ConformityAssessmentBodyId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this conformity assessment body identification is NOT null or empty.
        /// </summary>
        /// <param name="ConformityAssessmentBodyId">A conformity assessment body identification.</param>
        public static Boolean IsNotNullOrEmpty(this ConformityAssessmentBody_Id? ConformityAssessmentBodyId)
            => ConformityAssessmentBodyId.HasValue && ConformityAssessmentBodyId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a conformity assessment body.
    /// </summary>
    public readonly struct ConformityAssessmentBody_Id : IId<ConformityAssessmentBody_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this conformity assessment body identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this conformity assessment body identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the conformity assessment body identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new conformity assessment body identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a conformity assessment body identification.</param>
        private ConformityAssessmentBody_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random conformity assessment body identification.
        /// </summary>
        /// <param name="Length">The expected length of the conformity assessment body identification.</param>
        public static ConformityAssessmentBody_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a conformity assessment body identification.
        /// </summary>
        /// <param name="Text">A text representation of a conformity assessment body identification.</param>
        public static ConformityAssessmentBody_Id Parse(String Text)
        {

            if (TryParse(Text, out var conformityAssessmentBodyId))
                return conformityAssessmentBodyId;

            throw new ArgumentException($"Invalid text representation of a conformity assessment body identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a conformity assessment body identification.
        /// </summary>
        /// <param name="Text">A text representation of a conformity assessment body identification.</param>
        public static ConformityAssessmentBody_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var conformityAssessmentBodyId))
                return conformityAssessmentBodyId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ConformityAssessmentBodyId)

        /// <summary>
        /// Try to parse the given text as a conformity assessment body identification.
        /// </summary>
        /// <param name="Text">A text representation of a conformity assessment body identification.</param>
        /// <param name="ConformityAssessmentBodyId">The parsed conformity assessment body identification.</param>
        public static Boolean TryParse(String Text, out ConformityAssessmentBody_Id ConformityAssessmentBodyId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ConformityAssessmentBodyId = new ConformityAssessmentBody_Id(Text);
                    return true;
                }
                catch
                { }
            }

            ConformityAssessmentBodyId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this conformity assessment body identification.
        /// </summary>
        public ConformityAssessmentBody_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ConformityAssessmentBodyId1, ConformityAssessmentBodyId2)

        /// <summary>
        /// Compares two conformity assessment body identifications for equality.
        /// </summary>
        /// <param name="ConformityAssessmentBodyId1">A conformity assessment body identification.</param>
        /// <param name="ConformityAssessmentBodyId2">Another conformity assessment body identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ConformityAssessmentBody_Id ConformityAssessmentBodyId1,
                                           ConformityAssessmentBody_Id ConformityAssessmentBodyId2)

            => ConformityAssessmentBodyId1.Equals(ConformityAssessmentBodyId2);

        #endregion

        #region Operator != (ConformityAssessmentBodyId1, ConformityAssessmentBodyId2)

        /// <summary>
        /// Compares two conformity assessment body identifications for inequality.
        /// </summary>
        /// <param name="ConformityAssessmentBodyId1">A conformity assessment body identification.</param>
        /// <param name="ConformityAssessmentBodyId2">Another conformity assessment body identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ConformityAssessmentBody_Id ConformityAssessmentBodyId1,
                                           ConformityAssessmentBody_Id ConformityAssessmentBodyId2)

            => !ConformityAssessmentBodyId1.Equals(ConformityAssessmentBodyId2);

        #endregion

        #region Operator <  (ConformityAssessmentBodyId1, ConformityAssessmentBodyId2)

        /// <summary>
        /// Compares two conformity assessment body identifications.
        /// </summary>
        /// <param name="ConformityAssessmentBodyId1">A conformity assessment body identification.</param>
        /// <param name="ConformityAssessmentBodyId2">Another conformity assessment body identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ConformityAssessmentBody_Id ConformityAssessmentBodyId1,
                                          ConformityAssessmentBody_Id ConformityAssessmentBodyId2)

            => ConformityAssessmentBodyId1.CompareTo(ConformityAssessmentBodyId2) < 0;

        #endregion

        #region Operator <= (ConformityAssessmentBodyId1, ConformityAssessmentBodyId2)

        /// <summary>
        /// Compares two conformity assessment body identifications.
        /// </summary>
        /// <param name="ConformityAssessmentBodyId1">A conformity assessment body identification.</param>
        /// <param name="ConformityAssessmentBodyId2">Another conformity assessment body identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ConformityAssessmentBody_Id ConformityAssessmentBodyId1,
                                           ConformityAssessmentBody_Id ConformityAssessmentBodyId2)

            => ConformityAssessmentBodyId1.CompareTo(ConformityAssessmentBodyId2) <= 0;

        #endregion

        #region Operator >  (ConformityAssessmentBodyId1, ConformityAssessmentBodyId2)

        /// <summary>
        /// Compares two conformity assessment body identifications.
        /// </summary>
        /// <param name="ConformityAssessmentBodyId1">A conformity assessment body identification.</param>
        /// <param name="ConformityAssessmentBodyId2">Another conformity assessment body identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ConformityAssessmentBody_Id ConformityAssessmentBodyId1,
                                          ConformityAssessmentBody_Id ConformityAssessmentBodyId2)

            => ConformityAssessmentBodyId1.CompareTo(ConformityAssessmentBodyId2) > 0;

        #endregion

        #region Operator >= (ConformityAssessmentBodyId1, ConformityAssessmentBodyId2)

        /// <summary>
        /// Compares two conformity assessment body identifications.
        /// </summary>
        /// <param name="ConformityAssessmentBodyId1">A conformity assessment body identification.</param>
        /// <param name="ConformityAssessmentBodyId2">Another conformity assessment body identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ConformityAssessmentBody_Id ConformityAssessmentBodyId1,
                                           ConformityAssessmentBody_Id ConformityAssessmentBodyId2)

            => ConformityAssessmentBodyId1.CompareTo(ConformityAssessmentBodyId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ConformityAssessmentBodyId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two conformity assessment body identifications.
        /// </summary>
        /// <param name="Object">A conformity assessment body identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ConformityAssessmentBody_Id conformityAssessmentBodyId
                   ? CompareTo(conformityAssessmentBodyId)
                   : throw new ArgumentException("The given object is not a conformity assessment body identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ConformityAssessmentBodyId)

        /// <summary>
        /// Compares two conformity assessment body identifications.
        /// </summary>
        /// <param name="ConformityAssessmentBodyId">A conformity assessment body identification to compare with.</param>
        public Int32 CompareTo(ConformityAssessmentBody_Id ConformityAssessmentBodyId)

            => String.Compare(InternalId,
                              ConformityAssessmentBodyId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ConformityAssessmentBodyId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two conformity assessment body identifications for equality.
        /// </summary>
        /// <param name="Object">A conformity assessment body identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ConformityAssessmentBody_Id conformityAssessmentBodyId &&
                   Equals(conformityAssessmentBodyId);

        #endregion

        #region Equals(ConformityAssessmentBodyId)

        /// <summary>
        /// Compares two conformity assessment body identifications for equality.
        /// </summary>
        /// <param name="ConformityAssessmentBodyId">A conformity assessment body identification to compare with.</param>
        public Boolean Equals(ConformityAssessmentBody_Id ConformityAssessmentBodyId)

            => String.Equals(InternalId,
                             ConformityAssessmentBodyId.InternalId,
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
