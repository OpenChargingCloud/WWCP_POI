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
    /// The unique identification of of a group of electric vehicle charging pools.
    /// </summary>
    public readonly struct ChargingPoolGroup_Id : IId<ChargingPoolGroup_Id>
    {

        #region Data

        /// <summary>
        /// The regular expression for parsing a charging pool group identification.
        /// </summary>
        public static readonly Regex ChargingPoolGroupId_RegEx  = new (@"^([A-Za-z]{2}[\*\-]?[A-Za-z0-9]{3})\*?PG([a-zA-Z0-9_][a-zA-Z0-9_\*\-\.€\$]{0,49})$",
                                                                          RegexOptions.IgnorePatternWhitespace);

        #endregion

        #region Properties

        /// <summary>
        /// The identification of the party embedding the group: its charging station operator, or its e-mobility
        /// provider, whose identification is written as one of a charging station operator, with a '*' separator.
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
        /// Generate a new electric vehicle charging pool group identification
        /// based on the given charging pool operator and identification suffix.
        /// </summary>
        private ChargingPoolGroup_Id(ChargingStationOperator_Id  OperatorId,
                                     String                      Suffix)
        {

            #region Initial checks

            if (Suffix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Suffix), "The charging pool group identification suffix must not be null or empty!");

            #endregion

            this.OperatorId  = OperatorId;
            this.Suffix      = Suffix;

        }

        #endregion


        #region Random(OperatorId, Mapper = null)

        /// <summary>
        /// Generate a new unique identification of a charging pool group.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging pool operator.</param>
        /// <param name="Mapper">A delegate to modify the newly generated charging pool group identification.</param>
        public static ChargingPoolGroup_Id Random(ChargingStationOperator_Id  OperatorId,
                                                     Func<String, String>?       Mapper   = null)


            => new (OperatorId,
                    Mapper is not null
                        ? Mapper(RandomExtensions.RandomString(30))
                        :        RandomExtensions.RandomString(30));

        #endregion

        #region Parse(Text)

        /// <summary>
        /// Parse the given string as a charging pool group identification.
        /// </summary>
        /// <param name="Text">A text representation of a charging pool group identification.</param>
        public static ChargingPoolGroup_Id Parse(String Text)
        {

            #region Initial checks

            if (Text.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Text), "The given text representation of a charging pool group identification must not be null or empty!");

            #endregion

            var matchCollection = ChargingPoolGroupId_RegEx.Matches(Text);

            if (matchCollection.Count != 1)
                throw new ArgumentException("Illegal text representation of a charging pool group identification: '{Text}'!",
                                            nameof(Text));

            if (ChargingStationOperator_Id.TryParse(matchCollection[0].Groups[1].Value.Replace('-', '*'), out ChargingStationOperator_Id chargingStationOperatorId))
                return new ChargingPoolGroup_Id(chargingStationOperatorId,
                                                   matchCollection[0].Groups[2].Value);

            throw new ArgumentException("Illegal charging pool group identification '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region Parse(OperatorId, Suffix)

        /// <summary>
        /// Parse the given string as a charging pool group identification.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging pool operator.</param>
        /// <param name="Suffix">The suffix of the charging pool group identification.</param>
        public static ChargingPoolGroup_Id Parse(ChargingStationOperator_Id  OperatorId,
                                                    String                      Suffix)

            => Parse(OperatorId.ToString() + "*PG" + Suffix);

        #endregion

        #region Parse(OperatorId, ChargingTariffGroupId, Suffix)

        /// <summary>
        /// Parse the given string as a charging pool group identification.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging pool operator.</param>
        /// <param name="Suffix">The suffix of the charging pool group identification.</param>
        public static ChargingPoolGroup_Id Parse(ChargingStationOperator_Id  OperatorId,
                                                    ChargingTariffGroup_Id      ChargingTariffGroupId,
                                                    String                      Suffix)

            => Parse(OperatorId.ToString() + "*PG_" + ChargingTariffGroupId + "_" + Suffix);

        #endregion

        #region TryParse(Text, out ChargingPoolGroup_Id)

        /// <summary>
        /// Parse the given string as a charging pool group identification.
        /// </summary>
        public static Boolean TryParse(String Text, out ChargingPoolGroup_Id ChargingPoolGroupId)
        {

            #region Initial checks

            ChargingPoolGroupId = default;

            if (Text.IsNullOrEmpty())
                return false;

            #endregion

            try
            {

                var matchCollection = ChargingPoolGroupId_RegEx.Matches(Text);

                if (matchCollection.Count != 1)
                    return false;

                if (ChargingStationOperator_Id.TryParse(matchCollection[0].Groups[1].Value.Replace('-', '*'), out ChargingStationOperator_Id chargingStationOperatorId))
                {

                    ChargingPoolGroupId = new ChargingPoolGroup_Id(chargingStationOperatorId,
                                                                         matchCollection[0].Groups[2].Value);

                    return true;

                }

            }
            catch
            { }

            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging pool group identification.
        /// </summary>
        public ChargingPoolGroup_Id Clone()

            => new (
                   OperatorId.Clone(),
                   Suffix.    CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ChargingPoolGroupId1, ChargingPoolGroupId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupId1">A charging pool group identification.</param>
        /// <param name="ChargingPoolGroupId2">Another charging pool group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingPoolGroup_Id ChargingPoolGroupId1,
                                           ChargingPoolGroup_Id ChargingPoolGroupId2)

            => ChargingPoolGroupId1.Equals(ChargingPoolGroupId2);

        #endregion

        #region Operator != (ChargingPoolGroupId1, ChargingPoolGroupId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupId1">A charging pool group identification.</param>
        /// <param name="ChargingPoolGroupId2">Another charging pool group identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingPoolGroup_Id ChargingPoolGroupId1,
                                           ChargingPoolGroup_Id ChargingPoolGroupId2)

            => !ChargingPoolGroupId1.Equals(ChargingPoolGroupId2);

        #endregion

        #region Operator <  (ChargingPoolGroupId1, ChargingPoolGroupId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupId1">A charging pool group identification.</param>
        /// <param name="ChargingPoolGroupId2">Another charging pool group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingPoolGroup_Id ChargingPoolGroupId1,
                                          ChargingPoolGroup_Id ChargingPoolGroupId2)

            => ChargingPoolGroupId1.CompareTo(ChargingPoolGroupId2) < 0;

        #endregion

        #region Operator <= (ChargingPoolGroupId1, ChargingPoolGroupId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupId1">A charging pool group identification.</param>
        /// <param name="ChargingPoolGroupId2">Another charging pool group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingPoolGroup_Id ChargingPoolGroupId1,
                                           ChargingPoolGroup_Id ChargingPoolGroupId2)

            => ChargingPoolGroupId1.CompareTo(ChargingPoolGroupId2) <= 0;

        #endregion

        #region Operator >  (ChargingPoolGroupId1, ChargingPoolGroupId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupId1">A charging pool group identification.</param>
        /// <param name="ChargingPoolGroupId2">Another charging pool group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingPoolGroup_Id ChargingPoolGroupId1,
                                          ChargingPoolGroup_Id ChargingPoolGroupId2)

            => ChargingPoolGroupId1.CompareTo(ChargingPoolGroupId2) > 0;

        #endregion

        #region Operator >= (ChargingPoolGroupId1, ChargingPoolGroupId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolGroupId1">A charging pool group identification.</param>
        /// <param name="ChargingPoolGroupId2">Another charging pool group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingPoolGroup_Id ChargingPoolGroupId1,
                                           ChargingPoolGroup_Id ChargingPoolGroupId2)

            => ChargingPoolGroupId1.CompareTo(ChargingPoolGroupId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingPoolGroupId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging pool group identifications.
        /// </summary>
        /// <param name="Object">A charging pool group identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingPoolGroup_Id chargingPoolGroupId
                   ? CompareTo(chargingPoolGroupId)
                   : throw new ArgumentException("The given object is not a charging pool group identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingPoolGroupId)

        /// <summary>
        /// Compares two charging pool group identifications.
        /// </summary>
        /// <param name="ChargingPoolGroupId">A charging pool group identification to compare with.</param>
        public Int32 CompareTo(ChargingPoolGroup_Id ChargingPoolGroupId)
        {

            var c = OperatorId.CompareTo(ChargingPoolGroupId.OperatorId);

            if (c == 0)
                c = String.Compare(Suffix,
                                   ChargingPoolGroupId.Suffix,
                                   StringComparison.OrdinalIgnoreCase);

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<ChargingPoolGroupId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging pool group identifications for equality.
        /// </summary>
        /// <param name="Object">A charging pool group identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingPoolGroup_Id chargingPoolGroupId &&
                   Equals(chargingPoolGroupId);

        #endregion

        #region Equals(ChargingPoolGroupId)

        /// <summary>
        /// Compares two charging pool group identifications for equality.
        /// </summary>
        /// <param name="ChargingPoolGroupId">A charging pool group identification to compare with.</param>
        public Boolean Equals(ChargingPoolGroup_Id ChargingPoolGroupId)

            => OperatorId.Equals(ChargingPoolGroupId.OperatorId) &&

               String.Equals(Suffix,
                             ChargingPoolGroupId.Suffix,
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
            => String.Concat(OperatorId, "*PG", Suffix);

        #endregion

    }

}
