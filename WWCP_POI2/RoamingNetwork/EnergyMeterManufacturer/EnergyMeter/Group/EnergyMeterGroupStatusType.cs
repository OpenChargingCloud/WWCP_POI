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
    /// Extension methods for energy meter group status types.
    /// </summary>
    public static class EnergyMeterGroupStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this energy meter group status types is null or empty.
        /// </summary>
        /// <param name="EnergyMeterGroupStatusType">An energy meter group status type.</param>
        public static Boolean IsNullOrEmpty(this EnergyMeterGroupStatusType? EnergyMeterGroupStatusType)
            => !EnergyMeterGroupStatusType.HasValue || EnergyMeterGroupStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this energy meter group status types is null or empty.
        /// </summary>
        /// <param name="EnergyMeterGroupStatusType">An energy meter group status type.</param>
        public static Boolean IsNotNullOrEmpty(this EnergyMeterGroupStatusType? EnergyMeterGroupStatusType)
            => EnergyMeterGroupStatusType.HasValue && EnergyMeterGroupStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of an energy meter group.
    /// </summary>
    public readonly struct EnergyMeterGroupStatusType : IId,
                                                              IEquatable<EnergyMeterGroupStatusType>,
                                                              IComparable<EnergyMeterGroupStatusType>
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
        /// The length of the energy meter group status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter group status type based on the given string.
        /// </summary>
        private EnergyMeterGroupStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as an energy meter group status type.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter group status type.</param>
        public static EnergyMeterGroupStatusType Parse(String Text)
        {

            if (TryParse(Text, out EnergyMeterGroupStatusType energyMeterGroupStatusType))
                return energyMeterGroupStatusType;

            throw new ArgumentException($"Invalid text representation of an energy meter group status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as an energy meter group status type.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter group status type.</param>
        public static EnergyMeterGroupStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out EnergyMeterGroupStatusType energyMeterGroupStatusType))
                return energyMeterGroupStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EnergyMeterGroupStatusType)

        /// <summary>
        /// Parse the given string as an energy meter group status type.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter group status type.</param>
        /// <param name="EnergyMeterGroupStatusType">The parsed energy meter group status type.</param>
        public static Boolean TryParse(String Text, out EnergyMeterGroupStatusType EnergyMeterGroupStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EnergyMeterGroupStatusType = new EnergyMeterGroupStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            EnergyMeterGroupStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this energy meter group status type.
        /// </summary>
        public EnergyMeterGroupStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the energy meter group.
        /// </summary>
        public static readonly EnergyMeterGroupStatusType Unknown           = new ("unknown");

        /// <summary>
        /// Unclear status of the energy meter group.
        /// </summary>
        public static readonly EnergyMeterGroupStatusType Unspecified       = new ("unspecified");

        /// <summary>
        /// The energy meters of the group are being deployed, not fully operational yet.
        /// </summary>
        public static readonly EnergyMeterGroupStatusType InDeployment      = new ("inDeployment");

        /// <summary>
        /// Currently no communication with the energy meters of the group is possible.
        /// </summary>
        public static readonly EnergyMeterGroupStatusType Offline           = new ("offline");

        /// <summary>
        /// All energy meters of the group are available.
        /// </summary>
        public static readonly EnergyMeterGroupStatusType Available         = new ("available");

        /// <summary>
        /// Some energy meters of the group are available, others are not.
        /// </summary>
        public static readonly EnergyMeterGroupStatusType PartialAvailable  = new ("partialAvailable");

        /// <summary>
        /// An error occurred within the energy meters of the group.
        /// </summary>
        public static readonly EnergyMeterGroupStatusType Error             = new ("error");

        #endregion


        #region Operator overloading

        #region Operator == (EnergyMeterGroupStatusType1, EnergyMeterGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterGroupStatusType1">An energy meter group status type.</param>
        /// <param name="EnergyMeterGroupStatusType2">Another energy meter group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EnergyMeterGroupStatusType EnergyMeterGroupStatusType1,
                                           EnergyMeterGroupStatusType EnergyMeterGroupStatusType2)

            => EnergyMeterGroupStatusType1.Equals(EnergyMeterGroupStatusType2);

        #endregion

        #region Operator != (EnergyMeterGroupStatusType1, EnergyMeterGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterGroupStatusType1">An energy meter group status type.</param>
        /// <param name="EnergyMeterGroupStatusType2">Another energy meter group status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EnergyMeterGroupStatusType EnergyMeterGroupStatusType1,
                                           EnergyMeterGroupStatusType EnergyMeterGroupStatusType2)

            => !EnergyMeterGroupStatusType1.Equals(EnergyMeterGroupStatusType2);

        #endregion

        #region Operator <  (EnergyMeterGroupStatusType1, EnergyMeterGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterGroupStatusType1">An energy meter group status type.</param>
        /// <param name="EnergyMeterGroupStatusType2">Another energy meter group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EnergyMeterGroupStatusType EnergyMeterGroupStatusType1,
                                          EnergyMeterGroupStatusType EnergyMeterGroupStatusType2)

            => EnergyMeterGroupStatusType1.CompareTo(EnergyMeterGroupStatusType2) < 0;

        #endregion

        #region Operator <= (EnergyMeterGroupStatusType1, EnergyMeterGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterGroupStatusType1">An energy meter group status type.</param>
        /// <param name="EnergyMeterGroupStatusType2">Another energy meter group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EnergyMeterGroupStatusType EnergyMeterGroupStatusType1,
                                           EnergyMeterGroupStatusType EnergyMeterGroupStatusType2)

            => EnergyMeterGroupStatusType1.CompareTo(EnergyMeterGroupStatusType2) <= 0;

        #endregion

        #region Operator >  (EnergyMeterGroupStatusType1, EnergyMeterGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterGroupStatusType1">An energy meter group status type.</param>
        /// <param name="EnergyMeterGroupStatusType2">Another energy meter group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EnergyMeterGroupStatusType EnergyMeterGroupStatusType1,
                                          EnergyMeterGroupStatusType EnergyMeterGroupStatusType2)

            => EnergyMeterGroupStatusType1.CompareTo(EnergyMeterGroupStatusType2) > 0;

        #endregion

        #region Operator >= (EnergyMeterGroupStatusType1, EnergyMeterGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterGroupStatusType1">An energy meter group status type.</param>
        /// <param name="EnergyMeterGroupStatusType2">Another energy meter group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EnergyMeterGroupStatusType EnergyMeterGroupStatusType1,
                                           EnergyMeterGroupStatusType EnergyMeterGroupStatusType2)

            => EnergyMeterGroupStatusType1.CompareTo(EnergyMeterGroupStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<EnergyMeterGroupStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two energy meter group status types.
        /// </summary>
        /// <param name="Object">An energy meter group status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyMeterGroupStatusType energyMeterGroupStatusType
                   ? CompareTo(energyMeterGroupStatusType)
                   : throw new ArgumentException("The given object is not an energy meter group status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EnergyMeterGroupStatusType)

        /// <summary>
        /// Compares two energy meter group status types.
        /// </summary>
        /// <param name="EnergyMeterGroupStatusType">An energy meter group status type to compare with.</param>
        public Int32 CompareTo(EnergyMeterGroupStatusType EnergyMeterGroupStatusType)

            => String.Compare(InternalId,
                              EnergyMeterGroupStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EnergyMeterGroupStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy meter group status types for equality.
        /// </summary>
        /// <param name="Object">An energy meter group status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterGroupStatusType energyMeterGroupStatusType &&
                   Equals(energyMeterGroupStatusType);

        #endregion

        #region Equals(EnergyMeterGroupStatusType)

        /// <summary>
        /// Compares two energy meter group status types for equality.
        /// </summary>
        /// <param name="EnergyMeterGroupStatusType">An energy meter group status type to compare with.</param>
        public Boolean Equals(EnergyMeterGroupStatusType EnergyMeterGroupStatusType)

            => String.Equals(InternalId,
                             EnergyMeterGroupStatusType.InternalId,
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
