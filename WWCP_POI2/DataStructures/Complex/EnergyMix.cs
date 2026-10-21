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

    /// <summary>
    /// An energy mix.
    /// </summary>
    public sealed partial class EnergyMix : IEquatable<EnergyMix>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/energyMix";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context                = "@context";

            /// <summary>
            /// The property of its energy sources.
            /// </summary>
            public const String  EnergySources          = "energySources";

            /// <summary>
            /// The property of its environmental impacts.
            /// </summary>
            public const String  EnvironmentalImpacts   = "environmentalImpacts";

            /// <summary>
            /// The property of its supplier name.
            /// </summary>
            public const String  SupplierName           = "supplierName";

            /// <summary>
            /// The property of its product name.
            /// </summary>
            public const String  ProductName            = "productName";

            /// <summary>
            /// The property of its additional remarks.
            /// </summary>
            public const String  AdditionalRemarks      = "additionalRemarks";

        }

        #endregion

        /// <summary>
        /// The energy sources.
        /// </summary>
        public IEnumerable<PercentageOf<EnergySourceCategory>>  EnergySources           { get; }

        /// <summary>
        /// The environmental impacts per kWh, e.g. 350 g of carbon dioxide.
        /// </summary>
        public IEnumerable<EnvironmentalImpactAmount>           EnvironmentalImpacts    { get; }

        /// <summary>
        /// The name or brand of the energy supplier.
        /// </summary>
        public ImmutableI18NString                              SupplierName            { get; }

        /// <summary>
        /// The name or brand of the energy product.
        /// </summary>
        public ImmutableI18NString                              ProductName             { get; }

        /// <summary>
        /// Optional additional remarks.
        /// </summary>
        public ImmutableI18NString?                             AdditionalRemarks       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy mix.
        /// </summary>
        /// <param name="EnergySources">The energy sources.</param>
        /// <param name="EnvironmentalImpacts">The environmental impacts per kWh, e.g. 350 g of carbon dioxide.</param>
        /// <param name="SupplierName">The name or brand of the energy supplier.</param>
        /// <param name="ProductName">The name or brand of the energy product.</param>
        /// <param name="AdditionalRemarks">Optional additional remarks.</param>
        public EnergyMix(IEnumerable<PercentageOf<EnergySourceCategory>>  EnergySources,
                         IEnumerable<EnvironmentalImpactAmount>           EnvironmentalImpacts,
                         ImmutableI18NString                              SupplierName,
                         ImmutableI18NString                              ProductName,
                         ImmutableI18NString?                             AdditionalRemarks   = null)
        {

            var sources  = EnergySources.       ToImmutableArray();
            var impacts  = EnvironmentalImpacts.ToImmutableArray();

            if (sources.Any(item => item.Value.IsNullOrEmpty || !float.IsFinite(item.Percent) || item.Percent < 0 || item.Percent > 100))
                throw new ArgumentException("Energy mix entries require a category and a percentage between zero and 100.");

            // E.g. a default one: amounts are not negative by construction.
            if (impacts.Any(item => item.Category.IsNullOrEmpty))
                throw new ArgumentException("Environmental impacts require a category.");

            if (sources.Select(item => item.Value).Distinct().Count() != sources.Length || impacts.Select(item => item.Category).Distinct().Count() != impacts.Length)
                throw new ArgumentException("Energy mix categories must be unique.");

            this.EnergySources         = sources;
            this.EnvironmentalImpacts  = impacts;
            this.SupplierName          = SupplierName;
            this.ProductName           = ProductName;
            this.AdditionalRemarks     = AdditionalRemarks;

            unchecked
            {

                hashCode = this.EnergySources.       CalcHashCode() * 11 ^
                           this.EnvironmentalImpacts.CalcHashCode() *  7 ^
                           this.SupplierName.        GetHashCode()  *  5 ^
                           this.ProductName.         GetHashCode()  *  3 ^
                          (this.AdditionalRemarks?.  GetHashCode() ?? 0);

            }

        }

        #endregion


        #region Parse/TryParse

        /// <summary>
        /// Parse the given JSON representation of an energy mix.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        public static EnergyMix Parse(JObject JSON)
        {

            if (TryParse(JSON, out var energyMix, out var errorResponse))
                return energyMix;

            throw new ArgumentException("The given JSON representation of an energy mix is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        /// <summary>
        /// Try to parse the given JSON representation of an energy mix.
        /// The composition arrays are mandatory; [] stands for an unknown composition.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EnergyMix">The parsed energy mix.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                  JSON,
                                       [NotNullWhen(true)]  out EnergyMix?      EnergyMix,
                                       [NotNullWhen(false)] out String?         ErrorResponse,
                                       CustomJObjectParserDelegate<EnergyMix>?  CustomEnergyMixParser   = null)
        {

            EnergyMix      = null;
            ErrorResponse  = null;

            try
            {

                if (JSON?.HasValues != true)
                {
                    ErrorResponse = "The given JSON object must not be null or empty!";
                    return false;
                }

                //StrictJSON.OnlyProperties(JSON,
                //                          PropertyKeys.Context,
                //                          PropertyKeys.EnergySources,
                //                          PropertyKeys.EnvironmentalImpacts,
                //                          PropertyKeys.SupplierName,
                //                          PropertyKeys.ProductName,
                //                          PropertyKeys.AdditionalRemarks);

                //StrictJSON.Context(JSON, JSONLDContext);

                #region Parse EnergySources           [mandatory]

                if (!PercentageOfExtensions.TryParse<EnergySourceCategory>(JSON,
                                                                           PropertyKeys.EnergySources,
                                                                           "energy source",
                                                                           EnergySourceCategory.TryParse,
                                                                           out var energySources,
                                                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse EnvironmentalImpacts    [mandatory]

                if (!EnvironmentalImpactAmountExtensions.TryParse(JSON,
                                                                  PropertyKeys.EnvironmentalImpacts,
                                                                  out var environmentalImpacts,
                                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse SupplierName            [mandatory]

                if (!JSON.ParseMandatory(PropertyKeys.SupplierName,
                                         "supplier name",
                                         out I18NString supplierName,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse ProductName             [mandatory]

                if (!JSON.ParseMandatory(PropertyKeys.ProductName,
                                         "product name",
                                         out I18NString productName,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse AdditionalRemarks       [optional]

                if (JSON.ParseOptional(PropertyKeys.AdditionalRemarks,
                                       "additional remarks",
                                       I18NString.TryParse,
                                       out I18NString? additionalRemarks,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                EnergyMix = new EnergyMix(
                                energySources,
                                environmentalImpacts,
                                supplierName,
                                productName,
                                additionalRemarks
                            );


                if (CustomEnergyMixParser is not null)
                    EnergyMix = CustomEnergyMixParser(JSON,
                                                      EnergyMix);

                return true;

            }
            catch (Exception e)
            {
                EnergyMix      = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomEnergyMixSerializer">A delegate to serialize custom energy mix JSON objects.</param>
        public JObject ToJSON(Boolean                                      Embedded                    = false,
                              CustomJObjectSerializerDelegate<EnergyMix>?  CustomEnergyMixSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,                 JSONLDContext);

            // Mandatory, even when empty: [] stands for an unknown composition.
            json.Add(PropertyKeys.EnergySources,               EnergySources.       ToJSON());
            json.Add(PropertyKeys.EnvironmentalImpacts,        EnvironmentalImpacts.ToJSON());
            json.Add(PropertyKeys.SupplierName,                SupplierName.        ToJSON());
            json.Add(PropertyKeys.ProductName,                 ProductName.         ToJSON());

            if (AdditionalRemarks is not null && AdditionalRemarks.IsNotNullOrEmpty())
                json.Add(PropertyKeys.AdditionalRemarks,       AdditionalRemarks.ToJSON());

            return CustomEnergyMixSerializer is not null
                       ? CustomEnergyMixSerializer(this, json)
                       : json;

        }

        #endregion


        #region IEquatable<EnergyMix> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy mixes for equality.
        /// </summary>
        /// <param name="Object">An energy mix to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMix energyMix &&
                   Equals(energyMix);

        #endregion

        #region Equals(EnergyMix)

        /// <summary>
        /// Compares two energy mixes for equality.
        /// </summary>
        /// <param name="EnergyMix">An energy mix to compare with.</param>
        public Boolean Equals(EnergyMix? EnergyMix)

            => EnergyMix is not null &&

               SupplierName.        Equals       (EnergyMix.SupplierName)         &&
               ProductName.         Equals       (EnergyMix.ProductName)          &&
               EnergySources.       SequenceEqual(EnergyMix.EnergySources)        &&
               EnvironmentalImpacts.SequenceEqual(EnergyMix.EnvironmentalImpacts) &&

               ((AdditionalRemarks is null     && EnergyMix.AdditionalRemarks is null) ||
                (AdditionalRemarks is not null && EnergyMix.AdditionalRemarks is not null && AdditionalRemarks.Equals(EnergyMix.AdditionalRemarks)));

        #endregion

        #endregion

        #region (override) GetHashCode()

        private readonly Int32 hashCode;

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => hashCode;

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat("'", ProductName.FirstText(), "' from '", SupplierName.FirstText(), "'");

        #endregion

    }

}
