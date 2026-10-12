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

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The examples of the README, as they are written there.
    /// </summary>
    [TestFixture]
    public class ReadmeTests
    {

        #region The_change_set_example_works()

        [Test]
        public void The_change_set_example_works()
        {

            var gef = new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GEF"),
                                                  I18NString.Create(Languages.en, "GraphDefined"));

            var rn1 = new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                         DataSource:                "v1",
                                         ChargingStationOperators:  [ gef ]);

            var newOperator = new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*NEW"));

            var cs  = rn1.CreateChangeSet([
                          RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "v2"),             // the network
                          RoamingNetworkChange.Add           (newOperator),                               // a new operator
                          RoamingNetworkChange.UpdateProperty(gef, "telephone",  null, "+49 30 7654321")  // the operator DE*GEF
                      ]);

            var rn2 = rn1.ApplyChangeSet(cs);   // rn1 stays as it was

            using (Assert.EnterMultipleScope())
            {

                Assert.That(cs.Changes[2].EntityType,  Is.EqualTo(nameof(ChargingStationOperator)));
                Assert.That(cs.Changes[2].EntityId,    Is.EqualTo("DE*GEF"));

                Assert.That(rn1.DataSource,                                         Is.EqualTo("v1"));
                Assert.That(rn1.ChargingStationOperators.Count(),                   Is.EqualTo(1));

                Assert.That(rn2.DataSource,                                         Is.EqualTo("v2"));
                Assert.That(rn2.ChargingStationOperators.Count(),                   Is.EqualTo(2));
                Assert.That(rn2.GetChargingStationOperatorById(gef.Id)!.ToEmbeddedJSON()["telephone"]?.ToString(),
                            Is.EqualTo("+49 30 7654321"));

            }

        }

        #endregion

    }

}
