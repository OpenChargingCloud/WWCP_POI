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

using cloud.charging.open.protocols.WWCP.POI.tests.Proofs;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// No lastChange goes back (SPECIFICATION.md, section 4; POI3-ISSUES.md S5): a change set is stamped with its
    /// createdAt, or with the lastChange of the version it is applied to, where it was created before it, as the
    /// lastChange of the roaming network, of what it changes, adds and what embeds it, and as the created of what it
    /// adds without one.
    /// Its keys are checked at its createdAt still, and nobody is refused for it.
    /// </summary>
    [TestFixture]
    public class LastChangeTests
    {

        #region A_change_set_created_before_the_version_is_stamped_with_its_last_change()

        [Test]
        public void A_change_set_created_before_the_version_is_stamped_with_its_last_change()
        {

            var network   = StationProofTests.NewNetwork();
            var last      = network.LastChangeDate;
            var gef       = network.GetChargingStationOperatorById(ChargingStationOperator_Id.Parse("DE*GEF"))!;

            // Created an hour before the last change of the version: a homepage of the operator, and a new provider.
            var backdated = network.ApplyChangeSet(network.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(gef, "homepage", null, "https://gef.example"),
                                                                                   RoamingNetworkChange.Add(new EMobilityProvider(EMobilityProvider_Id.Parse("DE*NEW")).WithKeys()) ],
                                                                                 CreatedAt: last.AddHours(-1)));

            // Created later: stamped with its createdAt.
            var later     = backdated.ApplyChangeSet(backdated.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(backdated.GetChargingStationOperatorById(gef.Id)!, "homepage", null, "https://gef.example/later") ],
                                                                                     CreatedAt: last.AddHours(1)));

            Assert.That(backdated.TryGetEMobilityProviderById(EMobilityProvider_Id.Parse("DE*NEW"), out var added), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(backdated.LastChangeDate,                                                             Is.EqualTo(last), "the roaming network");
                Assert.That(backdated.GetChargingStationOperatorById(gef.Id)!.LastChangeDate,                    Is.EqualTo(last), "what it changes");
                Assert.That(added!.LastChangeDate,                                                                Is.EqualTo(last), "what it adds");
                Assert.That(later.LastChangeDate,                                                                 Is.EqualTo(last.AddHours(1)));
                Assert.That(later.GetChargingStationOperatorById(gef.Id)!.LastChangeDate,                        Is.EqualTo(last.AddHours(1)));
            }

        }

        #endregion

    }

}
