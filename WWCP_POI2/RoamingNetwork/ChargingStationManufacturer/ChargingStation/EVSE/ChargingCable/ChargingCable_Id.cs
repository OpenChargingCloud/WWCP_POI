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
    /// A delegate for filtering charging cable identifications.
    /// </summary>
    /// <param name="ChargingCableId">A charging cable identification to include.</param>
    public delegate Boolean IncludeChargingCableIdDelegate(ChargingCable_Id ChargingCableId);


    /// <summary>
    /// Extension methods for charging cable identifications.
    /// </summary>
    public static class ChargingCableIdExtensions
    {

        /// <summary>
        /// Indicates whether this charging cable identification is null or empty.
        /// </summary>
        /// <param name="ChargingCableId">A charging cable identification.</param>
        public static Boolean IsNullOrEmpty(this ChargingCable_Id? ChargingCableId)
            => !ChargingCableId.HasValue || ChargingCableId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this charging cable identification is NOT null or empty.
        /// </summary>
        /// <param name="ChargingCableId">A charging cable identification.</param>
        public static Boolean IsNotNullOrEmpty(this ChargingCable_Id? ChargingCableId)
            => ChargingCableId.HasValue && ChargingCableId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a charging cable.
    /// CiString(3)
    /// </summary>
    public readonly struct ChargingCable_Id : IId<ChargingCable_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this charging cable identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this charging cable identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the charging cable identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging cable identification based on the given text.
        /// </summary>
        /// <param name="Text">A text representation of a charging cable identification.</param>
        private ChargingCable_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse    (Text)

        /// <summary>
        /// Parse the given text as a charging cable identification.
        /// </summary>
        /// <param name="Text">A text representation of a charging cable identification.</param>
        public static ChargingCable_Id Parse(String Text)
        {

            if (TryParse(Text, out var chargingCableId))
                return chargingCableId;

            throw new ArgumentException($"Invalid text representation of a charging cable identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) Parse    (Number)

        /// <summary>
        /// Parse the given number as a charging cable identification.
        /// </summary>
        /// <param name="number">A numeric representation of a charging cable identification.</param>
        public static ChargingCable_Id Parse(UInt16 Number)
        {

            if (TryParse(Number, out var chargingCableId))
                return chargingCableId;

            throw new ArgumentException("Invalid numeric representation of a charging cable identification: '" + Number + "'!",
                                        nameof(Number));

        }

        #endregion

        #region (static) TryParse (Text)

        /// <summary>
        /// Try to parse the given text as a charging cable identification.
        /// </summary>
        /// <param name="Text">A text representation of a charging cable identification.</param>
        public static ChargingCable_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var chargingCableId))
                return chargingCableId;

            return null;

        }

        #endregion

        #region (static) TryParse (Number)

        /// <summary>
        /// Try to parse the given number as a charging cable identification.
        /// </summary>
        /// <param name="number">A numeric representation of a charging cable identification.</param>
        public static ChargingCable_Id? TryParse(UInt16 Number)
        {

            if (TryParse(Number, out var chargingCableId))
                return chargingCableId;

            return null;

        }

        #endregion

        #region (static) TryParse (Text,   out ChargingCableId)

        /// <summary>
        /// Try to parse the given text as a charging cable identification.
        /// </summary>
        /// <param name="Text">A text representation of a charging cable identification.</param>
        /// <param name="ChargingCableId">The parsed charging cable identification.</param>
        public static Boolean TryParse(String Text, out ChargingCable_Id ChargingCableId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChargingCableId = new ChargingCable_Id(Text);
                    return true;
                }
                catch
                { }
            }

            ChargingCableId = default;
            return false;

        }

        #endregion

        #region (static) TryParse (Number, out ChargingCableId)

        /// <summary>
        /// Try to parse the given number as a charging cable identification.
        /// </summary>
        /// <param name="number">A numeric representation of a charging cable identification.</param>
        /// <param name="ChargingCableId">The parsed charging cable identification.</param>
        public static Boolean TryParse(UInt16 Number, out ChargingCable_Id ChargingCableId)
        {

            try
            {
                ChargingCableId = new ChargingCable_Id(Number.ToString());
                return true;
            }
            catch
            { }

            ChargingCableId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging cable identification.
        /// </summary>
        public ChargingCable_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ChargingCableId1, ChargingCableId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingCableId1">A charging cable identification.</param>
        /// <param name="ChargingCableId2">Another charging cable identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingCable_Id ChargingCableId1,
                                           ChargingCable_Id ChargingCableId2)

            => ChargingCableId1.Equals(ChargingCableId2);

        #endregion

        #region Operator != (ChargingCableId1, ChargingCableId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingCableId1">A charging cable identification.</param>
        /// <param name="ChargingCableId2">Another charging cable identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingCable_Id ChargingCableId1,
                                           ChargingCable_Id ChargingCableId2)

            => !ChargingCableId1.Equals(ChargingCableId2);

        #endregion

        #region Operator <  (ChargingCableId1, ChargingCableId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingCableId1">A charging cable identification.</param>
        /// <param name="ChargingCableId2">Another charging cable identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingCable_Id ChargingCableId1,
                                          ChargingCable_Id ChargingCableId2)

            => ChargingCableId1.CompareTo(ChargingCableId2) < 0;

        #endregion

        #region Operator <= (ChargingCableId1, ChargingCableId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingCableId1">A charging cable identification.</param>
        /// <param name="ChargingCableId2">Another charging cable identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingCable_Id ChargingCableId1,
                                           ChargingCable_Id ChargingCableId2)

            => ChargingCableId1.CompareTo(ChargingCableId2) <= 0;

        #endregion

        #region Operator >  (ChargingCableId1, ChargingCableId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingCableId1">A charging cable identification.</param>
        /// <param name="ChargingCableId2">Another charging cable identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingCable_Id ChargingCableId1,
                                          ChargingCable_Id ChargingCableId2)

            => ChargingCableId1.CompareTo(ChargingCableId2) > 0;

        #endregion

        #region Operator >= (ChargingCableId1, ChargingCableId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingCableId1">A charging cable identification.</param>
        /// <param name="ChargingCableId2">Another charging cable identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingCable_Id ChargingCableId1,
                                           ChargingCable_Id ChargingCableId2)

            => ChargingCableId1.CompareTo(ChargingCableId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingCableId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging cable identifications.
        /// </summary>
        /// <param name="Object">A charging cable identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingCable_Id chargingCableId
                   ? CompareTo(chargingCableId)
                   : throw new ArgumentException("The given object is not a charging cable identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingCableId)

        /// <summary>
        /// Compares two charging cable identifications.
        /// </summary>
        /// <param name="ChargingCableId">A charging cable identification to compare with.</param>
        public Int32 CompareTo(ChargingCable_Id ChargingCableId)

            => String.Compare(InternalId,
                              ChargingCableId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChargingCableId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging cable identifications for equality.
        /// </summary>
        /// <param name="Object">A charging cable identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingCable_Id chargingCableId &&
                   Equals(chargingCableId);

        #endregion

        #region Equals(ChargingCableId)

        /// <summary>
        /// Compares two charging cable identifications for equality.
        /// </summary>
        /// <param name="ChargingCableId">A charging cable identification to compare with.</param>
        public Boolean Equals(ChargingCable_Id ChargingCableId)

            => String.Equals(InternalId,
                             ChargingCableId.InternalId,
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
