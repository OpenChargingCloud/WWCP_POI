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

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Complex
{

    /// <summary>
    /// The energy mix is a plain data structure with an optional JSON-LD context,
    /// and shares written as [category, percentage] pairs with the percentage's unit.
    /// </summary>
    [TestFixture]
    public class EnergyMixJsonTests
    {

        private const String Full = """
            {
              "energySources":        [ [ "solar", "60 %" ], [ "wind", "40 %" ] ],
              "environmentalImpacts": [ [ "CO2",   "0 %"  ] ],
              "supplierName":         { "en": "Supplier" },
              "productName":          { "en": "Green", "de": "Grün" },
              "additionalRemarks":    { "de": "Test" }
            }
            """;

        private static EnergyMix Parse(String JSON)
            => EnergyMix.Parse(JObject.Parse(JSON));

        private static Boolean Rejects(String JSON)
            => !EnergyMix.TryParse(JObject.Parse(JSON), out _, out _);


        #region Roundtrip_with_and_without_context()

        [Test]
        public void Roundtrip_with_and_without_context()
        {

            var mix = Parse(Full);

            Assert.That(mix.EnergySources.Select(share => share.Percent),   Is.EqualTo(new[] { 60f, 40f }));
            Assert.That(mix.ProductName[Languages.de],                      Is.EqualTo("Grün"));
            Assert.That(mix.AdditionalRemarks![Languages.de],               Is.EqualTo("Test"));

            var standalone = mix.ToJSON();
            var embedded   = mix.ToJSON(Embedded: true);

            Assert.That(standalone["@context"]!.Value<String>(),             Is.EqualTo(EnergyMix.JSONLDContext));
            Assert.That(embedded  ["@context"],                              Is.Null);
            Assert.That(embedded  ["energySources"]![0]![1]!.Value<String>(),  Is.EqualTo("60 %"));

            Assert.That(EnergyMix.Parse(standalone),                         Is.EqualTo(mix));
            Assert.That(EnergyMix.Parse(embedded),                           Is.EqualTo(mix));

        }

        #endregion

        #region Percentages_roundtrip_exactly_in_any_culture()

        [TestCase("en-US")]
        [TestCase("de-DE")]
        public void Percentages_roundtrip_exactly_in_any_culture(String Culture)
        {

            var previous = System.Globalization.CultureInfo.CurrentCulture;
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(Culture);

            try
            {

                var mix  = new EnergyMix([new PercentageOf<EnergySourceCategory>(EnergySourceCategory.Parse("solar"), 100f / 3)],
                                         [],
                                         ImmutableI18NString.Empty,
                                         ImmutableI18NString.Empty);

                var json = mix.ToJSON(Embedded: true);

                Assert.That(json["energySources"]![0]![1]!.Value<String>(),  Does.Contain(".").And.EndWith(" %"));
                Assert.That(EnergyMix.Parse(json),                           Is.EqualTo(mix));

            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }

        }

        #endregion

        #region Unknown_composition_and_empty_names()

        [Test]
        public void Unknown_composition_and_empty_names()
        {

            var mix = Parse("""{"energySources":[],"environmentalImpacts":[],"supplierName":{},"productName":{"en":"Unknown"}}""");

            Assert.That(mix.EnergySources,         Is.Empty);
            Assert.That(mix.EnvironmentalImpacts,  Is.Empty);
            Assert.That(mix.SupplierName,          Is.EqualTo(ImmutableI18NString.Empty));
            Assert.That(mix.AdditionalRemarks,     Is.Null);

        }

        #endregion

        #region Invalid_documents_are_rejected(JSON)

        [TestCase("""{"environmentalImpacts":[],"supplierName":{},"productName":{}}""")]
        [TestCase("""{"energySources":{},"environmentalImpacts":[],"supplierName":{},"productName":{}}""")]
        [TestCase("""{"energySources":[],"environmentalImpacts":[],"productName":{}}""")]
        [TestCase("""{"energySources":[],"environmentalImpacts":[],"supplierName":"Supplier","productName":{}}""")]
        [TestCase("""{"energySources":[["solar","101 %"]],"environmentalImpacts":[],"supplierName":{},"productName":{}}""")]
        [TestCase("""{"energySources":[["solar","-1 %"]],"environmentalImpacts":[],"supplierName":{},"productName":{}}""")]
        [TestCase("""{"energySources":[["solar","NaN %"]],"environmentalImpacts":[],"supplierName":{},"productName":{}}""")]
        [TestCase("""{"energySources":[["solar",50]],"environmentalImpacts":[],"supplierName":{},"productName":{}}""")]
        [TestCase("""{"energySources":[["solar","50"]],"environmentalImpacts":[],"supplierName":{},"productName":{}}""")]
        [TestCase("""{"energySources":[["solar","50,5 %"]],"environmentalImpacts":[],"supplierName":{},"productName":{}}""")]
        [TestCase("""{"energySources":[[42,"50 %"]],"environmentalImpacts":[],"supplierName":{},"productName":{}}""")]
        [TestCase("""{"energySources":[["solar"]],"environmentalImpacts":[],"supplierName":{},"productName":{}}""")]
        [TestCase("""{"energySources":[["solar","50 %","x"]],"environmentalImpacts":[],"supplierName":{},"productName":{}}""")]
        [TestCase("""{"energySources":[{"source":"solar","percentage":"50 %"}],"environmentalImpacts":[],"supplierName":{},"productName":{}}""")]
        [TestCase("""{"energySources":[["solar","50 %"],["solar","50 %"]],"environmentalImpacts":[],"supplierName":{},"productName":{}}""")]
        public void Invalid_documents_are_rejected(String JSON)
        {
            Assert.That(Rejects(JSON), Is.True);
        }

        #endregion

        #region Errors_name_their_path()

        [Test]
        public void Errors_name_their_path()
        {

            Assert.That(EnergyMix.TryParse(JObject.Parse("""{"energySources":[["solar","50 %"],["wind","x %"]],"environmentalImpacts":[],"supplierName":{},"productName":{}}"""),
                                           out var mix,
                                           out var error),
                        Is.False);

            Assert.That(mix,    Is.Null);
            Assert.That(error,  Does.StartWith("energySources[1][1]: "));

            Assert.That(EnergyMix.TryParse(JObject.Parse("""{"energySources":[["solar","50 %"],["solar","50 %"]],"environmentalImpacts":[],"supplierName":{},"productName":{}}"""),
                                           out _,
                                           out error),
                        Is.False);

            Assert.That(error,  Is.EqualTo("energySources[1][0]: the energy source 'solar' must not be given twice!"));

            Assert.That(EnergyMix.TryParse(JObject.Parse("""{"energySources":[],"environmentalImpacts":[["CO2"]],"supplierName":{},"productName":{}}"""),
                                           out _,
                                           out error),
                        Is.False);

            Assert.That(error,  Does.StartWith("environmentalImpacts[0]: "));

        }

        #endregion

    }

}
