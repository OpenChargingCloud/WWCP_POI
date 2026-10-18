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
    /// The unique identification of a template of electric vehicle charging pools.
    /// </summary>
    public readonly struct ChargingPoolTemplate_Id : IId<ChargingPoolTemplate_Id>
    {

        #region Data

        /// <summary>
        /// The regular expression for parsing a charging pool template identification.
        /// </summary>
        public static readonly Regex ChargingPoolTemplateId_RegEx  = new (@"^([A-Za-z]{2}\*?[A-Za-z0-9]{3})\*?PT([a-zA-Z0-9_][a-zA-Z0-9_\*\-\.€\$]{0,49})$",
                                                                          RegexOptions.IgnorePatternWhitespace);

        #endregion

        #region Properties

        /// <summary>
        /// The charging pool operator identification.
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
        /// Generate a new electric vehicle charging pool template identification
        /// based on the given charging pool operator and identification suffix.
        /// </summary>
        private ChargingPoolTemplate_Id(ChargingStationOperator_Id  OperatorId,
                                     String                      Suffix)
        {

            #region Initial checks

            if (Suffix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Suffix), "The charging pool template identification suffix must not be null or empty!");

            #endregion

            this.OperatorId  = OperatorId;
            this.Suffix      = Suffix;

        }

        #endregion


        #region Random(OperatorId, Mapper = null)

        /// <summary>
        /// Generate a new unique identification of a charging pool template.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging pool operator.</param>
        /// <param name="Mapper">A delegate to modify the newly generated charging pool template identification.</param>
        public static ChargingPoolTemplate_Id Random(ChargingStationOperator_Id  OperatorId,
                                                     Func<String, String>?       Mapper   = null)


            => new (OperatorId,
                    Mapper is not null
                        ? Mapper(RandomExtensions.RandomString(30))
                        :        RandomExtensions.RandomString(30));

        #endregion

        #region Parse(Text)

        /// <summary>
        /// Parse the given string as a charging pool template identification.
        /// </summary>
        /// <param name="Text">A text representation of a charging pool template identification.</param>
        public static ChargingPoolTemplate_Id Parse(String Text)
        {

            #region Initial checks

            if (Text.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Text), "The given text representation of a charging pool template identification must not be null or empty!");

            #endregion

            var matchCollection = ChargingPoolTemplateId_RegEx.Matches(Text);

            if (matchCollection.Count != 1)
                throw new ArgumentException("Illegal text representation of a charging pool template identification: '{Text}'!",
                                            nameof(Text));

            if (ChargingStationOperator_Id.TryParse(matchCollection[0].Groups[1].Value, out ChargingStationOperator_Id chargingStationOperatorId))
                return new ChargingPoolTemplate_Id(chargingStationOperatorId,
                                                   matchCollection[0].Groups[2].Value);

            throw new ArgumentException("Illegal charging pool template identification '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region Parse(OperatorId, Suffix)

        /// <summary>
        /// Parse the given string as a charging pool template identification.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging pool operator.</param>
        /// <param name="Suffix">The suffix of the charging pool template identification.</param>
        public static ChargingPoolTemplate_Id Parse(ChargingStationOperator_Id  OperatorId,
                                                    String                      Suffix)

            => Parse(OperatorId.ToString() + "*PT" + Suffix);

        #endregion

        #region TryParse(Text, out ChargingPoolTemplate_Id)

        /// <summary>
        /// Parse the given string as a charging pool template identification.
        /// </summary>
        public static Boolean TryParse(String Text, out ChargingPoolTemplate_Id ChargingPoolTemplateId)
        {

            #region Initial checks

            ChargingPoolTemplateId = default;

            if (Text.IsNullOrEmpty())
                return false;

            #endregion

            try
            {

                var matchCollection = ChargingPoolTemplateId_RegEx.Matches(Text);

                if (matchCollection.Count != 1)
                    return false;

                if (ChargingStationOperator_Id.TryParse(matchCollection[0].Groups[1].Value, out ChargingStationOperator_Id chargingStationOperatorId))
                {

                    ChargingPoolTemplateId = new ChargingPoolTemplate_Id(chargingStationOperatorId,
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
        /// Clone this charging pool template identification.
        /// </summary>
        public ChargingPoolTemplate_Id Clone()

            => new (
                   OperatorId.Clone(),
                   Suffix.    CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ChargingPoolTemplateId1, ChargingPoolTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolTemplateId1">A charging pool template identification.</param>
        /// <param name="ChargingPoolTemplateId2">Another charging pool template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingPoolTemplate_Id ChargingPoolTemplateId1,
                                           ChargingPoolTemplate_Id ChargingPoolTemplateId2)

            => ChargingPoolTemplateId1.Equals(ChargingPoolTemplateId2);

        #endregion

        #region Operator != (ChargingPoolTemplateId1, ChargingPoolTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolTemplateId1">A charging pool template identification.</param>
        /// <param name="ChargingPoolTemplateId2">Another charging pool template identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingPoolTemplate_Id ChargingPoolTemplateId1,
                                           ChargingPoolTemplate_Id ChargingPoolTemplateId2)

            => !ChargingPoolTemplateId1.Equals(ChargingPoolTemplateId2);

        #endregion

        #region Operator <  (ChargingPoolTemplateId1, ChargingPoolTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolTemplateId1">A charging pool template identification.</param>
        /// <param name="ChargingPoolTemplateId2">Another charging pool template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingPoolTemplate_Id ChargingPoolTemplateId1,
                                          ChargingPoolTemplate_Id ChargingPoolTemplateId2)

            => ChargingPoolTemplateId1.CompareTo(ChargingPoolTemplateId2) < 0;

        #endregion

        #region Operator <= (ChargingPoolTemplateId1, ChargingPoolTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolTemplateId1">A charging pool template identification.</param>
        /// <param name="ChargingPoolTemplateId2">Another charging pool template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingPoolTemplate_Id ChargingPoolTemplateId1,
                                           ChargingPoolTemplate_Id ChargingPoolTemplateId2)

            => ChargingPoolTemplateId1.CompareTo(ChargingPoolTemplateId2) <= 0;

        #endregion

        #region Operator >  (ChargingPoolTemplateId1, ChargingPoolTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolTemplateId1">A charging pool template identification.</param>
        /// <param name="ChargingPoolTemplateId2">Another charging pool template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingPoolTemplate_Id ChargingPoolTemplateId1,
                                          ChargingPoolTemplate_Id ChargingPoolTemplateId2)

            => ChargingPoolTemplateId1.CompareTo(ChargingPoolTemplateId2) > 0;

        #endregion

        #region Operator >= (ChargingPoolTemplateId1, ChargingPoolTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPoolTemplateId1">A charging pool template identification.</param>
        /// <param name="ChargingPoolTemplateId2">Another charging pool template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingPoolTemplate_Id ChargingPoolTemplateId1,
                                           ChargingPoolTemplate_Id ChargingPoolTemplateId2)

            => ChargingPoolTemplateId1.CompareTo(ChargingPoolTemplateId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingPoolTemplateId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging pool template identifications.
        /// </summary>
        /// <param name="Object">A charging pool template identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingPoolTemplate_Id chargingPoolTemplateId
                   ? CompareTo(chargingPoolTemplateId)
                   : throw new ArgumentException("The given object is not a charging pool template identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingPoolTemplateId)

        /// <summary>
        /// Compares two charging pool template identifications.
        /// </summary>
        /// <param name="ChargingPoolTemplateId">A charging pool template identification to compare with.</param>
        public Int32 CompareTo(ChargingPoolTemplate_Id ChargingPoolTemplateId)
        {

            var c = OperatorId.CompareTo(ChargingPoolTemplateId.OperatorId);

            if (c == 0)
                c = String.Compare(Suffix,
                                   ChargingPoolTemplateId.Suffix,
                                   StringComparison.OrdinalIgnoreCase);

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<ChargingPoolTemplateId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging pool template identifications for equality.
        /// </summary>
        /// <param name="Object">A charging pool template identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingPoolTemplate_Id chargingPoolTemplateId &&
                   Equals(chargingPoolTemplateId);

        #endregion

        #region Equals(ChargingPoolTemplateId)

        /// <summary>
        /// Compares two charging pool template identifications for equality.
        /// </summary>
        /// <param name="ChargingPoolTemplateId">A charging pool template identification to compare with.</param>
        public Boolean Equals(ChargingPoolTemplate_Id ChargingPoolTemplateId)

            => OperatorId.Equals(ChargingPoolTemplateId.OperatorId) &&

               String.Equals(Suffix,
                             ChargingPoolTemplateId.Suffix,
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
            => String.Concat(OperatorId, "*PT", Suffix);

        #endregion

    }

}
