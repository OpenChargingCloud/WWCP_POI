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

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// A roaming network embeds its operator, with the keys of the operator: the roaming network has none of its own.
    /// The operator owns the roaming network and itself, adds the other parties, and may operate many roaming networks.
    /// </summary>
    [TestFixture]
    public class RoamingNetworkOperatorTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-17T12:00:00Z");

        private static readonly RoamingNetworkOperator_Id   RNO      = RoamingNetworkOperator_Id.Parse("GraphDefined");
        private static readonly POIPartyId                  Party    = new (nameof(RoamingNetworkOperator), RNO.ToString());

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");

        #endregion

        #region NewOperator() / NewNetwork(Id, Operator)

        private static RoamingNetworkOperator NewOperator()

            => new (RNO,
                    I18NString.Create(Languages.en, "GraphDefined"),
                    IdentityKeys:            [ TestParties.Key(Party, POIKeyKind.Identity).Public ],
                    SigningKeyCertificates:  [ RoamingNetworkSigningKeyCertificate.Create(TestParties.Key(Party, POIKeyKind.Identity).Private,
                                                                                         COSEAlgorithm.ES512,
                                                                                         RNO,
                                                                                         TestParties.Key(Party, POIKeyKind.Signing).Public,
                                                                                         Created) ],
                    Created:                 Created);

        private static RoamingNetwork NewNetwork(String                   Id        = "QA",
                                                 RoamingNetworkOperator?  Operator  = null,
                                                 Boolean                  Operated  = true)

            => new (RoamingNetwork_Id.Parse(Id),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF, Created: Created).WithKeys() ],
                    Operator:                  Operated ? Operator ?? NewOperator() : null);

        #endregion

        #region Change(Network, Changes)

        private static RoamingNetworkChangeSet Change(RoamingNetwork                Network,
                                                      params RoamingNetworkChange[] Changes)

            => Network.CreateChangeSet(Changes, CreatedAt: T0);

        #endregion

        #region Apply(Network, ChangeSet)

        private static (RoamingNetwork? Network, String? Error) Apply(RoamingNetwork           Network,
                                                                      RoamingNetworkChangeSet  ChangeSet)

            => Network.TryApplyChangeSet(ChangeSet, out var applied, out var error)
                   ? (applied, null)
                   : (null,    error);

        #endregion


        #region A_roaming_network_embeds_its_operator_with_its_keys()

        [Test]
        public void A_roaming_network_embeds_its_operator_with_its_keys()
        {

            var qa        = NewNetwork();
            var json      = qa.ToJSON();

            var withKeys  = (JObject) json.DeepClone();
            withKeys[POIPartyKeys.SigningKeysProperty] = json[RoamingNetwork.PropertyKeys.Operator]![POIPartyKeys.IdentityKeysProperty]!.DeepClone();

            Assert.That(RoamingNetwork.TryParse(JObject.Parse(json.ToString()),  out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(qa.ToCBOR(),                     out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(json[RoamingNetwork.PropertyKeys.Operator]!["@id"]!.Value<String>(),       Is.EqualTo("GraphDefined"));
                Assert.That(json[POIPartyKeys.SigningKeysProperty],                                   Is.Null);
                Assert.That(json[POIPartyKeys.IdentityKeysProperty],                                  Is.Null);

                Assert.That(fromJSON!.Operator!.Id,                                                   Is.EqualTo(RNO));
                Assert.That(fromJSON.Operator.Name.FirstText(),                                       Is.EqualTo("GraphDefined"));
                Assert.That(fromJSON.Operator.SigningKeys.Single().Id,                                Is.EqualTo(TestParties.Key(Party).Public.Id));
                Assert.That(fromJSON.CalcETags(),                                                     Is.EqualTo(qa.CalcETags()));
                Assert.That(fromCBOR!.Operator!.IdentityKeys.Single().Id,                             Is.EqualTo(TestParties.Key(Party, POIKeyKind.Identity).Public.Id));
                Assert.That(fromCBOR.CalcETags(),                                                     Is.EqualTo(qa.CalcETags()));

                // The operator is the first party.
                Assert.That(qa.Parties.First().Id,                                                    Is.EqualTo(Party));
                Assert.That(qa.OperatorParty,                                                         Is.EqualTo(Party));

                // Keys of the roaming network itself, as before there were operators, are refused, not ignored.
                Assert.That(RoamingNetwork.TryParse(withKeys, out _, out var keysError),              Is.False);
                Assert.That(keysError,                                                                Does.Contain("has no keys of its own"));

            }

        }

        #endregion

        #region The_operator_changes_the_network_and_itself_and_adds_parties()

        [Test]
        public void The_operator_changes_the_network_and_itself_and_adds_parties()
        {

            var qa         = NewNetwork();
            var newCSO     = new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*NEW"), Created: T0).WithKeys();

            var changes    = Change(qa,
                                    RoamingNetworkChange.UpdateProperty(qa,           "dataSource", null, "v2"),
                                    RoamingNetworkChange.UpdateProperty(qa.Operator!, "homepage",   null, "https://open.charging.cloud"),
                                    RoamingNetworkChange.Add(newCSO));

            var byOperator = Apply(qa, changes.SignedBy(Party).ProvenByNewIdentityKeys());
            var byGEF      = Apply(qa, changes.SignedBy(TestParties.Party(GEF)));
            var nameByGEF  = Apply(qa, Change(qa, RoamingNetworkChange.UpdateProperty(qa.Operator!, "name", null, new JObject(new JProperty("en", "Not GraphDefined")))).
                                           SignedBy(TestParties.Party(GEF)));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(byOperator.Error,                                                         Is.Null);
                Assert.That(byOperator.Network!.DataSource,                                           Is.EqualTo("v2"));
                Assert.That(byOperator.Network.Operator!.Homepage?.ToString(),                        Is.EqualTo("https://open.charging.cloud"));
                Assert.That(byOperator.Network.Operator.LastChangeDate,                               Is.EqualTo(T0));
                Assert.That(byOperator.Network.ChargingStationOperators.Count(),                      Is.EqualTo(2));

                // Its keys are kept.
                Assert.That(byOperator.Network.Operator.SigningKeys.Single().Id,                      Is.EqualTo(TestParties.Key(Party).Public.Id));

                Assert.That(byGEF.Error,                                                              Does.Contain("needs a signature by a signing key of the roaming network operator 'GraphDefined'"));
                Assert.That(nameByGEF.Error,                                                          Does.Contain("needs a signature by a signing key of the roaming network operator 'GraphDefined'"));

            }

        }

        #endregion

        #region The_operator_is_neither_added_nor_removed()

        [Test]
        public void The_operator_is_neither_added_nor_removed()
        {

            var qa = NewNetwork();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => Change(qa, RoamingNetworkChange.Remove(nameof(RoamingNetworkOperator), RNO.ToString())),
                            Throws.ArgumentException.With.Message.Contains("neither added nor removed"));

                Assert.That(() => Change(NewNetwork(Operated: false), RoamingNetworkChange.Add(nameof(RoamingNetworkOperator), RNO.ToString(), NewOperator().ToJSON())),
                            Throws.ArgumentException.With.Message.Contains("neither added nor removed"));

                Assert.That(() => Change(qa, RoamingNetworkChange.UpdateProperty(nameof(RoamingNetworkOperator), "Somebody else", "name", null, new JObject(new JProperty("en", "Somebody else")))),
                            Throws.ArgumentException.With.Message.Contains("the roaming network operator 'Somebody else' does not exist"));

            }

        }

        #endregion

        #region A_roaming_network_without_an_operator_is_changed_only_by_its_parties()

        [Test]
        public void A_roaming_network_without_an_operator_is_changed_only_by_its_parties()
        {

            var unoperated  = NewNetwork(Operated: false);

            var network     = Apply(unoperated, Change(unoperated, RoamingNetworkChange.UpdateProperty(unoperated, "dataSource", null, "v2")).
                                                    SignedBy(TestParties.Party(GEF)));

            var own         = Apply(unoperated, Change(unoperated, RoamingNetworkChange.UpdateProperty(unoperated.GetChargingStationOperatorById(GEF)!, "dataSource", null, "v2")).
                                                    SignedBy(TestParties.Party(GEF)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(unoperated.ToJSON()[RoamingNetwork.PropertyKeys.Operator],                 Is.Null);
                Assert.That(unoperated.Parties.Select(party => party.Id.EntityType),                  Is.EqualTo(new[] { nameof(ChargingStationOperator) }));
                Assert.That(network.Error,                                                            Does.Contain("the roaming network operator, of which this roaming network has none"));
                Assert.That(own.Error,                                                                Is.Null);
            }

        }

        #endregion

        #region One_operator_operates_many_roaming_networks()

        [Test]
        public void One_operator_operates_many_roaming_networks()
        {

            var qa        = NewNetwork("QA");
            var prod      = NewNetwork("PROD");

            var qaChange  = Change(qa,   RoamingNetworkChange.UpdateProperty(qa,   "dataSource", null, "qa")).  SignedBy(Party);
            var prodNext  = Apply(prod,  Change(prod, RoamingNetworkChange.UpdateProperty(prod, "dataSource", null, "prod")).SignedBy(Party));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Apply(qa,  qaChange).Network?.DataSource,                                Is.EqualTo("qa"));
                Assert.That(prodNext.Network?.DataSource,                                            Is.EqualTo("prod"));
                Assert.That(Apply(prod, qaChange).Error,                                              Does.Contain("is for the roaming network 'QA', not for 'PROD'"));
                Assert.That(prod.Operator!.ToJSON().ToString(),                                       Is.EqualTo(qa.Operator!.ToJSON().ToString()));
            }

        }

        #endregion

        #region Concurrent_changes_of_the_operator_are_merged_property_by_property()

        [Test]
        public void Concurrent_changes_of_the_operator_are_merged_property_by_property()
        {

            var qa      = NewNetwork();

            var left    = qa.ApplyChangeSet(Change(qa, RoamingNetworkChange.UpdateProperty(qa.Operator!, "homepage", null, "https://open.charging.cloud")).SignedBy(Party));
            var right   = qa.ApplyChangeSet(Change(qa, RoamingNetworkChange.UpdateProperty(qa.Operator!, "hotline",  null, "+49 30 1234567")).             SignedBy(Party));

            var merged  = RoamingNetwork.TryMerge(qa, left, right, ChangeSet_Id.Parse("merge-1"), out var merge, out _, out var conflicts);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(merged,                                                                   Is.True, String.Join("; ", conflicts.Select(conflict => conflict.ToString())));
                Assert.That(merge!.Operator!.Homepage?.ToString(),                                   Is.EqualTo("https://open.charging.cloud"));
                Assert.That(merge.Operator.HotlinePhoneNumber?.ToString(),                           Is.EqualTo("+49 30 1234567"));
            }

        }

        #endregion

        #region The_operator_has_a_runtime_status()

        [Test]
        public void The_operator_has_a_runtime_status()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());

            var offline  = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(RoamingNetworkOperator), "GraphDefined",  "offline", T0, out _, out var offlineError);
            var unknown  = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(RoamingNetworkOperator), "Somebody else", "offline", T0, out _, out var unknownError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(offline,                                                                  Is.EqualTo(POIStatusUpdateResult.Updated), offlineError);
                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(RoamingNetworkOperator), "GraphDefined", out var status), Is.True);
                Assert.That(status!.Value,                                                            Is.EqualTo("offline"));
                Assert.That(history.Head.RoamingNetwork.Operator!.Status.Value.ToString(),            Is.EqualTo("offline"));

                Assert.That(unknown,                                                                  Is.EqualTo(POIStatusUpdateResult.Refused));
                Assert.That(unknownError,                                                             Does.Contain("is unknown"));

            }

        }

        #endregion

    }

}
