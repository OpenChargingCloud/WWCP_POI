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
    /// Extension methods for energy meter group identifications.
    /// </summary>
    public static class EnergyMeterGroupIdExtensions
    {

        /// <summary>
        /// Indicates whether this energy meter group identification is null or empty.
        /// </summary>
        /// <param name="EnergyMeterGroupId">An energy meter group identification.</param>
        public static Boolean IsNullOrEmpty(this EnergyMeterGroup_Id? EnergyMeterGroupId)
            => !EnergyMeterGroupId.HasValue || EnergyMeterGroupId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this energy meter group identification is NOT null or empty.
        /// </summary>
        /// <param name="EnergyMeterGroupId">An energy meter group identification.</param>
        public static Boolean IsNotNullOrEmpty(this EnergyMeterGroup_Id? EnergyMeterGroupId)
            => EnergyMeterGroupId.HasValue && EnergyMeterGroupId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of an energy meter group.
    /// </summary>
    public readonly struct EnergyMeterGroup_Id : IId<EnergyMeterGroup_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this energy meter group identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this energy meter group identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the energy meter group identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter group identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of an energy meter group identification.</param>
        private EnergyMeterGroup_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) New

        /// <summary>
        /// Create a new unique energy meter group identification (UUIDv7).
        /// </summary>
        public static EnergyMeterGroup_Id New

            => new (UUIDv7.Generate().ToString());

        #endregion

        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random energy meter group identification.
        /// </summary>
        /// <param name="Length">The expected length of the energy meter group identification.</param>
        public static EnergyMeterGroup_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as an energy meter group identification.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter group identification.</param>
        public static EnergyMeterGroup_Id Parse(String Text)
        {

            if (TryParse(Text, out var energyMeterGroupId))
                return energyMeterGroupId;

            throw new ArgumentException($"Invalid text representation of an energy meter group identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as an energy meter group identification.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter group identification.</param>
        public static EnergyMeterGroup_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var energyMeterGroupId))
                return energyMeterGroupId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EnergyMeterGroupId)

        /// <summary>
        /// Try to parse the given text as an energy meter group identification.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter group identification.</param>
        /// <param name="EnergyMeterGroupId">The parsed energy meter group identification.</param>
        public static Boolean TryParse(String Text, out EnergyMeterGroup_Id EnergyMeterGroupId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EnergyMeterGroupId = new EnergyMeterGroup_Id(Text);
                    return true;
                }
                catch
                { }
            }

            EnergyMeterGroupId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this energy meter group identification.
        /// </summary>
        public EnergyMeterGroup_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (EnergyMeterGroupId1, EnergyMeterGroupId2)

        /// <summary>
        /// Compares two energy meter group identifications for equality.
        /// </summary>
        /// <param name="EnergyMeterGroupId1">An energy meter group identification.</param>
        /// <param name="EnergyMeterGroupId2">Another energy meter group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EnergyMeterGroup_Id EnergyMeterGroupId1,
                                           EnergyMeterGroup_Id EnergyMeterGroupId2)

            => EnergyMeterGroupId1.Equals(EnergyMeterGroupId2);

        #endregion

        #region Operator != (EnergyMeterGroupId1, EnergyMeterGroupId2)

        /// <summary>
        /// Compares two energy meter group identifications for inequality.
        /// </summary>
        /// <param name="EnergyMeterGroupId1">An energy meter group identification.</param>
        /// <param name="EnergyMeterGroupId2">Another energy meter group identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EnergyMeterGroup_Id EnergyMeterGroupId1,
                                           EnergyMeterGroup_Id EnergyMeterGroupId2)

            => !EnergyMeterGroupId1.Equals(EnergyMeterGroupId2);

        #endregion

        #region Operator <  (EnergyMeterGroupId1, EnergyMeterGroupId2)

        /// <summary>
        /// Compares two energy meter group identifications.
        /// </summary>
        /// <param name="EnergyMeterGroupId1">An energy meter group identification.</param>
        /// <param name="EnergyMeterGroupId2">Another energy meter group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EnergyMeterGroup_Id EnergyMeterGroupId1,
                                          EnergyMeterGroup_Id EnergyMeterGroupId2)

            => EnergyMeterGroupId1.CompareTo(EnergyMeterGroupId2) < 0;

        #endregion

        #region Operator <= (EnergyMeterGroupId1, EnergyMeterGroupId2)

        /// <summary>
        /// Compares two energy meter group identifications.
        /// </summary>
        /// <param name="EnergyMeterGroupId1">An energy meter group identification.</param>
        /// <param name="EnergyMeterGroupId2">Another energy meter group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EnergyMeterGroup_Id EnergyMeterGroupId1,
                                           EnergyMeterGroup_Id EnergyMeterGroupId2)

            => EnergyMeterGroupId1.CompareTo(EnergyMeterGroupId2) <= 0;

        #endregion

        #region Operator >  (EnergyMeterGroupId1, EnergyMeterGroupId2)

        /// <summary>
        /// Compares two energy meter group identifications.
        /// </summary>
        /// <param name="EnergyMeterGroupId1">An energy meter group identification.</param>
        /// <param name="EnergyMeterGroupId2">Another energy meter group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EnergyMeterGroup_Id EnergyMeterGroupId1,
                                          EnergyMeterGroup_Id EnergyMeterGroupId2)

            => EnergyMeterGroupId1.CompareTo(EnergyMeterGroupId2) > 0;

        #endregion

        #region Operator >= (EnergyMeterGroupId1, EnergyMeterGroupId2)

        /// <summary>
        /// Compares two energy meter group identifications.
        /// </summary>
        /// <param name="EnergyMeterGroupId1">An energy meter group identification.</param>
        /// <param name="EnergyMeterGroupId2">Another energy meter group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EnergyMeterGroup_Id EnergyMeterGroupId1,
                                           EnergyMeterGroup_Id EnergyMeterGroupId2)

            => EnergyMeterGroupId1.CompareTo(EnergyMeterGroupId2) >= 0;

        #endregion

        #endregion

        #region IComparable<EnergyMeterGroupId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two energy meter group identifications.
        /// </summary>
        /// <param name="Object">An energy meter group identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyMeterGroup_Id energyMeterGroupId
                   ? CompareTo(energyMeterGroupId)
                   : throw new ArgumentException("The given object is not an energy meter group identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EnergyMeterGroupId)

        /// <summary>
        /// Compares two energy meter group identifications.
        /// </summary>
        /// <param name="EnergyMeterGroupId">An energy meter group identification to compare with.</param>
        public Int32 CompareTo(EnergyMeterGroup_Id EnergyMeterGroupId)

            => String.Compare(InternalId,
                              EnergyMeterGroupId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EnergyMeterGroupId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy meter group identifications for equality.
        /// </summary>
        /// <param name="Object">An energy meter group identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterGroup_Id energyMeterGroupId &&
                   Equals(energyMeterGroupId);

        #endregion

        #region Equals(EnergyMeterGroupId)

        /// <summary>
        /// Compares two energy meter group identifications for equality.
        /// </summary>
        /// <param name="EnergyMeterGroupId">An energy meter group identification to compare with.</param>
        public Boolean Equals(EnergyMeterGroup_Id EnergyMeterGroupId)

            => String.Equals(InternalId,
                             EnergyMeterGroupId.InternalId,
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
