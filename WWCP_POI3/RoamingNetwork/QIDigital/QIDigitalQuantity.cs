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

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Whether a measured value conforms to its tolerance limits, as a PTB DCC states it (dcc:conformity).
    /// </summary>
    public enum QIDigitalConformity
    {

        /// <summary>
        /// Within its tolerance limits, its uncertainty included.
        /// </summary>
        Pass,

        /// <summary>
        /// Outside its tolerance limits, its uncertainty included.
        /// </summary>
        Fail,

        /// <summary>
        /// Within its tolerance limits, but not with all of its uncertainty.
        /// </summary>
        ConditionalPass,

        /// <summary>
        /// Outside its tolerance limits, but not with all of its uncertainty.
        /// </summary>
        ConditionalFail,

        /// <summary>
        /// No pass can be stated.
        /// </summary>
        NoPass,

        /// <summary>
        /// No fail can be stated.
        /// </summary>
        NoFail

    }


    /// <summary>
    /// A quantity of a QI-Digital document: a value in a unit with its expanded uncertainty, as a PTB DCC gives it in the
    /// D-SI format (si:real with si:expandedUnc), named by the DCC refType of what it is, e.g. basic_measurementError, and
    /// with the tolerance limits and the conformity a DCC states in its measurement metadata.
    /// </summary>
    /// <remarks>
    /// Its unit is written as D-SI writes units, e.g. \percent, \ampere, \volt, \watt, \kilo\watt\hour or \degreecelsius.
    /// </remarks>
    public sealed class QIDigitalQuantity
    {

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its DCC refType.
            /// </summary>
            public const String  RefType              = "refType";

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name                 = "name";

            /// <summary>
            /// The property of its value.
            /// </summary>
            public const String  Value                = "value";

            /// <summary>
            /// The property of its D-SI unit.
            /// </summary>
            public const String  Unit                 = "unit";

            /// <summary>
            /// The property of its expanded uncertainty.
            /// </summary>
            public const String  ExpandedUncertainty  = "expandedUncertainty";

            /// <summary>
            /// The property of the coverage factor of its expanded uncertainty.
            /// </summary>
            public const String  CoverageFactor       = "coverageFactor";

            /// <summary>
            /// The property of the coverage probability of its expanded uncertainty.
            /// </summary>
            public const String  CoverageProbability  = "coverageProbability";

            /// <summary>
            /// The property of its lower tolerance limit.
            /// </summary>
            public const String  ToleranceLower       = "toleranceLower";

            /// <summary>
            /// The property of its upper tolerance limit.
            /// </summary>
            public const String  ToleranceUpper       = "toleranceUpper";

            /// <summary>
            /// The property of its conformity.
            /// </summary>
            public const String  Conformity           = "conformity";

        }

        #endregion


        /// <summary>
        /// What it is, as a DCC names it by its refType, e.g. basic_nominalValue, basic_measuredValue or
        /// basic_measurementError.
        /// </summary>
        public String?                RefType                { get; }

        /// <summary>
        /// Its multilingual name.
        /// </summary>
        public ImmutableI18NString?   Name                   { get; }

        /// <summary>
        /// Its value, without trailing zeros.
        /// </summary>
        public Decimal                Value                  { get; }

        /// <summary>
        /// Its unit, as D-SI writes it, e.g. \percent.
        /// </summary>
        public String                 Unit                   { get; }

        /// <summary>
        /// Its expanded uncertainty, in its unit.
        /// </summary>
        public Decimal?               ExpandedUncertainty    { get; }

        /// <summary>
        /// The coverage factor of its expanded uncertainty, e.g. 2.
        /// </summary>
        public Decimal?               CoverageFactor         { get; }

        /// <summary>
        /// The coverage probability of its expanded uncertainty, e.g. 0.95.
        /// </summary>
        public Decimal?               CoverageProbability    { get; }

        /// <summary>
        /// Its lower tolerance limit, e.g. the negative maximum permissible error.
        /// </summary>
        public Decimal?               ToleranceLower         { get; }

        /// <summary>
        /// Its upper tolerance limit, e.g. the maximum permissible error.
        /// </summary>
        public Decimal?               ToleranceUpper         { get; }

        /// <summary>
        /// Whether it conforms to its tolerance limits.
        /// </summary>
        public QIDigitalConformity?   Conformity             { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new quantity.
        /// </summary>
        /// <param name="Value">Its value.</param>
        /// <param name="Unit">Its unit, as D-SI writes it, e.g. \percent.</param>
        /// <param name="RefType">What it is, as a DCC names it by its refType.</param>
        /// <param name="Name">Its multilingual name.</param>
        /// <param name="ExpandedUncertainty">Its expanded uncertainty, in its unit.</param>
        /// <param name="CoverageFactor">The coverage factor of its expanded uncertainty.</param>
        /// <param name="CoverageProbability">The coverage probability of its expanded uncertainty.</param>
        /// <param name="ToleranceLower">Its lower tolerance limit.</param>
        /// <param name="ToleranceUpper">Its upper tolerance limit.</param>
        /// <param name="Conformity">Whether it conforms to its tolerance limits.</param>
        public QIDigitalQuantity(Decimal                Value,
                                 String                 Unit,
                                 String?                RefType               = null,
                                 ImmutableI18NString?   Name                  = null,
                                 Decimal?               ExpandedUncertainty   = null,
                                 Decimal?               CoverageFactor        = null,
                                 Decimal?               CoverageProbability   = null,
                                 Decimal?               ToleranceLower        = null,
                                 Decimal?               ToleranceUpper        = null,
                                 QIDigitalConformity?   Conformity            = null)
        {

            if (String.IsNullOrWhiteSpace(Unit) || Unit != Unit.Trim())
                throw new ArgumentException("The unit of a quantity must neither be empty nor begin or end with whitespace!", nameof(Unit));

            if (RefType is not null && (String.IsNullOrWhiteSpace(RefType) || RefType.Any(Char.IsWhiteSpace)))
                throw new ArgumentException("The refType of a quantity must not be empty nor contain whitespace!", nameof(RefType));

            if (ExpandedUncertainty < 0)
                throw new ArgumentException("The expanded uncertainty of a quantity must not be negative!", nameof(ExpandedUncertainty));

            if ((CoverageFactor.HasValue || CoverageProbability.HasValue) && !ExpandedUncertainty.HasValue)
                throw new ArgumentException("A coverage factor or probability is one of an expanded uncertainty!", nameof(ExpandedUncertainty));

            if (CoverageFactor <= 0)
                throw new ArgumentException("The coverage factor of a quantity must be positive!", nameof(CoverageFactor));

            if (CoverageProbability <= 0 || CoverageProbability > 1)
                throw new ArgumentException("The coverage probability of a quantity must be greater than 0 and at most 1!", nameof(CoverageProbability));

            if (ToleranceLower > ToleranceUpper)
                throw new ArgumentException("The lower tolerance limit of a quantity must not exceed its upper one!", nameof(ToleranceLower));

            this.Value                = QIDigitalJSON.Number(Value);
            this.Unit                 = Unit;
            this.RefType              = RefType;
            this.Name                 = Name;
            this.ExpandedUncertainty  = QIDigitalJSON.Number(ExpandedUncertainty);
            this.CoverageFactor       = QIDigitalJSON.Number(CoverageFactor);
            this.CoverageProbability  = QIDigitalJSON.Number(CoverageProbability);
            this.ToleranceLower       = QIDigitalJSON.Number(ToleranceLower);
            this.ToleranceUpper       = QIDigitalJSON.Number(ToleranceUpper);
            this.Conformity           = Conformity;

        }

        #endregion


        #region IsWithinTolerance

        /// <summary>
        /// Whether its value is within its tolerance limits, its expanded uncertainty included: null without limits.
        /// </summary>
        public Boolean? IsWithinTolerance

            => !ToleranceLower.HasValue && !ToleranceUpper.HasValue
                   ? null
                   : (!ToleranceLower.HasValue || Value - (ExpandedUncertainty ?? 0) >= ToleranceLower.Value) &&
                     (!ToleranceUpper.HasValue || Value + (ExpandedUncertainty ?? 0) <= ToleranceUpper.Value);

        #endregion


        #region (static) TryParse(JSON, out Quantity, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a quantity.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Quantity">The parsed quantity.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out QIDigitalQuantity?  Quantity,
                                       [NotNullWhen(false)] out String?             ErrorResponse)
        {

            Quantity = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "a quantity", out ErrorResponse,
                                           PropertyKeys.RefType, PropertyKeys.Name, PropertyKeys.Value, PropertyKeys.Unit,
                                           PropertyKeys.ExpandedUncertainty, PropertyKeys.CoverageFactor, PropertyKeys.CoverageProbability,
                                           PropertyKeys.ToleranceLower, PropertyKeys.ToleranceUpper, PropertyKeys.Conformity))
                {
                    return false;
                }

                if (!QIDigitalJSON.TryParseDecimal(JSON, PropertyKeys.Value,                "value of a quantity",   out var value,                out ErrorResponse) ||
                    !QIDigitalJSON.TryParseDecimal(JSON, PropertyKeys.ExpandedUncertainty,  "expanded uncertainty",  out var expandedUncertainty,  out ErrorResponse) ||
                    !QIDigitalJSON.TryParseDecimal(JSON, PropertyKeys.CoverageFactor,       "coverage factor",       out var coverageFactor,       out ErrorResponse) ||
                    !QIDigitalJSON.TryParseDecimal(JSON, PropertyKeys.CoverageProbability,  "coverage probability",  out var coverageProbability,  out ErrorResponse) ||
                    !QIDigitalJSON.TryParseDecimal(JSON, PropertyKeys.ToleranceLower,       "lower tolerance limit", out var toleranceLower,       out ErrorResponse) ||
                    !QIDigitalJSON.TryParseDecimal(JSON, PropertyKeys.ToleranceUpper,       "upper tolerance limit", out var toleranceUpper,       out ErrorResponse) ||
                    !POIJSON.TryParseString       (JSON, PropertyKeys.RefType,              "refType",               out var refType,              out ErrorResponse) ||
                    !POIJSON.TryParseI18N         (JSON, PropertyKeys.Name,                 "name of a quantity",    out var name,                 out ErrorResponse) ||
                    !POIJSON.TryParseEnum<QIDigitalConformity>(JSON, PropertyKeys.Conformity, "conformity",          out var conformity,           out ErrorResponse))
                {
                    return false;
                }

                if (!value.HasValue || QIDigitalJSON.Text(JSON, PropertyKeys.Unit) is not String unit)
                {
                    ErrorResponse = "A quantity requires a value and a unit!";
                    return false;
                }

                Quantity       = new QIDigitalQuantity(value.Value,
                                                       unit,
                                                       refType,
                                                       name,
                                                       expandedUncertainty,
                                                       coverageFactor,
                                                       coverageProbability,
                                                       toleranceLower,
                                                       toleranceUpper,
                                                       conformity);

                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                Quantity       = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this quantity.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject();

            if (RefType is not null)
                json.Add(PropertyKeys.RefType,              RefType);

            if (Name.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Name,                 Name.ToJSON());

            json.Add(PropertyKeys.Value,                    POIJSON.Number(Value));
            json.Add(PropertyKeys.Unit,                     Unit);

            if (ExpandedUncertainty.HasValue)
                json.Add(PropertyKeys.ExpandedUncertainty,  POIJSON.Number(ExpandedUncertainty.Value));

            if (CoverageFactor.HasValue)
                json.Add(PropertyKeys.CoverageFactor,       POIJSON.Number(CoverageFactor.Value));

            if (CoverageProbability.HasValue)
                json.Add(PropertyKeys.CoverageProbability,  POIJSON.Number(CoverageProbability.Value));

            if (ToleranceLower.HasValue)
                json.Add(PropertyKeys.ToleranceLower,       POIJSON.Number(ToleranceLower.Value));

            if (ToleranceUpper.HasValue)
                json.Add(PropertyKeys.ToleranceUpper,       POIJSON.Number(ToleranceUpper.Value));

            if (Conformity.HasValue)
                json.Add(PropertyKeys.Conformity,           Conformity.Value.ToString());

            return json;

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Value} {Unit}{(ExpandedUncertainty.HasValue ? $" ± {ExpandedUncertainty} (k = {CoverageFactor?.ToString() ?? "?"})" : "")}{(Conformity.HasValue ? $", {Conformity}" : "")}";

        #endregion

    }

}
