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
    /// The unique identification of a template of electric vehicle charging tariffs.
    /// </summary>
    public readonly struct ChargingTariffTemplate_Id : IId<ChargingTariffTemplate_Id>
    {

        #region Data

        /// <summary>
        /// The regular expression for parsing a charging tariff template identification.
        /// </summary>
        public  static readonly Regex  ChargingTariffTemplateId_RegEx  = new (@"^([A-Za-z]{2}\*?[A-Za-z0-9]{3})\*?TT([a-zA-Z0-9_][a-zA-Z0-9_\*\-\.€\$]{0,49})$",
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
        /// Generate a new electric vehicle charging tariff template identification
        /// based on the given charging station operator and identification suffix.
        /// </summary>
        private ChargingTariffTemplate_Id(ChargingStationOperator_Id  OperatorId,
                                       String                      Suffix)
        {

            #region Initial checks

            if (Suffix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Suffix), "The charging tariff template identification suffix must not be null or empty!");

            #endregion

            this.OperatorId  = OperatorId;
            this.Suffix      = Suffix;

        }

        #endregion


        #region Random(OperatorId, Mapper = null)

        /// <summary>
        /// Generate a new unique identification of a charging tariff template.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging station operator.</param>
        /// <param name="Mapper">A delegate to modify the newly generated charging tariff template identification.</param>
        public static ChargingTariffTemplate_Id Random(ChargingStationOperator_Id  OperatorId,
                                                    Func<String, String>?       Mapper   = null)


            => new (OperatorId,
                    Mapper is not null
                        ? Mapper(RandomExtensions.RandomString(30))
                        :        RandomExtensions.RandomString(30));

        #endregion

        #region Parse(Text)

        /// <summary>
        /// Parse the given string as a charging tariff template identification.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff template identification.</param>
        public static ChargingTariffTemplate_Id Parse(String Text)
        {

            #region Initial checks

            if (Text.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Text), "The given text representation of a charging tariff template identification must not be null or empty!");

            #endregion

            var matchCollection = ChargingTariffTemplateId_RegEx.Matches(Text);

            if (matchCollection.Count != 1)
                throw new ArgumentException("Illegal text representation of a charging tariff template identification: '{Text}'!",
                                            nameof(Text));

            if (ChargingStationOperator_Id.TryParse(matchCollection[0].Groups[1].Value, out ChargingStationOperator_Id chargingStationOperatorId))
                return new ChargingTariffTemplate_Id(chargingStationOperatorId,
                                                  matchCollection[0].Groups[2].Value);

            throw new ArgumentException("Illegal charging tariff template identification '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region Parse(OperatorId, Suffix)

        /// <summary>
        /// Parse the given string as a charging tariff template identification.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging station operator.</param>
        /// <param name="Suffix">The suffix of the charging tariff template identification.</param>
        public static ChargingTariffTemplate_Id Parse(ChargingStationOperator_Id  OperatorId,
                                              String                      Suffix)

            => Parse(OperatorId.ToString() + "*TT" + Suffix);

        #endregion

        #region TryParse(Text, out ChargingTariffTemplate_Id)

        /// <summary>
        /// Parse the given string as a charging tariff template identification.
        /// </summary>
        public static Boolean TryParse(String Text, out ChargingTariffTemplate_Id ChargingTariffTemplateId)
        {

            #region Initial checks

            if (Text.IsNullOrEmpty())
            {
                ChargingTariffTemplateId = default;
                return false;
            }

            #endregion

            try
            {

                ChargingTariffTemplateId = default(ChargingTariffTemplate_Id);

                var matchCollection = ChargingTariffTemplateId_RegEx.Matches(Text);

                if (matchCollection.Count != 1)
                    return false;

                if (ChargingStationOperator_Id.TryParse(matchCollection[0].Groups[1].Value, out ChargingStationOperator_Id chargingStationOperatorId))
                {

                    ChargingTariffTemplateId = new ChargingTariffTemplate_Id(chargingStationOperatorId,
                                                                       matchCollection[0].Groups[2].Value);

                    return true;

                }

            }
            catch
            { }

            ChargingTariffTemplateId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging tariff template identification.
        /// </summary>
        public ChargingTariffTemplate_Id Clone()

            => new (
                   OperatorId.Clone(),
                   Suffix.    CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ChargingTariffTemplateId1, ChargingTariffTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffTemplateId1">A charging tariff template identification.</param>
        /// <param name="ChargingTariffTemplateId2">Another charging tariff template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingTariffTemplate_Id ChargingTariffTemplateId1,
                                           ChargingTariffTemplate_Id ChargingTariffTemplateId2)

            => ChargingTariffTemplateId1.Equals(ChargingTariffTemplateId2);

        #endregion

        #region Operator != (ChargingTariffTemplateId1, ChargingTariffTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffTemplateId1">A charging tariff template identification.</param>
        /// <param name="ChargingTariffTemplateId2">Another charging tariff template identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingTariffTemplate_Id ChargingTariffTemplateId1,
                                           ChargingTariffTemplate_Id ChargingTariffTemplateId2)

            => !ChargingTariffTemplateId1.Equals(ChargingTariffTemplateId2);

        #endregion

        #region Operator <  (ChargingTariffTemplateId1, ChargingTariffTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffTemplateId1">A charging tariff template identification.</param>
        /// <param name="ChargingTariffTemplateId2">Another charging tariff template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingTariffTemplate_Id ChargingTariffTemplateId1,
                                          ChargingTariffTemplate_Id ChargingTariffTemplateId2)

            => ChargingTariffTemplateId1.CompareTo(ChargingTariffTemplateId2) < 0;

        #endregion

        #region Operator <= (ChargingTariffTemplateId1, ChargingTariffTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffTemplateId1">A charging tariff template identification.</param>
        /// <param name="ChargingTariffTemplateId2">Another charging tariff template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingTariffTemplate_Id ChargingTariffTemplateId1,
                                           ChargingTariffTemplate_Id ChargingTariffTemplateId2)

            => ChargingTariffTemplateId1.CompareTo(ChargingTariffTemplateId2) <= 0;

        #endregion

        #region Operator >  (ChargingTariffTemplateId1, ChargingTariffTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffTemplateId1">A charging tariff template identification.</param>
        /// <param name="ChargingTariffTemplateId2">Another charging tariff template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingTariffTemplate_Id ChargingTariffTemplateId1,
                                          ChargingTariffTemplate_Id ChargingTariffTemplateId2)

            => ChargingTariffTemplateId1.CompareTo(ChargingTariffTemplateId2) > 0;

        #endregion

        #region Operator >= (ChargingTariffTemplateId1, ChargingTariffTemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffTemplateId1">A charging tariff template identification.</param>
        /// <param name="ChargingTariffTemplateId2">Another charging tariff template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingTariffTemplate_Id ChargingTariffTemplateId1,
                                           ChargingTariffTemplate_Id ChargingTariffTemplateId2)

            => ChargingTariffTemplateId1.CompareTo(ChargingTariffTemplateId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingTariffTemplateId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging tariff template identifications.
        /// </summary>
        /// <param name="Object">A charging tariff template identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingTariffTemplate_Id chargingTariffTemplateId
                   ? CompareTo(chargingTariffTemplateId)
                   : throw new ArgumentException("The given object is not a charging tariff template identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingTariffTemplateId)

        /// <summary>
        /// Compares two charging tariff template identifications.
        /// </summary>
        /// <param name="ChargingTariffTemplateId">A charging tariff template identification to compare with.</param>
        public Int32 CompareTo(ChargingTariffTemplate_Id ChargingTariffTemplateId)
        {

            var c = OperatorId.CompareTo(ChargingTariffTemplateId.OperatorId);

            if (c == 0)
                c = String.Compare(Suffix,
                                   ChargingTariffTemplateId.Suffix,
                                   StringComparison.OrdinalIgnoreCase);

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<ChargingTariffTemplateId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging tariff template identifications for equality.
        /// </summary>
        /// <param name="Object">A charging tariff template identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariffTemplate_Id chargingTariffTemplateId &&
                   Equals(chargingTariffTemplateId);

        #endregion

        #region Equals(ChargingTariffTemplateId)

        /// <summary>
        /// Compares two charging tariff template identifications for equality.
        /// </summary>
        /// <param name="ChargingTariffTemplateId">A charging tariff template identification to compare with.</param>
        public Boolean Equals(ChargingTariffTemplate_Id ChargingTariffTemplateId)

            => OperatorId.Equals(ChargingTariffTemplateId.OperatorId) &&

               String.Equals(Suffix,
                             ChargingTariffTemplateId.Suffix,
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
            => String.Concat(OperatorId, "*TT", Suffix);

        #endregion

    }

}
