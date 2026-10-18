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
    /// The unique identification of a template of Electric Vehicle Supply Equipments (EVSEs).
    /// </summary>
    public readonly struct EVSETemplate_Id : IId<EVSETemplate_Id>
    {

        #region Data



        /// <summary>
        /// The regular expression for parsing an EVSE template identification.
        /// </summary>
        public  static readonly Regex  EVSETemplateId_RegEx  = new Regex(@"^([A-Za-z]{2}\*?[A-Za-z0-9]{3})\*?ET([a-zA-Z0-9_][a-zA-Z0-9_\*\-\.€\$]{0,49})$",
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
        /// Generate a new electric vehicle supply equipment template identification
        /// based on the given charging station operator and identification suffix.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging station operator.</param>
        /// <param name="Suffix">The suffix of the EVSE template identification.</param>
        private EVSETemplate_Id(ChargingStationOperator_Id  OperatorId,
                             String                      Suffix)
        {

            #region Initial checks

            if (Suffix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Suffix),  "The EVSE template identification suffix must not be null or empty!");

            #endregion

            this.OperatorId  = OperatorId;
            this.Suffix      = Suffix;

        }

        #endregion


        #region Random(OperatorId, Mapper = null)

        /// <summary>
        /// Generate a new unique identification of an EVSE template identification.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging station operator.</param>
        /// <param name="Mapper">A delegate to modify the newly generated EVSE template identification.</param>
        public static EVSETemplate_Id Random(ChargingStationOperator_Id  OperatorId,
                                          Func<String, String>?       Mapper   = null)


            => new (OperatorId,
                    Mapper is not null
                        ? Mapper(RandomExtensions.RandomString(30))
                        :        RandomExtensions.RandomString(30));

        #endregion

        #region Parse(Text)

        /// <summary>
        /// Parse the given string as an EVSE template identification.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE template identification.</param>
        public static EVSETemplate_Id Parse(String Text)
        {

            #region Initial checks

            if (Text.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Text), "The given text representation of an EVSE template identification must not be null or empty!");

            #endregion

            var matchCollection = EVSETemplateId_RegEx.Matches(Text);

            if (matchCollection.Count != 1)
                throw new ArgumentException("Illegal text representation of an EVSE template identification: '{Text}'!",
                                            nameof(Text));

            if (ChargingStationOperator_Id.TryParse(matchCollection[0].Groups[1].Value, out ChargingStationOperator_Id chargingStationOperatorId))
                return new EVSETemplate_Id(chargingStationOperatorId,
                                        matchCollection[0].Groups[2].Value);

            throw new ArgumentException("Illegal EVSE template identification '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region Parse(OperatorId, Suffix)

        /// <summary>
        /// Parse the given string as an EVSE template identification.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging station operator.</param>
        /// <param name="Suffix">The suffix of the EVSE template identification.</param>
        public static EVSETemplate_Id Parse(ChargingStationOperator_Id  OperatorId,
                                         String                      Suffix)

            => Parse(OperatorId.ToString() + "*ET" + Suffix);

        #endregion

        #region TryParse(Text, out EVSETemplateId)

        /// <summary>
        /// Parse the given string as an EVSE template identification.
        /// </summary>
        public static Boolean TryParse(String Text, out EVSETemplate_Id EVSETemplateId)
        {

            #region Initial checks

            if (Text.IsNullOrEmpty())
            {
                EVSETemplateId = default;
                return false;
            }

            #endregion

            try
            {

                EVSETemplateId = default;

                var matchCollection = EVSETemplateId_RegEx.Matches(Text);

                if (matchCollection.Count != 1)
                    return false;

                if (ChargingStationOperator_Id.TryParse(matchCollection[0].Groups[1].Value, out ChargingStationOperator_Id chargingStationOperatorId))
                {

                    EVSETemplateId = new EVSETemplate_Id(chargingStationOperatorId,
                                                   matchCollection[0].Groups[2].Value);

                    return true;

                }

            }
            catch
            { }

            EVSETemplateId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this EVSE template identification.
        /// </summary>
        public EVSETemplate_Id Clone()

            => new (
                   OperatorId.Clone(),
                   Suffix.    CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (EVSETemplateId1, EVSETemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSETemplateId1">A EVSE template identification.</param>
        /// <param name="EVSETemplateId2">Another EVSE template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EVSETemplate_Id EVSETemplateId1,
                                           EVSETemplate_Id EVSETemplateId2)

            => EVSETemplateId1.Equals(EVSETemplateId2);

        #endregion

        #region Operator != (EVSETemplateId1, EVSETemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSETemplateId1">A EVSE template identification.</param>
        /// <param name="EVSETemplateId2">Another EVSE template identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EVSETemplate_Id EVSETemplateId1,
                                           EVSETemplate_Id EVSETemplateId2)

            => !EVSETemplateId1.Equals(EVSETemplateId2);

        #endregion

        #region Operator <  (EVSETemplateId1, EVSETemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSETemplateId1">A EVSE template identification.</param>
        /// <param name="EVSETemplateId2">Another EVSE template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EVSETemplate_Id EVSETemplateId1,
                                          EVSETemplate_Id EVSETemplateId2)

            => EVSETemplateId1.CompareTo(EVSETemplateId2) < 0;

        #endregion

        #region Operator <= (EVSETemplateId1, EVSETemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSETemplateId1">A EVSE template identification.</param>
        /// <param name="EVSETemplateId2">Another EVSE template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EVSETemplate_Id EVSETemplateId1,
                                           EVSETemplate_Id EVSETemplateId2)

            => EVSETemplateId1.CompareTo(EVSETemplateId2) <= 0;

        #endregion

        #region Operator >  (EVSETemplateId1, EVSETemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSETemplateId1">A EVSE template identification.</param>
        /// <param name="EVSETemplateId2">Another EVSE template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EVSETemplate_Id EVSETemplateId1,
                                          EVSETemplate_Id EVSETemplateId2)

            => EVSETemplateId1.CompareTo(EVSETemplateId2) > 0;

        #endregion

        #region Operator >= (EVSETemplateId1, EVSETemplateId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSETemplateId1">A EVSE template identification.</param>
        /// <param name="EVSETemplateId2">Another EVSE template identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EVSETemplate_Id EVSETemplateId1,
                                           EVSETemplate_Id EVSETemplateId2)

            => EVSETemplateId1.CompareTo(EVSETemplateId2) >= 0;

        #endregion

        #endregion

        #region IComparable<EVSETemplateId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two EVSE template identifications.
        /// </summary>
        /// <param name="Object">A EVSE template identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EVSETemplate_Id evseTemplateId
                   ? CompareTo(evseTemplateId)
                   : throw new ArgumentException("The given object is not an EVSE template identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EVSETemplateId)

        /// <summary>
        /// Compares two EVSE template identifications.
        /// </summary>
        /// <param name="EVSETemplateId">A EVSE template identification to compare with.</param>
        public Int32 CompareTo(EVSETemplate_Id EVSETemplateId)
        {

            var c = OperatorId.CompareTo(EVSETemplateId.OperatorId);

            if (c == 0)
                c = String.Compare(Suffix,
                                   EVSETemplateId.Suffix,
                                   StringComparison.OrdinalIgnoreCase);

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<EVSETemplateId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EVSE template identifications for equality.
        /// </summary>
        /// <param name="Object">A EVSE template identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVSETemplate_Id evseTemplateId &&
                   Equals(evseTemplateId);

        #endregion

        #region Equals(EVSETemplateId)

        /// <summary>
        /// Compares two EVSE template identifications for equality.
        /// </summary>
        /// <param name="EVSETemplateId">A EVSE template identification to compare with.</param>
        public Boolean Equals(EVSETemplate_Id EVSETemplateId)

            => OperatorId.Equals(EVSETemplateId.OperatorId) &&

               String.Equals(Suffix,
                             EVSETemplateId.Suffix,
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
            => String.Concat(OperatorId, "*ET", Suffix);

        #endregion

    }

}
