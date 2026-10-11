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

using System.Globalization;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Complex
{

    [TestFixture]
    public class CommitTests
    {

        #region RoamingNetwork_JSON_RoundtripTest()

        [Test]
        public void RoamingNetwork_JSON_RoundtripTest()
        {

            var rn1 = new RoamingNetwork(
                          Id:           RoamingNetwork_Id.Parse("rn1"),
                          Name:         I18NString.Create(Languages.en, "Roaming Network 1"),
                          Description:  I18NString.Create(Languages.en, "My first roaming network!").
                                                      Set(Languages.de, "Mein erstes Roaming-Netzwerk!"),
                          DataSource:   "test"
                      );

            var j1  = rn1.ToJSON();
            var t1  = j1.ToString(Newtonsoft.Json.Formatting.None);

            var rn2 = RoamingNetwork.Parse(JObject.Parse(t1));
            var t2  = rn2.ToJSON().ToString(Newtonsoft.Json.Formatting.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rn2.Id,            Is.EqualTo(rn1.Id));
                Assert.That(rn2.Name,          Is.EqualTo(rn1.Name));
                Assert.That(rn2.Description,   Is.EqualTo(rn1.Description));
                Assert.That(rn2.DataSource,    Is.EqualTo(rn1.DataSource));
                Assert.That(t2,                Is.EqualTo(t1));
            }

        }

        #endregion

        #region RoamingNetwork_CBOR_RoundtripTest()

        [Test]
        public void RoamingNetwork_CBOR_RoundtripTest()
        {

            var rn1 = new RoamingNetwork(
                          Id:           RoamingNetwork_Id.Parse("rn1"),
                          Name:         I18NString.Create(Languages.en, "Roaming Network 1"),
                          Description:  I18NString.Create(Languages.en, "My first roaming network!").
                                                      Set(Languages.de, "Mein erstes Roaming-Netzwerk!"),
                          DataSource:   "test"
                      );

            var c1  = rn1.ToCBOR();
            var b1  = c1.ToByteArray(CBORWriterOptions.Canonical);

            var rn2 = RoamingNetwork.Parse(CBORValue.Parse(b1));
            var b2  = rn2.ToCBOR().ToByteArray(CBORWriterOptions.Canonical);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rn2.Id,            Is.EqualTo(rn1.Id));
                Assert.That(rn2.Name,          Is.EqualTo(rn1.Name));
                Assert.That(rn2.Description,   Is.EqualTo(rn1.Description));
                Assert.That(rn2.DataSource,    Is.EqualTo(rn1.DataSource));
                Assert.That(b2,                Is.EqualTo(b1));
            }

        }

        #endregion

        #region RoamingNetwork_CBOR_rejects_an_unknown_language()

        [Test]
        public void RoamingNetwork_CBOR_rejects_an_unknown_language()
        {

            var cbor = CBORValue.FromMap([
                           new (CBORValue.FromText("@id"),   CBORValue.FromText("rn1")),
                           new (CBORValue.FromText("name"),  CBORValue.FromMap([
                                                                 new (CBORValue.FromText("xx-unknown"), CBORValue.FromText("?"))
                                                             ]))
                       ]);

            Assert.That(RoamingNetwork.TryParse(cbor, out _, out var error),  Is.False);
            Assert.That(error,                                                Does.Contain("xx-unknown"));

        }

        #endregion

    }

}
