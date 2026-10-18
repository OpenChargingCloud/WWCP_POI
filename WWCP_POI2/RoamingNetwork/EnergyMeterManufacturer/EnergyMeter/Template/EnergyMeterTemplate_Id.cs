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
    /// Extension methods for energy meter template identifications.
    /// </summary>
    public static class EnergyMeterTemplateIdExtensions
    {

        /// <summary>
        /// Indicates whether this energy meter template identification is null or empty.
        /// </summary>
        /// <param name="EnergyMeterTemplateId">An energy meter template identification.</param>
        public static Boolean IsNullOrEmpty(this EnergyMeterTemplate_Id? EnergyMeterTemplateId)
            => !EnergyMeterTemplateId.HasValue || EnergyMeterTemplateId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this energy meter template identification is NOT null or empty.
        /// </summary>
        /// <param name="EnergyMeterTemplateId">An energy meter template identification.</param>
        public static Boolean IsNotNullOrEmpty(this EnergyMeterTemplate_Id? EnergyMeterTemplateId)
            => EnergyMeterTemplateId.HasValue && EnergyMeterTemplateId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of an energy meter template.
    /// </summary>
    public readonly struct EnergyMeterTemplate_Id : IId<EnergyMeterTemplate_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this energy meter template identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this energy meter template identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the energy meter template identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter template identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of an energy meter template identification.</param>
        private EnergyMeterTemplate_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) New

        /// <summary>
        /// Create a new unique energy meter template identification (UUIDv7).
        /// </summary>
        public static EnergyMeterTemplate_Id New

            => new (UUIDv7.Generate().ToString());

        #endregion

        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random energy meter template identification.
        /// </summary>
        /// <param name="Length">The expected length of the energy meter template identification.</param>
        public static EnergyMeterTemplate_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as an energy meter template identification.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter template identification.</param>
        public static EnergyMeterTemplate_Id Parse(String Text)
        {

            if (TryParse(Text, out var energyMeterTemplateId))
                return energyMeterTemplateId;

            throw new ArgumentException($"Invalid text representation of an energy meter template identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as an energy meter template identification.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter template identification.</param>
        public static EnergyMeterTemplate_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var energyMeterTemplateId))
                return energyMeterTemplateId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EnergyMeterTemplateId)

        /// <summary>
        /// Try to parse the given text as an energy meter template identification.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter template identification.</param>
        /// <param name="EnergyMeterTemplateId">The parsed energy meter template identification.</param>
        public static Boolean TryParse(String Text, out EnergyMeterTemplate_Id EnergyMeterTemplateId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EnergyMeterTemplateId = new EnergyMeterTemplate_Id(Text);
                    return true;
                }
                catch
                { }
            }

            EnergyMeterTemplateId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this energy meter template identification.
        /// </summary>
        public EnergyMeterTemplate_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (EnergyMeterTemplateId1, EnergyMeterTemplateId2)

        /// <summary>
        /// Compares two energy meter template identifications for equality.
        /// </summary>
        /// <param name="EnergyMeterTemplateId1">An energy meter template identification.</param>
        /// <param name="EnergyMeterTemplateId2">Another energy meter template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EnergyMeterTemplate_Id EnergyMeterTemplateId1,
                                           EnergyMeterTemplate_Id EnergyMeterTemplateId2)

            => EnergyMeterTemplateId1.Equals(EnergyMeterTemplateId2);

        #endregion

        #region Operator != (EnergyMeterTemplateId1, EnergyMeterTemplateId2)

        /// <summary>
        /// Compares two energy meter template identifications for inequality.
        /// </summary>
        /// <param name="EnergyMeterTemplateId1">An energy meter template identification.</param>
        /// <param name="EnergyMeterTemplateId2">Another energy meter template identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EnergyMeterTemplate_Id EnergyMeterTemplateId1,
                                           EnergyMeterTemplate_Id EnergyMeterTemplateId2)

            => !EnergyMeterTemplateId1.Equals(EnergyMeterTemplateId2);

        #endregion

        #region Operator <  (EnergyMeterTemplateId1, EnergyMeterTemplateId2)

        /// <summary>
        /// Compares two energy meter template identifications.
        /// </summary>
        /// <param name="EnergyMeterTemplateId1">An energy meter template identification.</param>
        /// <param name="EnergyMeterTemplateId2">Another energy meter template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EnergyMeterTemplate_Id EnergyMeterTemplateId1,
                                          EnergyMeterTemplate_Id EnergyMeterTemplateId2)

            => EnergyMeterTemplateId1.CompareTo(EnergyMeterTemplateId2) < 0;

        #endregion

        #region Operator <= (EnergyMeterTemplateId1, EnergyMeterTemplateId2)

        /// <summary>
        /// Compares two energy meter template identifications.
        /// </summary>
        /// <param name="EnergyMeterTemplateId1">An energy meter template identification.</param>
        /// <param name="EnergyMeterTemplateId2">Another energy meter template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EnergyMeterTemplate_Id EnergyMeterTemplateId1,
                                           EnergyMeterTemplate_Id EnergyMeterTemplateId2)

            => EnergyMeterTemplateId1.CompareTo(EnergyMeterTemplateId2) <= 0;

        #endregion

        #region Operator >  (EnergyMeterTemplateId1, EnergyMeterTemplateId2)

        /// <summary>
        /// Compares two energy meter template identifications.
        /// </summary>
        /// <param name="EnergyMeterTemplateId1">An energy meter template identification.</param>
        /// <param name="EnergyMeterTemplateId2">Another energy meter template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EnergyMeterTemplate_Id EnergyMeterTemplateId1,
                                          EnergyMeterTemplate_Id EnergyMeterTemplateId2)

            => EnergyMeterTemplateId1.CompareTo(EnergyMeterTemplateId2) > 0;

        #endregion

        #region Operator >= (EnergyMeterTemplateId1, EnergyMeterTemplateId2)

        /// <summary>
        /// Compares two energy meter template identifications.
        /// </summary>
        /// <param name="EnergyMeterTemplateId1">An energy meter template identification.</param>
        /// <param name="EnergyMeterTemplateId2">Another energy meter template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EnergyMeterTemplate_Id EnergyMeterTemplateId1,
                                           EnergyMeterTemplate_Id EnergyMeterTemplateId2)

            => EnergyMeterTemplateId1.CompareTo(EnergyMeterTemplateId2) >= 0;

        #endregion

        #endregion

        #region IComparable<EnergyMeterTemplateId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two energy meter template identifications.
        /// </summary>
        /// <param name="Object">An energy meter template identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyMeterTemplate_Id energyMeterTemplateId
                   ? CompareTo(energyMeterTemplateId)
                   : throw new ArgumentException("The given object is not an energy meter template identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EnergyMeterTemplateId)

        /// <summary>
        /// Compares two energy meter template identifications.
        /// </summary>
        /// <param name="EnergyMeterTemplateId">An energy meter template identification to compare with.</param>
        public Int32 CompareTo(EnergyMeterTemplate_Id EnergyMeterTemplateId)

            => String.Compare(InternalId,
                              EnergyMeterTemplateId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EnergyMeterTemplateId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy meter template identifications for equality.
        /// </summary>
        /// <param name="Object">An energy meter template identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterTemplate_Id energyMeterTemplateId &&
                   Equals(energyMeterTemplateId);

        #endregion

        #region Equals(EnergyMeterTemplateId)

        /// <summary>
        /// Compares two energy meter template identifications for equality.
        /// </summary>
        /// <param name="EnergyMeterTemplateId">An energy meter template identification to compare with.</param>
        public Boolean Equals(EnergyMeterTemplate_Id EnergyMeterTemplateId)

            => String.Equals(InternalId,
                             EnergyMeterTemplateId.InternalId,
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
