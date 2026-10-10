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

using System.Text.RegularExpressions;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A delegate for filtering EVSE identifications.
    /// </summary>
    /// <param name="EVSEId">An EVSE identification to include.</param>
    public delegate Boolean IncludeEVSEIdDelegate(EVSE_Id EVSEId);


    /// <summary>
    /// Extension methods for Electric Vehicle Supply Equipment (EVSE) identifications.
    /// </summary>
    public static class EVSEIdExtensions
    {

        /// <summary>
        /// Indicates whether this EVSE identification is null or empty.
        /// </summary>
        /// <param name="EVSEId">An EVSE identification.</param>
        public static Boolean IsNullOrEmpty(this EVSE_Id? EVSEId)
            => !EVSEId.HasValue || EVSEId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this EVSE identification is null or empty.
        /// </summary>
        /// <param name="EVSEId">An EVSE identification.</param>
        public static Boolean IsNotNullOrEmpty(this EVSE_Id? EVSEId)
            => EVSEId.HasValue && EVSEId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of an Electric Vehicle Supply Equipment (EVSE).
    /// </summary>
    public readonly struct EVSE_Id : IId,
                                     IEquatable<EVSE_Id>,
                                     IComparable<EVSE_Id>

    {

        #region Data

        /// <summary>
        /// The regular expression for strict parsing an EVSE identification.
        /// </summary>
        public static readonly Regex  EVSEId_RegEx       = new (@"^([A-Z]{2}\*?[A-Z0-9]{3})\*?E([A-Z0-9][a-zA-Z0-9\*]{1,30})$",
                                                                RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// The regular expression for parsing an ISO EVSE identification suffix.
        /// </summary>
        public static readonly Regex EVSEIdSuffix_RegEx  = new (@"^([A-Za-z0-9\*]{1,30})$",
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
        public Boolean  IsNullOrEmpty
            => Suffix.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this identification is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => Suffix.IsNotNullOrEmpty();

        /// <summary>
        /// Returns the length of the identification.
        /// </summary>
        public UInt64   Length

            => OperatorId.Length + 2 + (UInt64) Suffix.Length;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new Electric Vehicle Supply Equipment (EVSE) identification
        /// based on the given charging station operator and identification suffix.
        /// </summary>
        /// <param name="OperatorId">The unique identification of a charging station operator.</param>
        /// <param name="Suffix">The suffix of the EVSE identification.</param>
        private EVSE_Id(ChargingStationOperator_Id  OperatorId,
                        String                      Suffix)
        {

            this.OperatorId  = OperatorId;
            this.Suffix      = Suffix;

        }

        #endregion


        #region NewRandom(ChargingStationOperatorId, Length = 12, Mapper = null)

        /// <summary>
        /// Generate a new unique identification of an EVSE.
        /// </summary>
        /// <param name="ChargingStationOperatorId">The unique identification of a charging station operator.</param>
        /// <param name="Length">The expected length of the EVSE identification suffix</param>
        /// <param name="Mapper">A delegate to modify the newly generated EVSE identification.</param>
        public static EVSE_Id NewRandom(ChargingStationOperator_Id  ChargingStationOperatorId,
                                        Byte                        Length  = 12,
                                        Func<String, String>?       Mapper  = null)
        {

            if (Length < 12 || Length > 50)
                Length = 50;

            return Parse(ChargingStationOperatorId,
                         Mapper is not null
                             ? Mapper(RandomExtensions.RandomString(Length))
                             :        RandomExtensions.RandomString(Length));

        }

        #endregion

        #region NewRandom(ChargingPoolId,            Length = 12, Mapper = null)

        /// <summary>
        /// Generate a new unique identification of an EVSE.
        /// </summary>
        /// <param name="ChargingPoolId">The unique identification of a charging pool.</param>
        /// <param name="Length">The expected length of the EVSE identification suffix</param>
        /// <param name="Mapper">A delegate to modify the newly generated EVSE identification.</param>
        public static EVSE_Id NewRandom(ChargingPool_Id        ChargingPoolId,
                                        Byte                   Length  = 12,
                                        Func<String, String>?  Mapper  = null)
        {

            if (Length < 12 || Length > 40)
                Length = 40;

            return Parse(ChargingPoolId,
                         Mapper is not null
                             ? Mapper(RandomExtensions.RandomString(Length))
                             :        RandomExtensions.RandomString(Length));

        }

        #endregion

        #region NewRandom(ChargingStationId,         Length = 12, Mapper = null)

        /// <summary>
        /// Generate a new unique identification of an EVSE.
        /// </summary>
        /// <param name="ChargingStationId">The unique identification of a charging station operator.</param>
        /// <param name="Length">The expected length of the EVSE identification suffix</param>
        /// <param name="Mapper">A delegate to modify the newly generated EVSE identification.</param>
        public static EVSE_Id NewRandom(ChargingStation_Id     ChargingStationId,
                                        Byte                   Length  = 12,
                                        Func<String, String>?  Mapper  = null)
        {

            if (Length < 12 || Length > 30)
                Length = 30;

            return Parse(ChargingStationId,
                         Mapper is not null
                             ? Mapper(RandomExtensions.RandomString(Length))
                             :        RandomExtensions.RandomString(Length));

        }

        #endregion


        #region (static) Parse(Text)

        /// <summary>
        /// Parse the given text representation of an EVSE identification.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE identification.</param>
        public static EVSE_Id Parse(String Text)
        {

            if (TryParse(Text, out var evseId))
                return evseId;

            throw new ArgumentException($"Invalid text representation of an EVSE identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) Parse(ChargingStationOperatorId, Suffix)

        /// <summary>
        /// Parse the given charging station operator identification and suffix as an EVSE identification.
        /// </summary>
        /// <param name="ChargingStationOperatorId">The unique identification of a charging station operator.</param>
        /// <param name="Suffix">The suffix of the charging station identification.</param>
        public static EVSE_Id Parse(ChargingStationOperator_Id  ChargingStationOperatorId,
                                    String                      Suffix)

            => Parse(
                   String.Concat(
                       ChargingStationOperatorId.ToString(),
                       "*E",
                       Suffix
                   )
               );

        #endregion

        #region (static) Parse(ChargingPoolId,            Suffix)

        /// <summary>
        /// Parse the given charging pool identification and suffix as an EVSE identification.
        /// </summary>
        /// <param name="ChargingPoolId">The unique identification of a charging pool.</param>
        /// <param name="Suffix">The suffix of the charging station identification.</param>
        public static EVSE_Id Parse(ChargingPool_Id  ChargingPoolId,
                                    String           Suffix)

            => Parse(
                   String.Concat(
                       ChargingPoolId.OperatorId.ToString(),
                       "*E",
                       ChargingPoolId.Suffix,
                       Suffix.IsNeitherNullNorEmpty()
                           ? "*" + Suffix
                           : ""
                   )
               );

        #endregion

        #region (static) Parse(ChargingStationId,         Suffix)

        /// <summary>
        /// Parse the given charging station identification and suffix as an EVSE identification.
        /// </summary>
        /// <param name="ChargingStationId">The unique identification of a charging station.</param>
        /// <param name="Suffix">The suffix of the charging station identification.</param>
        public static EVSE_Id Parse(ChargingStation_Id  ChargingStationId,
                                    String              Suffix)

            => Parse(
                   String.Concat(
                       ChargingStationId.OperatorId.ToString(),
                       "*E",
                       ChargingStationId.Suffix,
                       Suffix.IsNeitherNullNorEmpty()
                           ? "*" + Suffix
                           : ""
                   )
               );

        #endregion


        #region TryParse(Text, ParsingMode = relaxed)

        /// <summary>
        /// Try to parse the given text representation of an EVSE identification.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE identification.</param>
        public static EVSE_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var evseId))
                return evseId;

            return null;

        }

        #endregion

        #region (static) TryParse(ChargingStationOperatorId, Suffix)

        /// <summary>
        /// Parse the given charging station operator identification and suffix as an EVSE identification.
        /// </summary>
        /// <param name="ChargingStationOperatorId">The unique identification of a charging station operator.</param>
        /// <param name="Suffix">The suffix of the charging station identification.</param>
        public static EVSE_Id? TryParse(ChargingStationOperator_Id  ChargingStationOperatorId,
                                        String                      Suffix)

            => TryParse(
                   String.Concat(
                       ChargingStationOperatorId.ToString(),
                       "*E",
                       Suffix
                   )
               );

        #endregion

        #region (static) TryParse(ChargingPoolId,            Suffix)

        /// <summary>
        /// Parse the given charging pool identification and suffix as an EVSE identification.
        /// </summary>
        /// <param name="ChargingPoolId">The unique identification of a charging pool.</param>
        /// <param name="Suffix">The suffix of the charging station identification.</param>
        public static EVSE_Id? TryParse(ChargingPool_Id  ChargingPoolId,
                                        String           Suffix)

            => TryParse(
                   String.Concat(
                       ChargingPoolId.OperatorId.ToString(),
                       "*E",
                       ChargingPoolId.Suffix,
                       Suffix.IsNeitherNullNorEmpty()
                           ? "*" + Suffix
                           : ""
                   )
               );

        #endregion

        #region (static) TryParse(ChargingStationId,         Suffix)

        /// <summary>
        /// Parse the given charging station identification and suffix as an EVSE identification.
        /// </summary>
        /// <param name="ChargingStationId">The unique identification of a charging station.</param>
        /// <param name="Suffix">The suffix of the charging station identification.</param>
        public static EVSE_Id? TryParse(ChargingStation_Id  ChargingStationId,
                                        String              Suffix)

            => TryParse(
                   String.Concat(
                       ChargingStationId.OperatorId.ToString(),
                       "*E",
                       ChargingStationId.Suffix,
                       Suffix.IsNeitherNullNorEmpty()
                           ? "*" + Suffix
                           : ""
                   )
               );

        #endregion


        #region (static) TryParse(Text, out EVSEId)

        /// <summary>
        /// Try to parse the given text representation of an EVSE identification.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE identification.</param>
        /// <param name="EVSEId">The parsed EVSE identification.</param>
        public static Boolean TryParse(String       Text,
                                       out EVSE_Id  EVSEId)
        {

            #region Initial checks

            EVSEId = default;

            if (Text is null)
                return false;

            Text = Text.Trim();

            if (Text.IsNullOrEmpty())
                return false;

            #endregion

            try
            {

                var matchCollection = EVSEId_RegEx.Matches(Text);

                if (matchCollection.Count != 1)
                    return false;

                if (ChargingStationOperator_Id.TryParse(matchCollection[0].Groups[1].Value,
                                                        out var chargingStationOperatorId))
                {

                    EVSEId = new EVSE_Id(
                                 chargingStationOperatorId,
                                 matchCollection[0].Groups[2].Value
                             );

                    return true;

                }

            }
            catch
            { }

            return false;

        }

        #endregion

        #region (static) TryParse(ChargingStationOperatorId, Suffix, out EVSEId)

        /// <summary>
        /// Parse the given charging station operator identification and suffix as a charging station identification.
        /// </summary>
        /// <param name="ChargingStationOperatorId">The unique identification of a charging station operator.</param>
        /// <param name="Suffix">The suffix of the charging station identification.</param>
        public static Boolean TryParse(ChargingStationOperator_Id  ChargingStationOperatorId,
                                       String                      Suffix,
                                       out EVSE_Id                 EVSEId)

            => TryParse(
                   String.Concat(
                       ChargingStationOperatorId.ToString(),
                       "*E",
                       Suffix
                   ),
                   out EVSEId
               );

        #endregion

        #region (static) TryParse(ChargingPoolId,            Suffix, out EVSEId)

        /// <summary>
        /// Parse the given charging pool identification and suffix as a charging station identification.
        /// </summary>
        /// <param name="ChargingPoolId">The unique identification of a charging pool.</param>
        /// <param name="Suffix">The suffix of the charging station identification.</param>
        public static Boolean TryParse(ChargingPool_Id  ChargingPoolId,
                                       String           Suffix,
                                       out EVSE_Id      EVSEId)

            => TryParse(
                   String.Concat(
                       ChargingPoolId.OperatorId.ToString(),
                       "*E",
                       ChargingPoolId.Suffix,
                       Suffix.IsNeitherNullNorEmpty()
                           ? "*" + Suffix
                           : ""
                   ),
                   out EVSEId
               );

        #endregion

        #region (static) TryParse(ChargingStationId,         Suffix, out EVSEId)

        /// <summary>
        /// Parse the given charging station identification and suffix as a charging station identification.
        /// </summary>
        /// <param name="ChargingStationId">The unique identification of a charging station.</param>
        /// <param name="Suffix">The suffix of the charging station identification.</param>
        public static Boolean TryParse(ChargingStation_Id  ChargingStationId,
                                       String              Suffix,
                                       out EVSE_Id         EVSEId)

            => TryParse(
                   String.Concat(
                       ChargingStationId.OperatorId.ToString(),
                       "*E",
                       ChargingStationId.Suffix,
                       Suffix.IsNeitherNullNorEmpty()
                           ? "*" + Suffix
                           : ""
                   ),
                   out EVSEId
               );

        #endregion


        #region Clone()

        /// <summary>
        /// Clone this EVSE identification.
        /// </summary>
        public EVSE_Id Clone()

            => new (
                   OperatorId.Clone(),
                   Suffix.    CloneString()
               );

        #endregion


        #region Replace(Old, New)

        /// <summary>
        /// Returns a new EVSE Id in which all occurrences of the specified
        /// old string value are replaced with the new value.
        /// </summary>
        /// <param name="OldValue">The string to be replaced.</param>
        /// <param name="NewValue">The new string value.</param>
        public EVSE_Id Replace(String  OldValue,
                               String  NewValue)

            => Parse(ToString().Replace(OldValue, NewValue));

        #endregion


        #region Operator overloading

        #region Operator == (EVSEId1, EVSEId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEId1">An EVSE identification.</param>
        /// <param name="EVSEId2">Another EVSE identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EVSE_Id EVSEId1,
                                           EVSE_Id EVSEId2)

            => EVSEId1.Equals(EVSEId2);

        #endregion

        #region Operator != (EVSEId1, EVSEId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEId1">An EVSE identification.</param>
        /// <param name="EVSEId2">Another EVSE identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EVSE_Id EVSEId1,
                                           EVSE_Id EVSEId2)

            => !EVSEId1.Equals(EVSEId2);

        #endregion

        #region Operator <  (EVSEId1, EVSEId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEId1">An EVSE identification.</param>
        /// <param name="EVSEId2">Another EVSE identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EVSE_Id EVSEId1,
                                          EVSE_Id EVSEId2)

            => EVSEId1.CompareTo(EVSEId2) < 0;

        #endregion

        #region Operator <= (EVSEId1, EVSEId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEId1">An EVSE identification.</param>
        /// <param name="EVSEId2">Another EVSE identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EVSE_Id EVSEId1,
                                           EVSE_Id EVSEId2)

            => EVSEId1.CompareTo(EVSEId2) <= 0;

        #endregion

        #region Operator >  (EVSEId1, EVSEId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEId1">An EVSE identification.</param>
        /// <param name="EVSEId2">Another EVSE identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EVSE_Id EVSEId1,
                                          EVSE_Id EVSEId2)

            => EVSEId1.CompareTo(EVSEId2) > 0;

        #endregion

        #region Operator >= (EVSEId1, EVSEId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEId1">An EVSE identification.</param>
        /// <param name="EVSEId2">Another EVSE identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EVSE_Id EVSEId1,
                                           EVSE_Id EVSEId2)

            => EVSEId1.CompareTo(EVSEId2) >= 0;

        #endregion

        #endregion

        #region IComparable<EVSEId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two EVSE identifications.
        /// </summary>
        /// <param name="Object">An EVSE identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EVSE_Id evseId
                   ? CompareTo(evseId)
                   : throw new ArgumentException("The given object is not an EVSE identification!", nameof(Object));

        #endregion

        #region CompareTo(EVSEId)

        /// <summary>
        /// Compares two EVSE identifications.
        /// </summary>
        /// <param name="EVSEId">An EVSE identification to compare with.</param>
        public Int32 CompareTo(EVSE_Id EVSEId)
        {

            var c = OperatorId.CompareTo(EVSEId.OperatorId);

            if (c == 0)
                c = String.Compare(Suffix.       Replace("*", ""),
                                   EVSEId.Suffix.Replace("*", ""),
                                   StringComparison.OrdinalIgnoreCase);

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<EVSEId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EVSE identifications for equality.
        /// </summary>
        /// <param name="Object">An EVSE identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVSE_Id evseId &&
                   Equals(evseId);

        #endregion

        #region Equals(EVSEId)

        /// <summary>
        /// Compares two EVSE identifications for equality.
        /// </summary>
        /// <param name="EVSEId">An EVSE identification to compare with.</param>
        public Boolean Equals(EVSE_Id EVSEId)

            => OperatorId.Equals(EVSEId.OperatorId) &&

               String.Equals(Suffix.       Replace("*", ""),
                             EVSEId.Suffix.Replace("*", ""),
                             StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => OperatorId.GetHashCode() ^
              (Suffix is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Suffix.Replace("*", "")));

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// ISO-IEC-15118 – Annex H "Specification of Identifiers"
        /// </summary>
        public override String ToString()

            => String.Concat(
                   OperatorId,
                   "*E",
                   Suffix
               );

        #endregion

    }

}
