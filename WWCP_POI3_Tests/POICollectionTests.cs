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

using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.DataStructures
{

    /// <summary>
    /// Immutable collections of children with unique identifications, in the order of their identifications,
    /// with their representations calculated once.
    /// </summary>
    [TestFixture]
    public class POICollectionTests
    {

        #region (static) Points(Ids)

        private static POICollection<GridConnectionPoint_Id, GridConnectionPoint> Points(params String[] Ids)

            => new (Ids.Select(id => new GridConnectionPoint(GridConnectionPoint_Id.Parse(id))),
                    point => point.Id,
                    "grid connection point",
                    "the grid operator 'DE*NET'");

        #endregion


        #region Children_are_ordered_and_found_by_their_identifications()

        [Test]
        public void Children_are_ordered_and_found_by_their_identifications()
        {

            var points = Points("b", "a", "C");

            using (Assert.EnterMultipleScope())
            {

                // Ordinal order of the texts: upper case before lower case.
                Assert.That(points.Select(point => point.Id.ToString()),   Is.EqualTo(new[] { "C", "a", "b" }));
                Assert.That(points.Ids.Select(id => id.ToString()),        Is.EqualTo(new[] { "C", "a", "b" }));
                Assert.That(points.Items.Select(point => point.Id.ToString()),  Is.EqualTo(new[] { "C", "a", "b" }));
                Assert.That(points[1].Id.ToString(),                       Is.EqualTo("a"));
                Assert.That(points.Count,                                  Is.EqualTo(3));
                Assert.That(points.IsEmpty,                                Is.False);

                // Grid connection point identifications compare without case.
                Assert.That(points.TryGet(GridConnectionPoint_Id.Parse("A"), out var a),  Is.True);
                Assert.That(a!.Id.ToString(),                              Is.EqualTo("a"));
                Assert.That(points.Contains(GridConnectionPoint_Id.Parse("c")),  Is.True);
                Assert.That(points.Get(GridConnectionPoint_Id.Parse("d")),       Is.Null);
                Assert.That(points.Index.Count,                            Is.EqualTo(3));

                Assert.That(POICollection<GridConnectionPoint_Id, GridConnectionPoint>.Empty.IsEmpty,  Is.True);
                Assert.That(new POICollection<GridConnectionPoint_Id, GridConnectionPoint>(null, point => point.Id, "grid connection point").Count,
                            Is.Zero);

            }

        }

        #endregion

        #region Duplicates_and_nulls_are_refused()

        [Test]
        public void Duplicates_and_nulls_are_refused()
        {

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => Points("a", "A"),
                            Throws.ArgumentException.With.Message.Contains("The grid connection point 'A' of the grid operator 'DE*NET' must not be given twice!"));

                Assert.That(() => new POICollection<GridConnectionPoint_Id, GridConnectionPoint>([ null! ], point => point.Id, "grid connection point"),
                            Throws.ArgumentException.With.Message.Contains("must not contain null entries"));

                // Parking product identifications compare with case: these are two.
                Assert.That(new POICollection<ParkingProduct_Id, ParkingProduct>([ new ParkingProduct(ParkingProduct_Id.Parse("hourly")),
                                                                                   new ParkingProduct(ParkingProduct_Id.Parse("Hourly")) ],
                                                                                 product => product.Id,
                                                                                 "parking product").Count,
                            Is.EqualTo(2));

            }

        }

        #endregion

        #region A_representation_is_calculated_once_per_view_and_format()

        [Test]
        public void A_representation_is_calculated_once_per_view_and_format()
        {

            var points   = Points("b", "a");
            var calls    = 0;

            JToken Ids()
            {
                calls++;
                return new JArray(points.Select(point => point.Id.ToString()));
            }

            var json1    = points.Representation("ids", ETagFormat.JSON, Ids);
            var json2    = points.Representation("ids", ETagFormat.JSON, Ids);
            var cbor     = points.Representation("ids", ETagFormat.CBOR, Ids);
            var other    = points.Representation("other", ETagFormat.JSON, () => new JArray());

            using (Assert.EnterMultipleScope())
            {

                Assert.That(json2,                                     Is.SameAs(json1));
                Assert.That(calls,                                     Is.EqualTo(2));
                Assert.That(System.Text.Encoding.UTF8.GetString(json1.Content),  Is.EqualTo("""["a","b"]"""));
                Assert.That(json1.ETag,                                Is.EqualTo(ETag.Compute(ETagFormat.JSON, json1.Content)));
                Assert.That(json1.ETag.DigestHex,                      Is.EqualTo(Convert.ToHexStringLower(SHA256.HashData(json1.Content))));
                Assert.That(cbor.ETag.Format,                          Is.EqualTo(ETagFormat.CBOR));
                Assert.That(CBORJSON.ToJSON(CBORValue.Parse(cbor.Content))!.ToString(Newtonsoft.Json.Formatting.None),
                            Is.EqualTo("""["a","b"]"""));
                Assert.That(other.ETag,                                Is.Not.EqualTo(json1.ETag));

            }

        }

        #endregion

    }

}
