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
    /// The unique identification of a template of electric vehicle charging stations.
    /// </summary>
    public readonly struct ChargingStationTemplate_Id : IId<ChargingStationTemplate_Id>
    {

        #region Data

        /// <summary>
        /// The regular expression for parsing a charging station template identification.
        /// </summary>
        public static readonly Regex ChargingStationTemplateId_RegEx  = new (@"^([A-Za-z]{2}\*?[A-Za-z0-9]{3})\*?ST([a-zA-Z0-9_][a-zA-Z0-9_\*\-\.€\$]{0,49})\z",
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
        /// Generate a new electric vehicle charging station template identification
        /// based on the given charging station operator and identification suffix.
        /// </summary>
        private ChargingStationTemplate_Id(ChargingStationOperator_Id  OperatorId,
                                        String                      Suffix)
        {

            #region Initial checks

            if (Suffix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Suffix), "The charging station template identification suffix must not be null or empty!");

            #endregion

            this.OperatorId  = OperatorId;
            this.Suffix      = Suffix;

        }

        #endregion


        #region Random(OperatorId, Mapper = null)

        /// <summary>
        /// Generate a new unique identification of a charging station template.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging station operator.</param>
        /// <param name="Mapper">A delegate to modify the newly generated charging station template identification.</param>
        public static ChargingStationTemplate_Id Random(ChargingStationOperator_Id  OperatorId,
                                                     Func<String, String>?       Mapper   = null)


            => new (OperatorId,
                    Mapper is not null
                        ? Mapper(RandomExtensions.RandomString(30))
                        :        RandomExtensions.RandomString(30));

        #endregion

        #region Parse(Text)

        /// <summary>
        /// Parse the given string as a charging station template identification.
        /// </summary>
        /// <param name="Text">A text representation of a charging station template identification.</param>
        public static ChargingStationTemplate_Id Parse(String Text)
        {

            #region Initial checks

            if (Text.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Text), "The given text representation of a charging station template identification must not be null or empty!");

            #endregion

            var matchCollection = ChargingStationTemplateId_RegEx.Matches(Text);

            if (matchCollection.Count != 1)
                throw new ArgumentException("Illegal text representation of a charging station template identification: '{Text}'!",
                                            nameof(Text));

            if (ChargingStationOperator_Id.TryParse(matchCollection[0].Groups[1].Value, out ChargingStationOperator_Id chargingStationOperatorId))
                return new ChargingStationTemplate_Id(chargingStationOperatorId,
                                                   matchCollection[0].Groups[2].Value);

            throw new ArgumentException("Illegal charging station template identification '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region Parse(OperatorId, Suffix)

        /// <summary>
        /// Parse the given string as a charging station template identification.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging station operator.</param>
        /// <param name="Suffix">The suffix of the charging station template identification.</param>
        public static ChargingStationTemplate_Id Parse(ChargingStationOperator_Id  OperatorId,
                                                    String                      Suffix)

            => Parse(OperatorId.ToString() + "*ST" + Suffix);

        #endregion

        #region TryParse(Text, out ChargingStationTemplate_Id)

        /// <summary>
        /// Parse the given string as a charging station template identification.
        /// </summary>
        public static Boolean TryParse(String Text, out ChargingStationTemplate_Id ChargingStationTemplateId)
        {

            #region Initial checks

            ChargingStationTemplateId = default;

            if (Text.IsNullOrEmpty())
                return false;

            #endregion

            try
            {

                var matchCollection = ChargingStationTemplateId_RegEx.Matches(Text);

                if (matchCollection.Count != 1)
                    return false;

                if (ChargingStationOperator_Id.TryParse(matchCollection[0].Groups[1].Value, out ChargingStationOperator_Id chargingStationOperatorId))
                {

                    ChargingStationTemplateId = new ChargingStationTemplate_Id(chargingStationOperatorId,
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
        /// Clone this charging station template identification.
        /// </summary>
        public ChargingStationTemplate_Id Clone()

            => new (
                   OperatorId.Clone(),
                   Suffix.    CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ChargingStationTemplateId1, ChargingStationTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationTemplateId1">A charging station template identification.</param>
        /// <param name="ChargingStationTemplateId2">Another charging station template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingStationTemplate_Id ChargingStationTemplateId1,
                                           ChargingStationTemplate_Id ChargingStationTemplateId2)

            => ChargingStationTemplateId1.Equals(ChargingStationTemplateId2);

        #endregion

        #region Operator != (ChargingStationTemplateId1, ChargingStationTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationTemplateId1">A charging station template identification.</param>
        /// <param name="ChargingStationTemplateId2">Another charging station template identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingStationTemplate_Id ChargingStationTemplateId1,
                                           ChargingStationTemplate_Id ChargingStationTemplateId2)

            => !ChargingStationTemplateId1.Equals(ChargingStationTemplateId2);

        #endregion

        #region Operator <  (ChargingStationTemplateId1, ChargingStationTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationTemplateId1">A charging station template identification.</param>
        /// <param name="ChargingStationTemplateId2">Another charging station template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingStationTemplate_Id ChargingStationTemplateId1,
                                          ChargingStationTemplate_Id ChargingStationTemplateId2)

            => ChargingStationTemplateId1.CompareTo(ChargingStationTemplateId2) < 0;

        #endregion

        #region Operator <= (ChargingStationTemplateId1, ChargingStationTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationTemplateId1">A charging station template identification.</param>
        /// <param name="ChargingStationTemplateId2">Another charging station template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingStationTemplate_Id ChargingStationTemplateId1,
                                           ChargingStationTemplate_Id ChargingStationTemplateId2)

            => ChargingStationTemplateId1.CompareTo(ChargingStationTemplateId2) <= 0;

        #endregion

        #region Operator >  (ChargingStationTemplateId1, ChargingStationTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationTemplateId1">A charging station template identification.</param>
        /// <param name="ChargingStationTemplateId2">Another charging station template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingStationTemplate_Id ChargingStationTemplateId1,
                                          ChargingStationTemplate_Id ChargingStationTemplateId2)

            => ChargingStationTemplateId1.CompareTo(ChargingStationTemplateId2) > 0;

        #endregion

        #region Operator >= (ChargingStationTemplateId1, ChargingStationTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationTemplateId1">A charging station template identification.</param>
        /// <param name="ChargingStationTemplateId2">Another charging station template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingStationTemplate_Id ChargingStationTemplateId1,
                                           ChargingStationTemplate_Id ChargingStationTemplateId2)

            => ChargingStationTemplateId1.CompareTo(ChargingStationTemplateId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingStationTemplateId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging station template identifications.
        /// </summary>
        /// <param name="Object">A charging station template identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingStationTemplate_Id chargingStationTemplateId
                   ? CompareTo(chargingStationTemplateId)
                   : throw new ArgumentException("The given object is not a charging station template identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingStationTemplateId)

        /// <summary>
        /// Compares two charging station template identifications.
        /// </summary>
        /// <param name="ChargingStationTemplateId">A charging station template identification to compare with.</param>
        public Int32 CompareTo(ChargingStationTemplate_Id ChargingStationTemplateId)
        {

            var c = OperatorId.CompareTo(ChargingStationTemplateId.OperatorId);

            if (c == 0)
                c = String.Compare(Suffix,
                                   ChargingStationTemplateId.Suffix,
                                   StringComparison.OrdinalIgnoreCase);

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<ChargingStationTemplateId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging station template identifications for equality.
        /// </summary>
        /// <param name="Object">A charging station template identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingStationTemplate_Id chargingStationTemplateId &&
                   Equals(chargingStationTemplateId);

        #endregion

        #region Equals(ChargingStationTemplateId)

        /// <summary>
        /// Compares two charging station template identifications for equality.
        /// </summary>
        /// <param name="ChargingStationTemplateId">A charging station template identification to compare with.</param>
        public Boolean Equals(ChargingStationTemplate_Id ChargingStationTemplateId)

            => OperatorId.Equals(ChargingStationTemplateId.OperatorId) &&

               String.Equals(Suffix,
                             ChargingStationTemplateId.Suffix,
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
            => String.Concat(OperatorId, "*ST", Suffix);

        #endregion

    }

}
