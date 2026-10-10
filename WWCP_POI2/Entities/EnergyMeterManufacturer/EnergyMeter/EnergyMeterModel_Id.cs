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
    /// Extension methods for energy meter model identifications.
    /// </summary>
    public static class EnergyMeterModelIdExtensions
    {

        /// <summary>
        /// Indicates whether this energy meter model identification is null or empty.
        /// </summary>
        /// <param name="EnergyMeterModelId">A energy meter model identification.</param>
        public static Boolean IsNullOrEmpty(this EnergyMeterModel_Id? EnergyMeterModelId)
            => !EnergyMeterModelId.HasValue || EnergyMeterModelId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this energy meter model identification is NOT null or empty.
        /// </summary>
        /// <param name="EnergyMeterModelId">A energy meter model identification.</param>
        public static Boolean IsNotNullOrEmpty(this EnergyMeterModel_Id? EnergyMeterModelId)
            => EnergyMeterModelId.HasValue && EnergyMeterModelId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a energy meter model.
    /// </summary>
    public readonly struct EnergyMeterModel_Id : IId<EnergyMeterModel_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this energy meter model identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this energy meter model identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the energy meter model identification.
        /// </summary>
        public UInt64 Length
            => (UInt64)(InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter model identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a energy meter model identification.</param>
        private EnergyMeterModel_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random energy meter model identification.
        /// </summary>
        /// <param name="Length">The expected length of the energy meter model identification.</param>
        public static EnergyMeterModel_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a energy meter model identification.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter model identification.</param>
        public static EnergyMeterModel_Id Parse(String Text)
        {

            if (TryParse(Text, out var energyMeterModelId))
                return energyMeterModelId;

            throw new ArgumentException($"Invalid text representation of a energy meter model identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a energy meter model identification.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter model identification.</param>
        public static EnergyMeterModel_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var energyMeterModelId))
                return energyMeterModelId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EnergyMeterModelId)

        /// <summary>
        /// Try to parse the given text as a energy meter model identification.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter model identification.</param>
        /// <param name="EnergyMeterModelId">The parsed energy meter model identification.</param>
        public static Boolean TryParse(String Text, out EnergyMeterModel_Id EnergyMeterModelId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EnergyMeterModelId = new EnergyMeterModel_Id(Text);
                    return true;
                }
                catch
                { }
            }

            EnergyMeterModelId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this energy meter model identification.
        /// </summary>
        public EnergyMeterModel_Id Clone()

            => new(
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (EnergyMeterModelId1, EnergyMeterModelId2)

        /// <summary>
        /// Compares two energy meter model identifications for equality.
        /// </summary>
        /// <param name="EnergyMeterModelId1">A energy meter model identification.</param>
        /// <param name="EnergyMeterModelId2">Another energy meter model identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator ==(EnergyMeterModel_Id EnergyMeterModelId1,
                                           EnergyMeterModel_Id EnergyMeterModelId2)

            => EnergyMeterModelId1.Equals(EnergyMeterModelId2);

        #endregion

        #region Operator != (EnergyMeterModelId1, EnergyMeterModelId2)

        /// <summary>
        /// Compares two energy meter model identifications for inequality.
        /// </summary>
        /// <param name="EnergyMeterModelId1">A energy meter model identification.</param>
        /// <param name="EnergyMeterModelId2">Another energy meter model identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator !=(EnergyMeterModel_Id EnergyMeterModelId1,
                                           EnergyMeterModel_Id EnergyMeterModelId2)

            => !EnergyMeterModelId1.Equals(EnergyMeterModelId2);

        #endregion

        #region Operator <  (EnergyMeterModelId1, EnergyMeterModelId2)

        /// <summary>
        /// Compares two energy meter model identifications.
        /// </summary>
        /// <param name="EnergyMeterModelId1">A energy meter model identification.</param>
        /// <param name="EnergyMeterModelId2">Another energy meter model identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <(EnergyMeterModel_Id EnergyMeterModelId1,
                                          EnergyMeterModel_Id EnergyMeterModelId2)

            => EnergyMeterModelId1.CompareTo(EnergyMeterModelId2) < 0;

        #endregion

        #region Operator <= (EnergyMeterModelId1, EnergyMeterModelId2)

        /// <summary>
        /// Compares two energy meter model identifications.
        /// </summary>
        /// <param name="EnergyMeterModelId1">A energy meter model identification.</param>
        /// <param name="EnergyMeterModelId2">Another energy meter model identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <=(EnergyMeterModel_Id EnergyMeterModelId1,
                                           EnergyMeterModel_Id EnergyMeterModelId2)

            => EnergyMeterModelId1.CompareTo(EnergyMeterModelId2) <= 0;

        #endregion

        #region Operator >  (EnergyMeterModelId1, EnergyMeterModelId2)

        /// <summary>
        /// Compares two energy meter model identifications.
        /// </summary>
        /// <param name="EnergyMeterModelId1">A energy meter model identification.</param>
        /// <param name="EnergyMeterModelId2">Another energy meter model identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >(EnergyMeterModel_Id EnergyMeterModelId1,
                                          EnergyMeterModel_Id EnergyMeterModelId2)

            => EnergyMeterModelId1.CompareTo(EnergyMeterModelId2) > 0;

        #endregion

        #region Operator >= (EnergyMeterModelId1, EnergyMeterModelId2)

        /// <summary>
        /// Compares two energy meter model identifications.
        /// </summary>
        /// <param name="EnergyMeterModelId1">A energy meter model identification.</param>
        /// <param name="EnergyMeterModelId2">Another energy meter model identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >=(EnergyMeterModel_Id EnergyMeterModelId1,
                                           EnergyMeterModel_Id EnergyMeterModelId2)

            => EnergyMeterModelId1.CompareTo(EnergyMeterModelId2) >= 0;

        #endregion

        #endregion

        #region IComparable<EnergyMeterModelId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two energy meter model identifications.
        /// </summary>
        /// <param name="Object">A energy meter model identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyMeterModel_Id energyMeterModelId
                   ? CompareTo(energyMeterModelId)
                   : throw new ArgumentException("The given object is not a energy meter model identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EnergyMeterModelId)

        /// <summary>
        /// Compares two energy meter model identifications.
        /// </summary>
        /// <param name="EnergyMeterModelId">A energy meter model identification to compare with.</param>
        public Int32 CompareTo(EnergyMeterModel_Id EnergyMeterModelId)

            => String.Compare(InternalId,
                              EnergyMeterModelId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EnergyMeterModelId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy meter model identifications for equality.
        /// </summary>
        /// <param name="Object">A energy meter model identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterModel_Id energyMeterModelId &&
                   Equals(energyMeterModelId);

        #endregion

        #region Equals(EnergyMeterModelId)

        /// <summary>
        /// Compares two energy meter model identifications for equality.
        /// </summary>
        /// <param name="EnergyMeterModelId">A energy meter model identification to compare with.</param>
        public Boolean Equals(EnergyMeterModel_Id EnergyMeterModelId)

            => String.Equals(InternalId,
                             EnergyMeterModelId.InternalId,
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
