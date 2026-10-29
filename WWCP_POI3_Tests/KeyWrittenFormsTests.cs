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

using cloud.charging.open.protocols.WWCP.POI.tests.Proofs;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// A public key as the content holds it (SPECIFICATION.md 2, Written forms, and 4.1): its timestamps written as
    /// yyyy-MM-ddTHH:mm:ss.fffZ, its key the deterministic CBOR of its COSE key. Another form of the same instant, null
    /// for none, or another CBOR of the same key would be written again as another text than the one hashed, and is
    /// refused (D13 of WWCPPOIConformanceTests).
    /// </summary>
    [TestFixture]
    public class KeyWrittenFormsTests
    {

        #region (static) Written()

        /// <summary>
        /// A signing key of the charging station operator DE*GEF with both timestamps, as POI writes it.
        /// </summary>
        private static JObject Written()
        {

            var (privateKey, _) = TestParties.Key(new POIPartyId(nameof(ChargingStationOperator), "DE*GEF/written"), POIKeyKind.Signing);

            return POIPublicKey.From(privateKey,
                                     NotBefore:  DateTimeOffset.Parse("2026-10-21T12:00:00.123Z"),
                                     NotAfter:   DateTimeOffset.Parse("2027-10-21T12:00:00.456Z")).ToJSON(Embedded: true);

        }

        #endregion


        #region Timestamps_as_the_content_holds_them()

        /// <summary>
        /// A key is read back as written; a timestamp of another form, of the same instant or not, or null, is refused.
        /// </summary>
        [Test]
        public void Timestamps_as_the_content_holds_them()
        {

            var json = Written();

            JObject With(String Property, JToken Value)
            {
                var copy = (JObject) json.DeepClone();
                copy[Property] = Value;
                return copy;
            }

            using (Assert.EnterMultipleScope())
            {

                Assert.That(POIPublicKey.TryParse(json, out var read, out var error),                                                      Is.True, error);
                Assert.That(read!.ToJSON(Embedded: true).ToString(),                                                                       Is.EqualTo(json.ToString()), "written again as read");

                Assert.That(POIPublicKey.TryParse(With(POIPublicKey.PropertyKeys.NotAfter,  "2027-10-21T14:00:00.456+02:00"), out _, out error), Is.False, "the same instant with an offset");
                Assert.That(error,                                                                                                         Does.Contain("must be written as yyyy-MM-ddTHH:mm:ss.fffZ, not '2027-10-21T14:00:00.456+02:00'!"));
                Assert.That(POIPublicKey.TryParse(With(POIPublicKey.PropertyKeys.NotAfter,  "2027-10-21"),                    out _, out error), Is.False, "a date only");
                Assert.That(POIPublicKey.TryParse(With(POIPublicKey.PropertyKeys.NotAfter,  "2027-10-21T12:00:00Z"),          out _, out error), Is.False, "not to the millisecond");
                Assert.That(POIPublicKey.TryParse(With(POIPublicKey.PropertyKeys.NotBefore, "2026-10-21T12:00:00.1230Z"),     out _, out error), Is.False, "to the tenth of a millisecond");
                Assert.That(POIPublicKey.TryParse(With(POIPublicKey.PropertyKeys.NotAfter,  JValue.CreateNull()),             out _, out error), Is.False, "null for none");
                Assert.That(error,                                                                                                         Does.Contain("must be a text!"));

            }

        }

        #endregion

        #region A_key_in_deterministic_CBOR()

        /// <summary>
        /// The same COSE key in CBOR that is not deterministic, its first label written in two bytes, is refused.
        /// </summary>
        [Test]
        public void A_key_in_deterministic_CBOR()
        {

            var json    = Written();
            var bytes   = Convert.FromBase64String(json[POIPublicKey.PropertyKeys.Key]!.Value<String>()!);
            var copy    = (JObject) json.DeepClone();

            Assert.That(bytes[1], Is.LessThan(24), "a first label of one byte");

            Byte[] notShortest = [ bytes[0], 0x18, .. bytes[1..] ];
            copy[POIPublicKey.PropertyKeys.Key] = Convert.ToBase64String(notShortest);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(POIPublicKey.TryParse(copy, out _, out var error),  Is.False);
                Assert.That(error,                                              Does.Contain("Invalid deterministic CBOR"));
            }

        }

        #endregion

    }

}
