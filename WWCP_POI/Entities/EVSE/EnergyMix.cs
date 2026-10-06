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

using Newtonsoft.Json.Linq;
using System.Collections.Immutable;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// An energy mix.
    /// </summary>
    public partial class EnergyMix : IEquatable<EnergyMix>
    {

        #region Properties

        /// <summary>
        /// The energy sources.
        /// </summary>
        public IEnumerable<PercentageOf<EnergySourceCategories>>  EnergySources           { get; }

        /// <summary>
        /// The environmental impacts.
        /// </summary>
        public IEnumerable<PercentageOf<EnvironmentalImpacts>>    EnvironmentalImpacts    { get; }

        /// <summary>
        /// The name or brand of the energy supplier.
        /// </summary>
        public I18NString                                         SupplierName            { get; }

        /// <summary>
        /// The name or brand of the energy product.
        /// </summary>
        public I18NString                                         ProductName             { get; }

        /// <summary>
        /// Optional additional remarks.
        /// </summary>
        public I18NString?                                        AdditionalRemarks       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy mix.
        /// </summary>
        /// <param name="EnergySources">The energy sources.</param>
        /// <param name="EnvironmentalImpacts">The environmental impacts.</param>
        /// <param name="SupplierName">The name or brand of the energy supplier.</param>
        /// <param name="ProductName">The name or brand of the energy product.</param>
        /// <param name="AdditionalRemarks">Optional additional remarks.</param>
        public EnergyMix(IEnumerable<PercentageOf<EnergySourceCategories>>  EnergySources,
                         IEnumerable<PercentageOf<EnvironmentalImpacts>>    EnvironmentalImpacts,
                         I18NString                                         SupplierName,
                         I18NString                                         ProductName,
                         I18NString?                                        AdditionalRemarks   = null)
        {

            ArgumentNullException.ThrowIfNull(EnergySources);
            ArgumentNullException.ThrowIfNull(EnvironmentalImpacts);
            ArgumentNullException.ThrowIfNull(SupplierName);
            ArgumentNullException.ThrowIfNull(ProductName);
            var sources = EnergySources.ToImmutableArray();
            var impacts = EnvironmentalImpacts.ToImmutableArray();
            if (sources.Any(item => item.Value.IsNullOrEmpty || !float.IsFinite(item.Percent) || item.Percent < 0 || item.Percent > 100) ||
                impacts.Any(item => item.Value.IsNullOrEmpty || !float.IsFinite(item.Percent) || item.Percent < 0 || item.Percent > 100))
                throw new ArgumentException("Energy mix entries require a category and a percentage between zero and 100.");
            if (sources.Select(item => item.Value).Distinct().Count() != sources.Length || impacts.Select(item => item.Value).Distinct().Count() != impacts.Length)
                throw new ArgumentException("Energy mix categories must be unique.");
            this.EnergySources         = sources;
            this.EnvironmentalImpacts  = impacts;
            this.SupplierName          = SupplierName;
            this.ProductName           = ProductName;
            this.AdditionalRemarks     = AdditionalRemarks;

        }

        #endregion


        #region ToJSON(this EnergyMix)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        public JObject ToJSON()

            => JSONObject.Create(

                         new JProperty("energySources", new JArray(EnergySources.Select(item =>
                             new JObject(new JProperty("source", item.Value.ToString()), new JProperty("percent", item.Percent))))),
                         new JProperty("environmentalImpacts", new JArray(EnvironmentalImpacts.Select(item =>
                             new JObject(new JProperty("impact", item.Value.ToString()), new JProperty("percent", item.Percent))))),

                         new JProperty("supplierName",  SupplierName.ToJSON()),
                         new JProperty("productName",   ProductName. ToJSON()),

                   AdditionalRemarks is not null
                       ? new JProperty("additionalRemarks",  AdditionalRemarks.ToJSON())
                       : null

               );

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

               SupplierName.       Equals(EnergyMix.SupplierName) &&
               ProductName.        Equals(EnergyMix.ProductName)  &&
               EnergySources.SequenceEqual(EnergyMix.EnergySources) &&
               EnvironmentalImpacts.SequenceEqual(EnergyMix.EnvironmentalImpacts) &&

               ((AdditionalRemarks is null     && EnergyMix.AdditionalRemarks is null) ||
                (AdditionalRemarks is not null && EnergyMix.AdditionalRemarks is not null && AdditionalRemarks.Equals(EnergyMix.AdditionalRemarks)));

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return SupplierName.       GetHashCode() * 7 ^
                       ProductName.        GetHashCode() * 5 ^
                       (AdditionalRemarks?.GetHashCode() ?? 0);

            }
        }

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
