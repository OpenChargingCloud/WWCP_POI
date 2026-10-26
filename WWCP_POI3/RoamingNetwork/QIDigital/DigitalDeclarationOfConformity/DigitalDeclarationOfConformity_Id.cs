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
    /// Extension methods for digital declaration of conformity identifications.
    /// </summary>
    public static class DigitalDeclarationOfConformityIdExtensions
    {

        /// <summary>
        /// Indicates whether this digital declaration of conformity identification is null or empty.
        /// </summary>
        /// <param name="DigitalDeclarationOfConformityId">A digital declaration of conformity identification.</param>
        public static Boolean IsNullOrEmpty(this DigitalDeclarationOfConformity_Id? DigitalDeclarationOfConformityId)
            => !DigitalDeclarationOfConformityId.HasValue || DigitalDeclarationOfConformityId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this digital declaration of conformity identification is NOT null or empty.
        /// </summary>
        /// <param name="DigitalDeclarationOfConformityId">A digital declaration of conformity identification.</param>
        public static Boolean IsNotNullOrEmpty(this DigitalDeclarationOfConformity_Id? DigitalDeclarationOfConformityId)
            => DigitalDeclarationOfConformityId.HasValue && DigitalDeclarationOfConformityId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a digital declaration of conformity, assigned by its issuer.
    /// </summary>
    public readonly struct DigitalDeclarationOfConformity_Id : IId<DigitalDeclarationOfConformity_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this digital declaration of conformity identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this digital declaration of conformity identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the digital declaration of conformity identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new digital declaration of conformity identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a digital declaration of conformity identification.</param>
        private DigitalDeclarationOfConformity_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random digital declaration of conformity identification.
        /// </summary>
        /// <param name="Length">The expected length of the digital declaration of conformity identification.</param>
        public static DigitalDeclarationOfConformity_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a digital declaration of conformity identification.
        /// </summary>
        /// <param name="Text">A text representation of a digital declaration of conformity identification.</param>
        public static DigitalDeclarationOfConformity_Id Parse(String Text)
        {

            if (TryParse(Text, out var digitalDeclarationOfConformityId))
                return digitalDeclarationOfConformityId;

            throw new ArgumentException($"Invalid text representation of a digital declaration of conformity identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a digital declaration of conformity identification.
        /// </summary>
        /// <param name="Text">A text representation of a digital declaration of conformity identification.</param>
        public static DigitalDeclarationOfConformity_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var digitalDeclarationOfConformityId))
                return digitalDeclarationOfConformityId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out DigitalDeclarationOfConformityId)

        /// <summary>
        /// Try to parse the given text as a digital declaration of conformity identification.
        /// </summary>
        /// <param name="Text">A text representation of a digital declaration of conformity identification.</param>
        /// <param name="DigitalDeclarationOfConformityId">The parsed digital declaration of conformity identification.</param>
        public static Boolean TryParse(String Text, out DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    DigitalDeclarationOfConformityId = new DigitalDeclarationOfConformity_Id(Text);
                    return true;
                }
                catch
                { }
            }

            DigitalDeclarationOfConformityId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this digital declaration of conformity identification.
        /// </summary>
        public DigitalDeclarationOfConformity_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (DigitalDeclarationOfConformityId1, DigitalDeclarationOfConformityId2)

        /// <summary>
        /// Compares two digital declaration of conformity identifications for equality.
        /// </summary>
        /// <param name="DigitalDeclarationOfConformityId1">A digital declaration of conformity identification.</param>
        /// <param name="DigitalDeclarationOfConformityId2">Another digital declaration of conformity identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId1,
                                           DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId2)

            => DigitalDeclarationOfConformityId1.Equals(DigitalDeclarationOfConformityId2);

        #endregion

        #region Operator != (DigitalDeclarationOfConformityId1, DigitalDeclarationOfConformityId2)

        /// <summary>
        /// Compares two digital declaration of conformity identifications for inequality.
        /// </summary>
        /// <param name="DigitalDeclarationOfConformityId1">A digital declaration of conformity identification.</param>
        /// <param name="DigitalDeclarationOfConformityId2">Another digital declaration of conformity identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId1,
                                           DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId2)

            => !DigitalDeclarationOfConformityId1.Equals(DigitalDeclarationOfConformityId2);

        #endregion

        #region Operator <  (DigitalDeclarationOfConformityId1, DigitalDeclarationOfConformityId2)

        /// <summary>
        /// Compares two digital declaration of conformity identifications.
        /// </summary>
        /// <param name="DigitalDeclarationOfConformityId1">A digital declaration of conformity identification.</param>
        /// <param name="DigitalDeclarationOfConformityId2">Another digital declaration of conformity identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId1,
                                          DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId2)

            => DigitalDeclarationOfConformityId1.CompareTo(DigitalDeclarationOfConformityId2) < 0;

        #endregion

        #region Operator <= (DigitalDeclarationOfConformityId1, DigitalDeclarationOfConformityId2)

        /// <summary>
        /// Compares two digital declaration of conformity identifications.
        /// </summary>
        /// <param name="DigitalDeclarationOfConformityId1">A digital declaration of conformity identification.</param>
        /// <param name="DigitalDeclarationOfConformityId2">Another digital declaration of conformity identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId1,
                                           DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId2)

            => DigitalDeclarationOfConformityId1.CompareTo(DigitalDeclarationOfConformityId2) <= 0;

        #endregion

        #region Operator >  (DigitalDeclarationOfConformityId1, DigitalDeclarationOfConformityId2)

        /// <summary>
        /// Compares two digital declaration of conformity identifications.
        /// </summary>
        /// <param name="DigitalDeclarationOfConformityId1">A digital declaration of conformity identification.</param>
        /// <param name="DigitalDeclarationOfConformityId2">Another digital declaration of conformity identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId1,
                                          DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId2)

            => DigitalDeclarationOfConformityId1.CompareTo(DigitalDeclarationOfConformityId2) > 0;

        #endregion

        #region Operator >= (DigitalDeclarationOfConformityId1, DigitalDeclarationOfConformityId2)

        /// <summary>
        /// Compares two digital declaration of conformity identifications.
        /// </summary>
        /// <param name="DigitalDeclarationOfConformityId1">A digital declaration of conformity identification.</param>
        /// <param name="DigitalDeclarationOfConformityId2">Another digital declaration of conformity identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId1,
                                           DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId2)

            => DigitalDeclarationOfConformityId1.CompareTo(DigitalDeclarationOfConformityId2) >= 0;

        #endregion

        #endregion

        #region IComparable<DigitalDeclarationOfConformityId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two digital declaration of conformity identifications.
        /// </summary>
        /// <param name="Object">A digital declaration of conformity identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is DigitalDeclarationOfConformity_Id digitalDeclarationOfConformityId
                   ? CompareTo(digitalDeclarationOfConformityId)
                   : throw new ArgumentException("The given object is not a digital declaration of conformity identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(DigitalDeclarationOfConformityId)

        /// <summary>
        /// Compares two digital declaration of conformity identifications.
        /// </summary>
        /// <param name="DigitalDeclarationOfConformityId">A digital declaration of conformity identification to compare with.</param>
        public Int32 CompareTo(DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId)

            => String.Compare(InternalId,
                              DigitalDeclarationOfConformityId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<DigitalDeclarationOfConformityId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two digital declaration of conformity identifications for equality.
        /// </summary>
        /// <param name="Object">A digital declaration of conformity identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is DigitalDeclarationOfConformity_Id digitalDeclarationOfConformityId &&
                   Equals(digitalDeclarationOfConformityId);

        #endregion

        #region Equals(DigitalDeclarationOfConformityId)

        /// <summary>
        /// Compares two digital declaration of conformity identifications for equality.
        /// </summary>
        /// <param name="DigitalDeclarationOfConformityId">A digital declaration of conformity identification to compare with.</param>
        public Boolean Equals(DigitalDeclarationOfConformity_Id DigitalDeclarationOfConformityId)

            => String.Equals(InternalId,
                             DigitalDeclarationOfConformityId.InternalId,
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
