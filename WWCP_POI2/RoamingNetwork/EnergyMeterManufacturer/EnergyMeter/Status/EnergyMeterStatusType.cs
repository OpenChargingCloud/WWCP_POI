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
    /// Extension methods for energy meter status typess.
    /// </summary>
    public static class EnergyMeterStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this energy meter status types is null or empty.
        /// </summary>
        /// <param name="EnergyMeterStatusType">An energy meter status type.</param>
        public static Boolean IsNullOrEmpty(this EnergyMeterStatusType? EnergyMeterStatusType)
            => !EnergyMeterStatusType.HasValue || EnergyMeterStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this energy meter status types is null or empty.
        /// </summary>
        /// <param name="EnergyMeterStatusType">An energy meter status type.</param>
        public static Boolean IsNotNullOrEmpty(this EnergyMeterStatusType? EnergyMeterStatusType)
            => EnergyMeterStatusType.HasValue && EnergyMeterStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status type of an energy meter.
    /// </summary>
    public readonly struct EnergyMeterStatusType : IId,
                                                   IEquatable<EnergyMeterStatusType>,
                                                   IComparable<EnergyMeterStatusType>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the energy meter status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter status types based on the given string.
        /// </summary>
        private EnergyMeterStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as an energy meter status type.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter status type.</param>
        public static EnergyMeterStatusType Parse(String Text)
        {

            if (TryParse(Text, out EnergyMeterStatusType energyMeterStatusTypes))
                return energyMeterStatusTypes;

            throw new ArgumentException($"Invalid text representation of an energy meter status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as an energy meter status type.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter status type.</param>
        public static EnergyMeterStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out EnergyMeterStatusType energyMeterStatusTypes))
                return energyMeterStatusTypes;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EnergyMeterStatusType)

        /// <summary>
        /// Parse the given string as an energy meter status type.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter status type.</param>
        /// <param name="EnergyMeterStatusType">The parsed energy meter status type.</param>
        public static Boolean TryParse(String Text, out EnergyMeterStatusType EnergyMeterStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EnergyMeterStatusType = new EnergyMeterStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            EnergyMeterStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this energy meter status type.
        /// </summary>
        public EnergyMeterStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the energy meter.
        /// </summary>
        public static readonly EnergyMeterStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear status of the energy meter.
        /// </summary>
        public static readonly EnergyMeterStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The energy meter is being deployed, not fully operational yet.
        /// </summary>
        public static readonly EnergyMeterStatusType InDeployment = new ("inDeployment");

        /// <summary>
        /// Currently no communication with the energy meter is possible.
        /// </summary>
        public static readonly EnergyMeterStatusType Offline      = new ("offline");

        /// <summary>
        /// The energy meter is available.
        /// </summary>
        public static readonly EnergyMeterStatusType Available    = new ("available");

        /// <summary>
        /// An error occurred within the energy meter.
        /// </summary>
        public static readonly EnergyMeterStatusType Error        = new ("error");

        #endregion


        #region Operator overloading

        #region Operator == (EnergyMeterStatusType1, EnergyMeterStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterStatusType1">An energy meter status type.</param>
        /// <param name="EnergyMeterStatusType2">Another energy meter status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EnergyMeterStatusType EnergyMeterStatusType1,
                                           EnergyMeterStatusType EnergyMeterStatusType2)

            => EnergyMeterStatusType1.Equals(EnergyMeterStatusType2);

        #endregion

        #region Operator != (EnergyMeterStatusType1, EnergyMeterStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterStatusType1">An energy meter status type.</param>
        /// <param name="EnergyMeterStatusType2">Another energy meter status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EnergyMeterStatusType EnergyMeterStatusType1,
                                           EnergyMeterStatusType EnergyMeterStatusType2)

            => !EnergyMeterStatusType1.Equals(EnergyMeterStatusType2);

        #endregion

        #region Operator <  (EnergyMeterStatusType1, EnergyMeterStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterStatusType1">An energy meter status type.</param>
        /// <param name="EnergyMeterStatusType2">Another energy meter status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EnergyMeterStatusType EnergyMeterStatusType1,
                                          EnergyMeterStatusType EnergyMeterStatusType2)

            => EnergyMeterStatusType1.CompareTo(EnergyMeterStatusType2) < 0;

        #endregion

        #region Operator <= (EnergyMeterStatusType1, EnergyMeterStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterStatusType1">An energy meter status type.</param>
        /// <param name="EnergyMeterStatusType2">Another energy meter status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EnergyMeterStatusType EnergyMeterStatusType1,
                                           EnergyMeterStatusType EnergyMeterStatusType2)

            => EnergyMeterStatusType1.CompareTo(EnergyMeterStatusType2) <= 0;

        #endregion

        #region Operator >  (EnergyMeterStatusType1, EnergyMeterStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterStatusType1">An energy meter status type.</param>
        /// <param name="EnergyMeterStatusType2">Another energy meter status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EnergyMeterStatusType EnergyMeterStatusType1,
                                          EnergyMeterStatusType EnergyMeterStatusType2)

            => EnergyMeterStatusType1.CompareTo(EnergyMeterStatusType2) > 0;

        #endregion

        #region Operator >= (EnergyMeterStatusType1, EnergyMeterStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterStatusType1">An energy meter status type.</param>
        /// <param name="EnergyMeterStatusType2">Another energy meter status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EnergyMeterStatusType EnergyMeterStatusType1,
                                           EnergyMeterStatusType EnergyMeterStatusType2)

            => EnergyMeterStatusType1.CompareTo(EnergyMeterStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<EnergyMeterStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two energy meter status types.
        /// </summary>
        /// <param name="Object">An energy meter status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyMeterStatusType energyMeterStatusTypes
                   ? CompareTo(energyMeterStatusTypes)
                   : throw new ArgumentException("The given object is not an energy meter status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EnergyMeterStatusType)

        /// <summary>
        /// Compares two energy meter status types.
        /// </summary>
        /// <param name="EnergyMeterStatusType">An energy meter status type to compare with.</param>
        public Int32 CompareTo(EnergyMeterStatusType EnergyMeterStatusType)

            => String.Compare(InternalId,
                              EnergyMeterStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EnergyMeterStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy meter status types for equality.
        /// </summary>
        /// <param name="Object">An energy meter status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterStatusType energyMeterStatusTypes &&
                   Equals(energyMeterStatusTypes);

        #endregion

        #region Equals(EnergyMeterStatusType)

        /// <summary>
        /// Compares two energy meter status types for equality.
        /// </summary>
        /// <param name="EnergyMeterStatusType">An energy meter status type to compare with.</param>
        public Boolean Equals(EnergyMeterStatusType EnergyMeterStatusType)

            => String.Equals(InternalId,
                             EnergyMeterStatusType.InternalId,
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
