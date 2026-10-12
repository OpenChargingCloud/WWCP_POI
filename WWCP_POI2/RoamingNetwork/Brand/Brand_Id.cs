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
    /// Extension methods for brand identifications.
    /// </summary>
    public static class BrandIdExtensions
    {

        /// <summary>
        /// Indicates whether this brand identification is null or empty.
        /// </summary>
        /// <param name="BrandId">An brand identification.</param>
        public static Boolean IsNullOrEmpty(this Brand_Id? BrandId)
            => !BrandId.HasValue || BrandId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this brand identification is null or empty.
        /// </summary>
        /// <param name="BrandId">An brand identification.</param>
        public static Boolean IsNotNullOrEmpty(this Brand_Id? BrandId)
            => BrandId.HasValue && BrandId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of an brand.
    /// </summary>
    public readonly struct Brand_Id : IId<Brand_Id>
    {

        #region Data

        /// <summary>
        /// The internal brand identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this brand identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this brand identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the brand identifier.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new brand identification based on the given string.
        /// </summary>
        private Brand_Id(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) NewRandom  (Length = 15)

        /// <summary>
        /// Create a new random brand identification.
        /// </summary>
        /// <param name="Length">The expected length of the random brand identification.</param>
        public static Brand_Id NewRandom(Byte Length = 15)

            => new (RandomExtensions.RandomString(Length).ToUpper());

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as an brand identification.
        /// </summary>
        /// <param name="Text">A text representation of an brand identification.</param>
        public static Brand_Id Parse(String Text)
        {

            if (TryParse(Text, out Brand_Id brandId))
                return brandId;

            throw new ArgumentException($"Invalid text representation of an brand identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as an brand identification.
        /// </summary>
        /// <param name="Text">A text representation of an brand identification.</param>
        public static Brand_Id? TryParse(String? Text)
        {

            if (Text is not null && TryParse(Text, out Brand_Id brandId))
                return brandId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out BrandId)

        /// <summary>
        /// Try to parse the given string as an brand identification.
        /// </summary>
        /// <param name="Text">A text representation of an brand identification.</param>
        /// <param name="BrandId">The parsed brand identification.</param>
        public static Boolean TryParse(String Text, out Brand_Id BrandId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    BrandId = new Brand_Id(Text);
                    return true;
                }
                catch
                { }
            }

            BrandId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this brand identification.
        /// </summary>
        public Brand_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (BrandId1, BrandId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="BrandId1">An brand identification.</param>
        /// <param name="BrandId2">Another brand identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (Brand_Id BrandId1,
                                           Brand_Id BrandId2)

            => BrandId1.Equals(BrandId2);

        #endregion

        #region Operator != (BrandId1, BrandId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="BrandId1">An brand identification.</param>
        /// <param name="BrandId2">Another brand identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (Brand_Id BrandId1,
                                           Brand_Id BrandId2)

            => !BrandId1.Equals(BrandId2);

        #endregion

        #region Operator <  (BrandId1, BrandId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="BrandId1">An brand identification.</param>
        /// <param name="BrandId2">Another brand identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (Brand_Id BrandId1,
                                          Brand_Id BrandId2)

            => BrandId1.CompareTo(BrandId2) < 0;

        #endregion

        #region Operator <= (BrandId1, BrandId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="BrandId1">An brand identification.</param>
        /// <param name="BrandId2">Another brand identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (Brand_Id BrandId1,
                                           Brand_Id BrandId2)

            => BrandId1.CompareTo(BrandId2) <= 0;

        #endregion

        #region Operator >  (BrandId1, BrandId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="BrandId1">An brand identification.</param>
        /// <param name="BrandId2">Another brand identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (Brand_Id BrandId1,
                                          Brand_Id BrandId2)

            => BrandId1.CompareTo(BrandId2) > 0;

        #endregion

        #region Operator >= (BrandId1, BrandId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="BrandId1">An brand identification.</param>
        /// <param name="BrandId2">Another brand identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (Brand_Id BrandId1,
                                           Brand_Id BrandId2)

            => BrandId1.CompareTo(BrandId2) >= 0;

        #endregion

        #endregion

        #region IComparable<Brand_Id> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Brand_Id brandId
                   ? CompareTo(brandId)
                   : throw new ArgumentException("The given object is not an brand identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(BrandId)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="BrandId">An object to compare with.</param>
        public Int32 CompareTo(Brand_Id BrandId)

            => String.Compare(InternalId,
                              BrandId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<Brand_Id> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public override Boolean Equals(Object? Object)

            => Object is Brand_Id brandId &&
                   Equals(brandId);

        #endregion

        #region Equals(BrandId)

        /// <summary>
        /// Compares two brand identifications for equality.
        /// </summary>
        /// <param name="BrandId">An brand identification to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(Brand_Id BrandId)

            => String.Equals(InternalId,
                             BrandId.InternalId,
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
