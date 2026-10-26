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
    /// Extension methods for digital certificate of conformity identifications.
    /// </summary>
    public static class DigitalCertificateOfConformityIdExtensions
    {

        /// <summary>
        /// Indicates whether this digital certificate of conformity identification is null or empty.
        /// </summary>
        /// <param name="DigitalCertificateOfConformityId">A digital certificate of conformity identification.</param>
        public static Boolean IsNullOrEmpty(this DigitalCertificateOfConformity_Id? DigitalCertificateOfConformityId)
            => !DigitalCertificateOfConformityId.HasValue || DigitalCertificateOfConformityId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this digital certificate of conformity identification is NOT null or empty.
        /// </summary>
        /// <param name="DigitalCertificateOfConformityId">A digital certificate of conformity identification.</param>
        public static Boolean IsNotNullOrEmpty(this DigitalCertificateOfConformity_Id? DigitalCertificateOfConformityId)
            => DigitalCertificateOfConformityId.HasValue && DigitalCertificateOfConformityId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a digital certificate of conformity, assigned by its issuer.
    /// </summary>
    public readonly struct DigitalCertificateOfConformity_Id : IId<DigitalCertificateOfConformity_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this digital certificate of conformity identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this digital certificate of conformity identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the digital certificate of conformity identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new digital certificate of conformity identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a digital certificate of conformity identification.</param>
        private DigitalCertificateOfConformity_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random digital certificate of conformity identification.
        /// </summary>
        /// <param name="Length">The expected length of the digital certificate of conformity identification.</param>
        public static DigitalCertificateOfConformity_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a digital certificate of conformity identification.
        /// </summary>
        /// <param name="Text">A text representation of a digital certificate of conformity identification.</param>
        public static DigitalCertificateOfConformity_Id Parse(String Text)
        {

            if (TryParse(Text, out var digitalCertificateOfConformityId))
                return digitalCertificateOfConformityId;

            throw new ArgumentException($"Invalid text representation of a digital certificate of conformity identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a digital certificate of conformity identification.
        /// </summary>
        /// <param name="Text">A text representation of a digital certificate of conformity identification.</param>
        public static DigitalCertificateOfConformity_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var digitalCertificateOfConformityId))
                return digitalCertificateOfConformityId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out DigitalCertificateOfConformityId)

        /// <summary>
        /// Try to parse the given text as a digital certificate of conformity identification.
        /// </summary>
        /// <param name="Text">A text representation of a digital certificate of conformity identification.</param>
        /// <param name="DigitalCertificateOfConformityId">The parsed digital certificate of conformity identification.</param>
        public static Boolean TryParse(String Text, out DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    DigitalCertificateOfConformityId = new DigitalCertificateOfConformity_Id(Text);
                    return true;
                }
                catch
                { }
            }

            DigitalCertificateOfConformityId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this digital certificate of conformity identification.
        /// </summary>
        public DigitalCertificateOfConformity_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (DigitalCertificateOfConformityId1, DigitalCertificateOfConformityId2)

        /// <summary>
        /// Compares two digital certificate of conformity identifications for equality.
        /// </summary>
        /// <param name="DigitalCertificateOfConformityId1">A digital certificate of conformity identification.</param>
        /// <param name="DigitalCertificateOfConformityId2">Another digital certificate of conformity identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId1,
                                           DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId2)

            => DigitalCertificateOfConformityId1.Equals(DigitalCertificateOfConformityId2);

        #endregion

        #region Operator != (DigitalCertificateOfConformityId1, DigitalCertificateOfConformityId2)

        /// <summary>
        /// Compares two digital certificate of conformity identifications for inequality.
        /// </summary>
        /// <param name="DigitalCertificateOfConformityId1">A digital certificate of conformity identification.</param>
        /// <param name="DigitalCertificateOfConformityId2">Another digital certificate of conformity identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId1,
                                           DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId2)

            => !DigitalCertificateOfConformityId1.Equals(DigitalCertificateOfConformityId2);

        #endregion

        #region Operator <  (DigitalCertificateOfConformityId1, DigitalCertificateOfConformityId2)

        /// <summary>
        /// Compares two digital certificate of conformity identifications.
        /// </summary>
        /// <param name="DigitalCertificateOfConformityId1">A digital certificate of conformity identification.</param>
        /// <param name="DigitalCertificateOfConformityId2">Another digital certificate of conformity identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId1,
                                          DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId2)

            => DigitalCertificateOfConformityId1.CompareTo(DigitalCertificateOfConformityId2) < 0;

        #endregion

        #region Operator <= (DigitalCertificateOfConformityId1, DigitalCertificateOfConformityId2)

        /// <summary>
        /// Compares two digital certificate of conformity identifications.
        /// </summary>
        /// <param name="DigitalCertificateOfConformityId1">A digital certificate of conformity identification.</param>
        /// <param name="DigitalCertificateOfConformityId2">Another digital certificate of conformity identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId1,
                                           DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId2)

            => DigitalCertificateOfConformityId1.CompareTo(DigitalCertificateOfConformityId2) <= 0;

        #endregion

        #region Operator >  (DigitalCertificateOfConformityId1, DigitalCertificateOfConformityId2)

        /// <summary>
        /// Compares two digital certificate of conformity identifications.
        /// </summary>
        /// <param name="DigitalCertificateOfConformityId1">A digital certificate of conformity identification.</param>
        /// <param name="DigitalCertificateOfConformityId2">Another digital certificate of conformity identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId1,
                                          DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId2)

            => DigitalCertificateOfConformityId1.CompareTo(DigitalCertificateOfConformityId2) > 0;

        #endregion

        #region Operator >= (DigitalCertificateOfConformityId1, DigitalCertificateOfConformityId2)

        /// <summary>
        /// Compares two digital certificate of conformity identifications.
        /// </summary>
        /// <param name="DigitalCertificateOfConformityId1">A digital certificate of conformity identification.</param>
        /// <param name="DigitalCertificateOfConformityId2">Another digital certificate of conformity identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId1,
                                           DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId2)

            => DigitalCertificateOfConformityId1.CompareTo(DigitalCertificateOfConformityId2) >= 0;

        #endregion

        #endregion

        #region IComparable<DigitalCertificateOfConformityId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two digital certificate of conformity identifications.
        /// </summary>
        /// <param name="Object">A digital certificate of conformity identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is DigitalCertificateOfConformity_Id digitalCertificateOfConformityId
                   ? CompareTo(digitalCertificateOfConformityId)
                   : throw new ArgumentException("The given object is not a digital certificate of conformity identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(DigitalCertificateOfConformityId)

        /// <summary>
        /// Compares two digital certificate of conformity identifications.
        /// </summary>
        /// <param name="DigitalCertificateOfConformityId">A digital certificate of conformity identification to compare with.</param>
        public Int32 CompareTo(DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId)

            => String.Compare(InternalId,
                              DigitalCertificateOfConformityId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<DigitalCertificateOfConformityId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two digital certificate of conformity identifications for equality.
        /// </summary>
        /// <param name="Object">A digital certificate of conformity identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is DigitalCertificateOfConformity_Id digitalCertificateOfConformityId &&
                   Equals(digitalCertificateOfConformityId);

        #endregion

        #region Equals(DigitalCertificateOfConformityId)

        /// <summary>
        /// Compares two digital certificate of conformity identifications for equality.
        /// </summary>
        /// <param name="DigitalCertificateOfConformityId">A digital certificate of conformity identification to compare with.</param>
        public Boolean Equals(DigitalCertificateOfConformity_Id DigitalCertificateOfConformityId)

            => String.Equals(InternalId,
                             DigitalCertificateOfConformityId.InternalId,
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
