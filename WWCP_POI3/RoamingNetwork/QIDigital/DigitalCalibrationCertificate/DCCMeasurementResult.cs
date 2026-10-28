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

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    #region DCCPerformanceLocation

    /// <summary>
    /// Where a calibration was performed (dcc:performanceLocation).
    /// </summary>
    public enum DCCPerformanceLocation
    {

        /// <summary>
        /// In the calibration laboratory.
        /// </summary>
        Laboratory,

        /// <summary>
        /// At the customer, e.g. at the charging station in the field.
        /// </summary>
        Customer,

        /// <summary>
        /// In a branch of the calibration laboratory.
        /// </summary>
        LaboratoryBranch,

        /// <summary>
        /// In a branch of the customer.
        /// </summary>
        CustomerBranch,

        /// <summary>
        /// Elsewhere.
        /// </summary>
        Other

    }

    #endregion

    #region DCCResult

    /// <summary>
    /// A result of a measurement of a digital calibration certificate (dcc:result): the quantities of one calibration
    /// point, e.g. the nominal current and the measurement error of an energy meter at it.
    /// </summary>
    public sealed class DCCResult
    {

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name        = "name";

            /// <summary>
            /// The property of its DCC refType.
            /// </summary>
            public const String  RefType     = "refType";

            /// <summary>
            /// The property of its quantities.
            /// </summary>
            public const String  Quantities  = "quantities";

        }

        #endregion

        #region Properties

        /// <summary>
        /// Its multilingual name, e.g. "Imax, cos φ = 1".
        /// </summary>
        public ImmutableI18NString                  Name          { get; }

        /// <summary>
        /// What it is, as a DCC names it by its refType.
        /// </summary>
        public String?                              RefType       { get; }

        /// <summary>
        /// Its quantities, at least one, in their given order.
        /// </summary>
        public ImmutableArray<QIDigitalQuantity>    Quantities    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new result of a measurement.
        /// </summary>
        /// <param name="Name">Its multilingual name.</param>
        /// <param name="Quantities">Its quantities, at least one.</param>
        /// <param name="RefType">What it is, as a DCC names it by its refType.</param>
        public DCCResult(ImmutableI18NString             Name,
                         IEnumerable<QIDigitalQuantity>  Quantities,
                         String?                         RefType   = null)
        {

            if (Name.IsNullOrEmpty())
                throw new ArgumentException("A result of a measurement requires a name!", nameof(Name));

            var quantities = (Quantities ?? []).ToImmutableArray();

            if (quantities.IsEmpty)
                throw new ArgumentException($"The result '{Name.FirstText()}' requires at least one quantity!", nameof(Quantities));

            this.Name        = Name;
            this.RefType     = RefType;
            this.Quantities  = quantities;

        }

        #endregion


        #region (static) TryParse(JSON, out Result, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a result of a measurement.
        /// </summary>
        public static Boolean TryParse(JObject                              JSON,
                                       [NotNullWhen(true)]  out DCCResult?  Result,
                                       [NotNullWhen(false)] out String?     ErrorResponse)
        {

            Result = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "a result", out ErrorResponse, PropertyKeys.Name, PropertyKeys.RefType, PropertyKeys.Quantities) ||
                    !QIDigitalJSON.TryParseI18N                 (JSON, PropertyKeys.Name,       "name of a result",  out var name,                                    out ErrorResponse) ||
                    !POIJSON.TryParseString                     (JSON, PropertyKeys.RefType,    "refType",           out var refType,                                 out ErrorResponse) ||
                    !POIJSON.TryParseObjects<QIDigitalQuantity> (JSON, PropertyKeys.Quantities, "quantity",          QIDigitalQuantity.TryParse, out var quantities,  out ErrorResponse))
                {
                    return false;
                }

                Result         = new DCCResult(name!, quantities, refType);
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                Result         = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this result.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(new JProperty(PropertyKeys.Name, Name.ToJSON()));

            if (RefType is not null)
                json.Add(PropertyKeys.RefType, RefType);

            json.Add(PropertyKeys.Quantities, new JArray(Quantities.Select(quantity => quantity.ToJSON())));

            return json;

        }

        #endregion

        /// <inheritdoc/>
        public override String ToString()
            => $"{Name.FirstText()}: {String.Join(", ", Quantities)}";

    }

    #endregion

    #region DCCMeasurementResult

    /// <summary>
    /// A measurement of a digital calibration certificate (dcc:measurementResult): the methods it used, the conditions
    /// it was performed in, e.g. the ambient temperature, and its results.
    /// </summary>
    public sealed class DCCMeasurementResult
    {

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name                 = "name";

            /// <summary>
            /// The property of the methods it used.
            /// </summary>
            public const String  UsedMethods          = "usedMethods";

            /// <summary>
            /// The property of the conditions it was performed in.
            /// </summary>
            public const String  InfluenceConditions  = "influenceConditions";

            /// <summary>
            /// The property of its results.
            /// </summary>
            public const String  Results              = "results";

        }

        #endregion

        #region Properties

        /// <summary>
        /// Its multilingual name, e.g. "Measurement error of the active energy".
        /// </summary>
        public ImmutableI18NString                  Name                   { get; }

        /// <summary>
        /// The methods it used, each with the standard it follows.
        /// </summary>
        public ImmutableArray<QIDigitalStandard>    UsedMethods            { get; }

        /// <summary>
        /// The conditions it was performed in, e.g. the ambient temperature and the frequency.
        /// </summary>
        public ImmutableArray<QIDigitalQuantity>    InfluenceConditions    { get; }

        /// <summary>
        /// Its results, at least one, in their given order.
        /// </summary>
        public ImmutableArray<DCCResult>            Results                { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new measurement of a digital calibration certificate.
        /// </summary>
        /// <param name="Name">Its multilingual name.</param>
        /// <param name="Results">Its results, at least one.</param>
        /// <param name="UsedMethods">The methods it used.</param>
        /// <param name="InfluenceConditions">The conditions it was performed in.</param>
        public DCCMeasurementResult(ImmutableI18NString              Name,
                                    IEnumerable<DCCResult>           Results,
                                    IEnumerable<QIDigitalStandard>?  UsedMethods           = null,
                                    IEnumerable<QIDigitalQuantity>?  InfluenceConditions   = null)
        {

            if (Name.IsNullOrEmpty())
                throw new ArgumentException("A measurement requires a name!", nameof(Name));

            var results = (Results ?? []).ToImmutableArray();

            if (results.IsEmpty)
                throw new ArgumentException($"The measurement '{Name.FirstText()}' requires at least one result!", nameof(Results));

            this.Name                 = Name;
            this.Results              = results;
            this.UsedMethods          = (UsedMethods         ?? []).ToImmutableArray();
            this.InfluenceConditions  = (InfluenceConditions ?? []).ToImmutableArray();

        }

        #endregion


        #region Quantities

        /// <summary>
        /// All quantities of its results, in their order.
        /// </summary>
        public IEnumerable<QIDigitalQuantity> Quantities
            => Results.SelectMany(result => result.Quantities);

        #endregion


        #region (static) TryParse(JSON, out MeasurementResult, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a measurement of a digital calibration certificate.
        /// </summary>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out DCCMeasurementResult?  MeasurementResult,
                                       [NotNullWhen(false)] out String?                ErrorResponse)
        {

            MeasurementResult = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "a measurement", out ErrorResponse,
                                           PropertyKeys.Name, PropertyKeys.UsedMethods, PropertyKeys.InfluenceConditions, PropertyKeys.Results) ||
                    !QIDigitalJSON.TryParseI18N                 (JSON, PropertyKeys.Name,                "name of a measurement", out var name,                                              out ErrorResponse) ||
                    !POIJSON.TryParseObjects<QIDigitalStandard> (JSON, PropertyKeys.UsedMethods,         "used method",           QIDigitalStandard.TryParse, out var usedMethods,           out ErrorResponse) ||
                    !POIJSON.TryParseObjects<QIDigitalQuantity> (JSON, PropertyKeys.InfluenceConditions, "influence condition",   QIDigitalQuantity.TryParse, out var influenceConditions,   out ErrorResponse) ||
                    !POIJSON.TryParseObjects<DCCResult>         (JSON, PropertyKeys.Results,             "result",                DCCResult.        TryParse, out var results,               out ErrorResponse))
                {
                    return false;
                }

                MeasurementResult  = new DCCMeasurementResult(name!, results, usedMethods, influenceConditions);
                ErrorResponse      = null;
                return true;

            }
            catch (Exception e)
            {
                MeasurementResult  = null;
                ErrorResponse      = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this measurement.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(new JProperty(PropertyKeys.Name, Name.ToJSON()));

            if (UsedMethods.Length > 0)
                json.Add(PropertyKeys.UsedMethods,          new JArray(UsedMethods.        Select(usedMethod => usedMethod.ToJSON())));

            if (InfluenceConditions.Length > 0)
                json.Add(PropertyKeys.InfluenceConditions,  new JArray(InfluenceConditions.Select(condition  => condition. ToJSON())));

            json.Add(PropertyKeys.Results,                  new JArray(Results.            Select(result     => result.    ToJSON())));

            return json;

        }

        #endregion

        /// <inheritdoc/>
        public override String ToString()
            => $"{Name.FirstText()}: {Results.Length} result(s)";

    }

    #endregion

    #region DCCStatement

    /// <summary>
    /// A statement of a digital calibration certificate (dcc:statement), e.g. that it follows a decision rule, that the
    /// item conforms to a standard, or when it is to be recalibrated.
    /// </summary>
    /// <param name="Declaration">What it states, multilingual.</param>
    /// <param name="RefType">What it is, as a DCC names it by its refType, e.g. basic_conformity.</param>
    /// <param name="Norm">The standard it refers to, e.g. "EN 50470-3:2022".</param>
    /// <param name="Conformity">Whether the item conforms to it.</param>
    public sealed record DCCStatement(ImmutableI18NString   Declaration,
                                      String?               RefType      = null,
                                      String?               Norm         = null,
                                      QIDigitalConformity?  Conformity   = null)
    {

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of what it states.
            /// </summary>
            public const String  Declaration  = "declaration";

            /// <summary>
            /// The property of its DCC refType.
            /// </summary>
            public const String  RefType      = "refType";

            /// <summary>
            /// The property of the standard it refers to.
            /// </summary>
            public const String  Norm         = "norm";

            /// <summary>
            /// The property of its conformity.
            /// </summary>
            public const String  Conformity   = "conformity";

        }

        #endregion

        /// <summary>
        /// What it states, multilingual.
        /// </summary>
        public ImmutableI18NString Declaration { get; }

            = Declaration.IsNotNullOrEmpty()
                  ? Declaration
                  : throw new ArgumentException("A statement requires its declaration!", nameof(Declaration));


        #region (static) TryParse(JSON, out Statement, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a statement.
        /// </summary>
        public static Boolean TryParse(JObject                                 JSON,
                                       [NotNullWhen(true)]  out DCCStatement?  Statement,
                                       [NotNullWhen(false)] out String?        ErrorResponse)
        {

            Statement = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "a statement", out ErrorResponse,
                                           PropertyKeys.Declaration, PropertyKeys.RefType, PropertyKeys.Norm, PropertyKeys.Conformity) ||
                    !QIDigitalJSON.TryParseI18N               (JSON, PropertyKeys.Declaration, "declaration of a statement", out var declaration, out ErrorResponse) ||
                    !POIJSON.TryParseString                   (JSON, PropertyKeys.RefType,     "refType",                    out var refType,     out ErrorResponse) ||
                    !POIJSON.TryParseString                   (JSON, PropertyKeys.Norm,        "norm of a statement",        out var norm,        out ErrorResponse) ||
                    !POIJSON.TryParseEnum<QIDigitalConformity>(JSON, PropertyKeys.Conformity,  "conformity",                 out var conformity,  out ErrorResponse))
                {
                    return false;
                }

                Statement      = new DCCStatement(declaration!, refType, norm, conformity);
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                Statement      = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this statement.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(new JProperty(PropertyKeys.Declaration, Declaration.ToJSON()));

            if (RefType is not null)
                json.Add(PropertyKeys.RefType,     RefType);

            if (Norm is not null)
                json.Add(PropertyKeys.Norm,        Norm);

            if (Conformity.HasValue)
                json.Add(PropertyKeys.Conformity,  Conformity.Value.ToString());

            return json;

        }

        #endregion

        /// <inheritdoc/>
        public override String ToString()
            => Declaration.FirstText();

    }

    #endregion

}
