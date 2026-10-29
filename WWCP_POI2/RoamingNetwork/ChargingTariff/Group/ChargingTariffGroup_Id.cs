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

using System;
using System.Text.RegularExpressions;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The unique identification of of a group of electric vehicle charging tariff group.
    /// </summary>
    public readonly struct ChargingTariffGroup_Id : IId<ChargingTariffGroup_Id>
    {

        #region Data

        /// <summary>
        /// The regular expression for parsing a charging tariff group identification.
        /// </summary>
        public  static readonly Regex  ChargingTariffGroupId_RegEx  = new (@"^([A-Za-z]{2}\*?[A-Za-z0-9]{3})\*?TG([a-zA-Z0-9_][a-zA-Z0-9_\*\-\.€\$]{0,49})\z",
                                                                           RegexOptions.IgnorePatternWhitespace);

        #endregion

        #region Properties

        /// <summary>
        /// The charging station operator identification.
        /// </summary>
        public ChargingStationOperator_Id  OperatorId   { get; }

        /// <summary>
        /// The suffix of the identification.
        /// </summary>
        public String                      Suffix       { get; }

        /// <summary>
        /// Indicates whether this identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => Suffix.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => Suffix.IsNotNullOrEmpty();

        /// <summary>
        /// Returns the length of the identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (OperatorId.ToString().Length + 3 + Suffix.Length);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Generate a new electric vehicle charging tariff group identification
        /// based on the given charging station operator and identification suffix.
        /// </summary>
        private ChargingTariffGroup_Id(ChargingStationOperator_Id  OperatorId,
                                       String                      Suffix)
        {

            #region Initial checks

            if (Suffix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Suffix), "The charging tariff group identification suffix must not be null or empty!");

            #endregion

            this.OperatorId  = OperatorId;
            this.Suffix      = Suffix;

        }

        #endregion


        #region Random(OperatorId, Mapper = null)

        /// <summary>
        /// Generate a new unique identification of a charging tariff group.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging station operator.</param>
        /// <param name="Mapper">A delegate to modify the newly generated charging tariff group identification.</param>
        public static ChargingTariffGroup_Id Random(ChargingStationOperator_Id  OperatorId,
                                                    Func<String, String>?       Mapper   = null)


            => new (OperatorId,
                    Mapper is not null
                        ? Mapper(RandomExtensions.RandomString(30))
                        :        RandomExtensions.RandomString(30));

        #endregion

        #region Parse(Text)

        /// <summary>
        /// Parse the given string as a charging tariff group identification.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff group identification.</param>
        public static ChargingTariffGroup_Id Parse(String Text)
        {

            #region Initial checks

            if (Text.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Text), "The given text representation of a charging tariff group identification must not be null or empty!");

            #endregion

            var matchCollection = ChargingTariffGroupId_RegEx.Matches(Text);

            if (matchCollection.Count != 1)
                throw new ArgumentException("Illegal text representation of a charging tariff group identification: '{Text}'!",
                                            nameof(Text));

            if (ChargingStationOperator_Id.TryParse(matchCollection[0].Groups[1].Value, out ChargingStationOperator_Id chargingStationOperatorId))
                return new ChargingTariffGroup_Id(chargingStationOperatorId,
                                                  matchCollection[0].Groups[2].Value);

            throw new ArgumentException("Illegal charging tariff group identification '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region Parse(OperatorId, Suffix)

        /// <summary>
        /// Parse the given string as a charging tariff group identification.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging station operator.</param>
        /// <param name="Suffix">The suffix of the charging tariff group identification.</param>
        public static ChargingTariffGroup_Id Parse(ChargingStationOperator_Id  OperatorId,
                                              String                      Suffix)

            => Parse(OperatorId.ToString() + "*TG" + Suffix);

        #endregion

        #region TryParse(Text, out ChargingTariffGroup_Id)

        /// <summary>
        /// Parse the given string as a charging tariff group identification.
        /// </summary>
        public static Boolean TryParse(String Text, out ChargingTariffGroup_Id ChargingTariffGroupId)
        {

            #region Initial checks

            if (Text.IsNullOrEmpty())
            {
                ChargingTariffGroupId = default;
                return false;
            }

            #endregion

            try
            {

                ChargingTariffGroupId = default(ChargingTariffGroup_Id);

                var matchCollection = ChargingTariffGroupId_RegEx.Matches(Text);

                if (matchCollection.Count != 1)
                    return false;

                if (ChargingStationOperator_Id.TryParse(matchCollection[0].Groups[1].Value, out ChargingStationOperator_Id chargingStationOperatorId))
                {

                    ChargingTariffGroupId = new ChargingTariffGroup_Id(chargingStationOperatorId,
                                                                       matchCollection[0].Groups[2].Value);

                    return true;

                }

            }
            catch
            { }

            ChargingTariffGroupId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging tariff group identification.
        /// </summary>
        public ChargingTariffGroup_Id Clone()

            => new (
                   OperatorId.Clone(),
                   Suffix.    CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ChargingTariffGroupId1, ChargingTariffGroupId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupId1">A charging tariff group identification.</param>
        /// <param name="ChargingTariffGroupId2">Another charging tariff group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingTariffGroup_Id ChargingTariffGroupId1,
                                           ChargingTariffGroup_Id ChargingTariffGroupId2)

            => ChargingTariffGroupId1.Equals(ChargingTariffGroupId2);

        #endregion

        #region Operator != (ChargingTariffGroupId1, ChargingTariffGroupId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupId1">A charging tariff group identification.</param>
        /// <param name="ChargingTariffGroupId2">Another charging tariff group identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingTariffGroup_Id ChargingTariffGroupId1,
                                           ChargingTariffGroup_Id ChargingTariffGroupId2)

            => !ChargingTariffGroupId1.Equals(ChargingTariffGroupId2);

        #endregion

        #region Operator <  (ChargingTariffGroupId1, ChargingTariffGroupId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupId1">A charging tariff group identification.</param>
        /// <param name="ChargingTariffGroupId2">Another charging tariff group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingTariffGroup_Id ChargingTariffGroupId1,
                                          ChargingTariffGroup_Id ChargingTariffGroupId2)

            => ChargingTariffGroupId1.CompareTo(ChargingTariffGroupId2) < 0;

        #endregion

        #region Operator <= (ChargingTariffGroupId1, ChargingTariffGroupId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupId1">A charging tariff group identification.</param>
        /// <param name="ChargingTariffGroupId2">Another charging tariff group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingTariffGroup_Id ChargingTariffGroupId1,
                                           ChargingTariffGroup_Id ChargingTariffGroupId2)

            => ChargingTariffGroupId1.CompareTo(ChargingTariffGroupId2) <= 0;

        #endregion

        #region Operator >  (ChargingTariffGroupId1, ChargingTariffGroupId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupId1">A charging tariff group identification.</param>
        /// <param name="ChargingTariffGroupId2">Another charging tariff group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingTariffGroup_Id ChargingTariffGroupId1,
                                          ChargingTariffGroup_Id ChargingTariffGroupId2)

            => ChargingTariffGroupId1.CompareTo(ChargingTariffGroupId2) > 0;

        #endregion

        #region Operator >= (ChargingTariffGroupId1, ChargingTariffGroupId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupId1">A charging tariff group identification.</param>
        /// <param name="ChargingTariffGroupId2">Another charging tariff group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingTariffGroup_Id ChargingTariffGroupId1,
                                           ChargingTariffGroup_Id ChargingTariffGroupId2)

            => ChargingTariffGroupId1.CompareTo(ChargingTariffGroupId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingTariffGroupId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging tariff group identifications.
        /// </summary>
        /// <param name="Object">A charging tariff group identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingTariffGroup_Id chargingTariffGroupId
                   ? CompareTo(chargingTariffGroupId)
                   : throw new ArgumentException("The given object is not a charging tariff group identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingTariffGroupId)

        /// <summary>
        /// Compares two charging tariff group identifications.
        /// </summary>
        /// <param name="ChargingTariffGroupId">A charging tariff group identification to compare with.</param>
        public Int32 CompareTo(ChargingTariffGroup_Id ChargingTariffGroupId)
        {

            var c = OperatorId.CompareTo(ChargingTariffGroupId.OperatorId);

            if (c == 0)
                c = String.Compare(Suffix,
                                   ChargingTariffGroupId.Suffix,
                                   StringComparison.OrdinalIgnoreCase);

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<ChargingTariffGroupId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging tariff group identifications for equality.
        /// </summary>
        /// <param name="Object">A charging tariff group identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariffGroup_Id chargingTariffGroupId &&
                   Equals(chargingTariffGroupId);

        #endregion

        #region Equals(ChargingTariffGroupId)

        /// <summary>
        /// Compares two charging tariff group identifications for equality.
        /// </summary>
        /// <param name="ChargingTariffGroupId">A charging tariff group identification to compare with.</param>
        public Boolean Equals(ChargingTariffGroup_Id ChargingTariffGroupId)

            => OperatorId.Equals(ChargingTariffGroupId.OperatorId) &&

               String.Equals(Suffix,
                             ChargingTariffGroupId.Suffix,
                             StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => OperatorId.GetHashCode() ^
              (Suffix is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Suffix));

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => String.Concat(OperatorId, "*TG", Suffix);

        #endregion

    }

}
