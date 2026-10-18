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
    /// Extension methods for parking space template identifications.
    /// </summary>
    public static class ParkingSpaceTemplateIdExtensions
    {

        /// <summary>
        /// Indicates whether this parking space template identification is null or empty.
        /// </summary>
        /// <param name="ParkingSpaceTemplateId">A parking space template identification.</param>
        public static Boolean IsNullOrEmpty(this ParkingSpaceTemplate_Id? ParkingSpaceTemplateId)
            => !ParkingSpaceTemplateId.HasValue || ParkingSpaceTemplateId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking space template identification is NOT null or empty.
        /// </summary>
        /// <param name="ParkingSpaceTemplateId">A parking space template identification.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingSpaceTemplate_Id? ParkingSpaceTemplateId)
            => ParkingSpaceTemplateId.HasValue && ParkingSpaceTemplateId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a parking space template.
    /// </summary>
    public readonly struct ParkingSpaceTemplate_Id : IId<ParkingSpaceTemplate_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this parking space template identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this parking space template identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the parking space template identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking space template identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a parking space template identification.</param>
        private ParkingSpaceTemplate_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) New

        /// <summary>
        /// Create a new unique parking space template identification (UUIDv7).
        /// </summary>
        public static ParkingSpaceTemplate_Id New

            => new (UUIDv7.Generate().ToString());

        #endregion

        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random parking space template identification.
        /// </summary>
        /// <param name="Length">The expected length of the parking space template identification.</param>
        public static ParkingSpaceTemplate_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a parking space template identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking space template identification.</param>
        public static ParkingSpaceTemplate_Id Parse(String Text)
        {

            if (TryParse(Text, out var parkingSpaceTemplateId))
                return parkingSpaceTemplateId;

            throw new ArgumentException($"Invalid text representation of a parking space template identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a parking space template identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking space template identification.</param>
        public static ParkingSpaceTemplate_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var parkingSpaceTemplateId))
                return parkingSpaceTemplateId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingSpaceTemplateId)

        /// <summary>
        /// Try to parse the given text as a parking space template identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking space template identification.</param>
        /// <param name="ParkingSpaceTemplateId">The parsed parking space template identification.</param>
        public static Boolean TryParse(String Text, out ParkingSpaceTemplate_Id ParkingSpaceTemplateId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingSpaceTemplateId = new ParkingSpaceTemplate_Id(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingSpaceTemplateId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking space template identification.
        /// </summary>
        public ParkingSpaceTemplate_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ParkingSpaceTemplateId1, ParkingSpaceTemplateId2)

        /// <summary>
        /// Compares two parking space template identifications for equality.
        /// </summary>
        /// <param name="ParkingSpaceTemplateId1">A parking space template identification.</param>
        /// <param name="ParkingSpaceTemplateId2">Another parking space template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingSpaceTemplate_Id ParkingSpaceTemplateId1,
                                           ParkingSpaceTemplate_Id ParkingSpaceTemplateId2)

            => ParkingSpaceTemplateId1.Equals(ParkingSpaceTemplateId2);

        #endregion

        #region Operator != (ParkingSpaceTemplateId1, ParkingSpaceTemplateId2)

        /// <summary>
        /// Compares two parking space template identifications for inequality.
        /// </summary>
        /// <param name="ParkingSpaceTemplateId1">A parking space template identification.</param>
        /// <param name="ParkingSpaceTemplateId2">Another parking space template identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingSpaceTemplate_Id ParkingSpaceTemplateId1,
                                           ParkingSpaceTemplate_Id ParkingSpaceTemplateId2)

            => !ParkingSpaceTemplateId1.Equals(ParkingSpaceTemplateId2);

        #endregion

        #region Operator <  (ParkingSpaceTemplateId1, ParkingSpaceTemplateId2)

        /// <summary>
        /// Compares two parking space template identifications.
        /// </summary>
        /// <param name="ParkingSpaceTemplateId1">A parking space template identification.</param>
        /// <param name="ParkingSpaceTemplateId2">Another parking space template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingSpaceTemplate_Id ParkingSpaceTemplateId1,
                                          ParkingSpaceTemplate_Id ParkingSpaceTemplateId2)

            => ParkingSpaceTemplateId1.CompareTo(ParkingSpaceTemplateId2) < 0;

        #endregion

        #region Operator <= (ParkingSpaceTemplateId1, ParkingSpaceTemplateId2)

        /// <summary>
        /// Compares two parking space template identifications.
        /// </summary>
        /// <param name="ParkingSpaceTemplateId1">A parking space template identification.</param>
        /// <param name="ParkingSpaceTemplateId2">Another parking space template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingSpaceTemplate_Id ParkingSpaceTemplateId1,
                                           ParkingSpaceTemplate_Id ParkingSpaceTemplateId2)

            => ParkingSpaceTemplateId1.CompareTo(ParkingSpaceTemplateId2) <= 0;

        #endregion

        #region Operator >  (ParkingSpaceTemplateId1, ParkingSpaceTemplateId2)

        /// <summary>
        /// Compares two parking space template identifications.
        /// </summary>
        /// <param name="ParkingSpaceTemplateId1">A parking space template identification.</param>
        /// <param name="ParkingSpaceTemplateId2">Another parking space template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingSpaceTemplate_Id ParkingSpaceTemplateId1,
                                          ParkingSpaceTemplate_Id ParkingSpaceTemplateId2)

            => ParkingSpaceTemplateId1.CompareTo(ParkingSpaceTemplateId2) > 0;

        #endregion

        #region Operator >= (ParkingSpaceTemplateId1, ParkingSpaceTemplateId2)

        /// <summary>
        /// Compares two parking space template identifications.
        /// </summary>
        /// <param name="ParkingSpaceTemplateId1">A parking space template identification.</param>
        /// <param name="ParkingSpaceTemplateId2">Another parking space template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingSpaceTemplate_Id ParkingSpaceTemplateId1,
                                           ParkingSpaceTemplate_Id ParkingSpaceTemplateId2)

            => ParkingSpaceTemplateId1.CompareTo(ParkingSpaceTemplateId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingSpaceTemplateId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking space template identifications.
        /// </summary>
        /// <param name="Object">A parking space template identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingSpaceTemplate_Id parkingSpaceTemplateId
                   ? CompareTo(parkingSpaceTemplateId)
                   : throw new ArgumentException("The given object is not a parking space template identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingSpaceTemplateId)

        /// <summary>
        /// Compares two parking space template identifications.
        /// </summary>
        /// <param name="ParkingSpaceTemplateId">A parking space template identification to compare with.</param>
        public Int32 CompareTo(ParkingSpaceTemplate_Id ParkingSpaceTemplateId)

            => String.Compare(InternalId,
                              ParkingSpaceTemplateId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingSpaceTemplateId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking space template identifications for equality.
        /// </summary>
        /// <param name="Object">A parking space template identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSpaceTemplate_Id parkingSpaceTemplateId &&
                   Equals(parkingSpaceTemplateId);

        #endregion

        #region Equals(ParkingSpaceTemplateId)

        /// <summary>
        /// Compares two parking space template identifications for equality.
        /// </summary>
        /// <param name="ParkingSpaceTemplateId">A parking space template identification to compare with.</param>
        public Boolean Equals(ParkingSpaceTemplate_Id ParkingSpaceTemplateId)

            => String.Equals(InternalId,
                             ParkingSpaceTemplateId.InternalId,
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
