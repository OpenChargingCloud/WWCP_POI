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
    /// An energy mix prognosis: names once, and consecutive periods
    /// with an optional end and their [category, percentage] pairs.
    /// </summary>
    [TestFixture]
    public class EnergyMixPrognosisJsonTests
    {

        private const String Full = """
            {
              "timestamp":    "2026-10-11T12:00:00.123+02:00",
              "supplierName": { "en": "Supplier" },
              "productName":  { "en": "Green" },
              "periods": [
                { "start": "2026-10-11T13:00:00Z", "end": "2026-10-11T14:00:00Z",
                  "energySources": [ [ "solar", "60 %" ], [ "wind", "40 %" ] ], "environmentalImpacts": [ [ "CO2", "0 %" ] ] },
                { "start": "2026-10-11T16:00:00+02:00",
                  "energySources": [ [ "wind", "100 %" ] ], "environmentalImpacts": [] }
              ]
            }
            """;

        private static Boolean Rejects(String JSON, out String? Error)
            => !EnergyMixPrognosis.TryParse(JObject.Parse(JSON), out _, out Error);


        #region Roundtrip_keeps_periods_ends_and_milliseconds()

        [Test]
        public void Roundtrip_keeps_periods_ends_and_milliseconds()
        {

            var prognosis = EnergyMixPrognosis.Parse(JObject.Parse(Full));
            var periods   = prognosis.Periods.ToArray();

            Assert.That(periods,                                           Has.Length.EqualTo(2));
            Assert.That(periods[0].End,                                    Is.EqualTo(DateTimeOffset.Parse("2026-10-11T14:00:00Z")));
            Assert.That(periods[1].End,                                    Is.Null);
            Assert.That(periods[1].Start,                                  Is.EqualTo(DateTimeOffset.Parse("2026-10-11T14:00:00Z")));
            Assert.That(periods[0].EnergySources.Select(share => share.Percent), Is.EqualTo(new[] { 60f, 40f }));

            var standalone = prognosis.ToJSON();
            var embedded   = prognosis.ToJSON(Embedded: true);

            Assert.That(standalone["@context"]!.Value<String>(),           Is.EqualTo(EnergyMixPrognosis.JSONLDContext));
            Assert.That(embedded  ["@context"],                            Is.Null);
            Assert.That(embedded  ["periods"]![1]!["end"],                 Is.Null);

            Assert.That(EnergyMixPrognosis.Parse(standalone),              Is.EqualTo(prognosis));
            Assert.That(EnergyMixPrognosis.Parse(embedded).GetHashCode(),  Is.EqualTo(prognosis.GetHashCode()));
            Assert.That(embedded  ["timestamp"]!.Value<String>(),          Is.EqualTo("2026-10-11T10:00:00.123Z"));
            Assert.That(embedded  ["periods"]![0]!["end"]!.Value<String>(), Is.EqualTo("2026-10-11T14:00:00.000Z"));

        }

        #endregion

        #region Invalid_prognoses_name_their_path(JSON, Error)

        [TestCase("""{"timestamp":"2026-10-11T12:00:00Z","supplierName":{},"productName":{}}""",
                  "The 'periods' property must be an array!")]
        [TestCase("""{"timestamp":"2026-10-11T12:00:00Z","supplierName":{},"productName":{},"periods":[42]}""",
                  "periods[0]: expected an object!")]
        [TestCase("""{"timestamp":"2026-10-11T12:00:00Z","supplierName":{},"productName":{},"periods":[{"start":"2026-10-11T13:00:00Z","end":"2026-10-11T13:00:00Z","energySources":[],"environmentalImpacts":[]}]}""",
                  "periods[0]: end: must be after the start!")]
        [TestCase("""{"timestamp":"2026-10-11T12:00:00Z","supplierName":{},"productName":{},"periods":[{"start":"2026-10-11T13:00:00Z","energySources":[["solar","101 %"]],"environmentalImpacts":[]}]}""",
                  "periods[0]: energySources[0][1]: ")]
        [TestCase("""{"timestamp":"2026-10-11T12:00:00Z","supplierName":{},"productName":{},"periods":[{"start":"2026-10-11T14:00:00Z","energySources":[],"environmentalImpacts":[]},{"start":"2026-10-11T13:00:00Z","energySources":[],"environmentalImpacts":[]}]}""",
                  "periods[1].start: the periods must be ordered by their start!")]
        [TestCase("""{"timestamp":"2026-10-11T12:00:00Z","supplierName":{},"productName":{},"periods":[{"start":"2026-10-11T13:00:00Z","end":"2026-10-11T15:00:00Z","energySources":[],"environmentalImpacts":[]},{"start":"2026-10-11T14:00:00Z","energySources":[],"environmentalImpacts":[]}]}""",
                  "periods[0].end: the period must end before the next one starts!")]
        public void Invalid_prognoses_name_their_path(String JSON, String Error)
        {

            Assert.That(Rejects(JSON, out var error),  Is.True);
            Assert.That(error,                         Does.StartWith(Error));

        }

        #endregion

        #region Gaps_between_periods_and_unknown_compositions_are_allowed()

        [Test]
        public void Gaps_between_periods_and_unknown_compositions_are_allowed()
        {

            var prognosis = EnergyMixPrognosis.Parse(JObject.Parse("""
                {
                  "timestamp": "2026-10-11T12:00:00Z", "supplierName": {}, "productName": {},
                  "periods": [
                    { "start": "2026-10-11T13:00:00Z", "end": "2026-10-11T14:00:00Z", "energySources": [], "environmentalImpacts": [] },
                    { "start": "2026-10-11T16:00:00Z",                                "energySources": [], "environmentalImpacts": [] }
                  ]
                }
                """));

            Assert.That(prognosis.Periods.Count(),  Is.EqualTo(2));
            Assert.That(prognosis.SupplierName,     Is.EqualTo(ImmutableI18NString.Empty));

        }

        #endregion

    }

}
