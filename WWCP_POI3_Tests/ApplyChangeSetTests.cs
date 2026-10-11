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

using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.Mail;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.ChangeSets
{

    /// <summary>
    /// Preparing change sets against a roaming network and applying them to new versions.
    /// </summary>
    [TestFixture]
    public class ApplyChangeSetTests
    {

        #region Data

        private static readonly Ed25519PrivateKeyParameters Alice = new (Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"), 0);

        private static readonly DateTimeOffset CommitTime = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        /// <summary>
        /// A license that is not well-known, with its URLs out of order.
        /// </summary>
        private static readonly DataLicense OwnLicense = new (DataLicense_Id.Parse("own-1.0"),
                                                              I18NString.Create(Languages.en, "Our own license"),
                                                              URL.Parse("https://example.org/z"),
                                                              URL.Parse("https://example.org/a"));

        private static ChargingStationOperator NewOperator(String Id, String Name)

            => new (
                   Id:                     ChargingStationOperator_Id.Parse(Id),
                   Name:                   I18NString.Create(Languages.en, Name),
                   Description:            I18NString.Create(Languages.de, "Ein Betreiber"),
                   Logo:                   URL.Parse("https://example.org/logo.png"),
                   Address:                new Address("Hauptstraße", "07743", I18NString.Create(Languages.de, "Jena"), Country.Germany, HouseNumber: "1"),
                   GeoLocation:            GeoCoordinate.Parse(50.9271, 11.5892),
                   Telephone:              PhoneNumber.Parse("+49 3641 123456"),
                   EMailAddress:           SimpleEMailAddress.Parse("info@example.org"),
                   Homepage:               URL.Parse("https://example.org"),
                   HotlinePhoneNumber:     PhoneNumber.Parse("+49 800 123456"),
                   TermsAndConditionsURL:  URL.Parse("https://example.org/terms"),
                   DataLicenses:           [ OwnLicense, DataLicense.OpenDatabaseLicense ],
                   DataSource:             "test"
               );

        private static RoamingNetwork NewNetwork()

            => new (
                   Id:                        RoamingNetwork_Id.Parse("rn1"),
                   Name:                      I18NString.Create(Languages.en, "Roaming Network 1"),
                   Description:               I18NString.Create(Languages.en, "My first roaming network!").
                                                          Set(Languages.de, "Mein erstes Roaming-Netzwerk!"),
                   DataSource:                "test",
                   Created:                   DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                   DataLicenses:              [ OwnLicense, DataLicense.OpenDatabaseLicense ],
                   ChargingStationOperators:  [ NewOperator("DE*GEF", "GraphDefined"), NewOperator("DE*BDO", "Example Operator") ]
               );

        #endregion


        #region A_change_set_makes_a_new_version()

        [Test]
        public void A_change_set_makes_a_new_version()
        {

            var rn1  = NewNetwork();

            var cs1  = rn1.CreateChangeSet(
                           [
                               RoamingNetworkChange.UpdateProperty(rn1, "dataSource", OldValue: "test", NewValue: "test2"),
                               RoamingNetworkChange.UpdateProperty(rn1, "name",       OldValue: null,   NewValue: new JObject(new JProperty("en", "Network 1"))),
                               RoamingNetworkChange.RemoveProperty(rn1, "description")
                           ],
                           Description:  I18NString.Create(Languages.en, "Hello world!"),
                           CreatedAt:    CommitTime
                       );

            var rn2  = rn1.ApplyChangeSet(cs1);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn2.DataSource,               Is.EqualTo("test2"));
                Assert.That(rn2.Name[Languages.en],       Is.EqualTo("Network 1"));
                Assert.That(rn2.Description.Count,        Is.Zero);
                Assert.That(rn2.Revision,                 Is.EqualTo(1));
                Assert.That(rn2.Created,                  Is.EqualTo(rn1.Created));
                Assert.That(rn2.LastChangeDate,           Is.EqualTo(CommitTime));
                Assert.That(rn2.ETags,                    Is.EqualTo(cs1.AfterETags));
                Assert.That(cs1.BeforeETags,              Is.EqualTo(rn1.ETags));

                // The old version stays as it was.
                Assert.That(rn1.DataSource,               Is.EqualTo("test"));
                Assert.That(rn1.Description.Count,        Is.EqualTo(2));
                Assert.That(rn1.Revision,                 Is.Zero);

                // Old values not given are filled in with the current values.
                Assert.That(cs1.Changes[1].OldValue,      Is.EqualTo(rn1.Name.ToJSON()).Using<JToken>(JToken.DeepEquals));
                Assert.That(cs1.Changes[2].OldValue,      Is.EqualTo(rn1.Description.ToJSON()).Using<JToken>(JToken.DeepEquals));

            }

        }

        #endregion

        #region A_change_set_travels_as_JSON_text()

        [Test]
        public void A_change_set_travels_as_JSON_text()
        {

            var rn1  = NewNetwork();
            var cs1  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "test2") ]);

            var rn2  = rn1.ApplyChangeSet(RoamingNetworkChangeSet.Parse(cs1.ToJSON().ToString()));

            Assert.That(rn2.DataSource, Is.EqualTo("test2"));

        }

        #endregion

        #region A_change_set_fits_only_its_own_version()

        [Test]
        public void A_change_set_fits_only_its_own_version()
        {

            var rn1  = NewNetwork();
            var cs1  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "test2") ]);
            var rn2  = rn1.ApplyChangeSet(cs1);

            Assert.That(rn2.TryApplyChangeSet(cs1, out _, out var error),  Is.False);
            Assert.That(error,                                             Does.Contain("does not fit this version"));

        }

        #endregion

        #region Concurrent_change_sets_of_one_property_conflict()

        [Test]
        public void Concurrent_change_sets_of_one_property_conflict()
        {

            var rn1  = NewNetwork();
            var csA  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "A") ]);
            var csB  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "B") ]);

            var rn2  = rn1.ApplyChangeSet(csA);

            using (Assert.EnterMultipleScope())
            {

                // B was prepared against the old version...
                Assert.That(rn2.TryApplyChangeSet(csB, out _, out var error1),  Is.False);
                Assert.That(error1,                                             Does.Contain("does not fit this version"));

                // ...and its changes, prepared again, find "A" where they expect "test".
                Assert.That(() => rn2.CreateChangeSet(csB.Changes),
                            Throws.ArgumentException.With.Message.Contains("no longer the expected old value"));

            }

        }

        #endregion

        #region Concurrent_change_sets_of_different_properties_can_be_prepared_again()

        [Test]
        public void Concurrent_change_sets_of_different_properties_can_be_prepared_again()
        {

            var rn1  = NewNetwork();
            var csA  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "A") ]);
            var csB  = rn1.CreateChangeSet([ RoamingNetworkChange.RemoveProperty(rn1, "description") ]);

            var rn3  = rn1.ApplyChangeSet(csA).
                           ApplyChangeSet(rn1.ApplyChangeSet(csA).CreateChangeSet(csB.Changes));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rn3.DataSource,         Is.EqualTo("A"));
                Assert.That(rn3.Description.Count,  Is.Zero);
                Assert.That(rn3.Revision,           Is.EqualTo(2));
            }

        }

        #endregion

        #region A_change_set_with_wrong_after_ETags_is_refused()

        [Test]
        public void A_change_set_with_wrong_after_ETags_is_refused()
        {

            var rn1    = NewNetwork();
            var cs1    = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "test2") ]);

            var wrong  = new RoamingNetworkChangeSet(cs1.Id,
                                                     cs1.RoamingNetworkId,
                                                     cs1.CreatedAt,
                                                     cs1.Changes,
                                                     cs1.BeforeETags,
                                                     cs1.BeforeETags);

            Assert.That(rn1.TryApplyChangeSet(wrong, out _, out var error),  Is.False);
            Assert.That(error,                                               Does.Contain("result does not match"));

        }

        #endregion

        #region Invalid_changes_are_refused()

        [Test]
        public void Invalid_changes_are_refused()
        {

            var rn1 = NewNetwork();
            var rn0 = new RoamingNetwork(Id: RoamingNetwork_Id.Parse("rn1"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "name", null, "no object") ]),
                            Throws.ArgumentException.With.Message.Contains("invalid name"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, 42) ]),
                            Throws.ArgumentException.With.Message.Contains("nonempty text"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "@id", null, "rn2") ]),
                            Throws.ArgumentException.With.Message.Contains("can not be changed"));

                Assert.That(() => rn0.CreateChangeSet([ RoamingNetworkChange.RemoveProperty(rn0, "description") ]),
                            Throws.ArgumentException.With.Message.Contains("does not exist"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.Remove("ChargingPool", "DE*GEF*P1") ]),
                            Throws.ArgumentException.With.Message.Contains("changes of ChargingPool 'DE*GEF*P1' are not supported yet"));

                Assert.That(() => new RoamingNetwork(Id: RoamingNetwork_Id.Parse("rn2")).ApplyChangeSet(
                                      rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "test2") ])),
                            Throws.ArgumentException.With.Message.Contains("is for the roaming network 'rn1'"));

            }

        }

        #endregion

        #region Two_changes_of_one_property_in_one_change_set()

        [Test]
        public void Two_changes_of_one_property_in_one_change_set()
        {

            var rn1  = NewNetwork();
            var cs1  = rn1.CreateChangeSet([
                           RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "A"),
                           RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "B")
                       ]);

            using (Assert.EnterMultipleScope())
            {
                // The second change finds the value of the first.
                Assert.That(cs1.Changes[0].OldValue!.Value<String>(),  Is.EqualTo("test"));
                Assert.That(cs1.Changes[1].OldValue!.Value<String>(),  Is.EqualTo("A"));
                Assert.That(rn1.ApplyChangeSet(cs1).DataSource,        Is.EqualTo("B"));
            }

        }

        #endregion

        #region Operators_are_added_and_removed()

        [Test]
        public void Operators_are_added_and_removed()
        {

            var rn1  = NewNetwork();
            var bdo  = rn1.GetChargingStationOperatorById(ChargingStationOperator_Id.Parse("DE*BDO"))!;

            var cs1  = rn1.CreateChangeSet([
                           RoamingNetworkChange.Add   (NewOperator("DE*NEW", "A new operator")),
                           RoamingNetworkChange.Remove(bdo.Id)
                       ]);

            var rn2  = rn1.ApplyChangeSet(RoamingNetworkChangeSet.Parse(cs1.ToJSON().ToString()));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn2.ChargingStationOperators.Select(o => o.Id.ToString()).Order(),  Is.EqualTo(new[] { "DE*GEF", "DE*NEW" }));
                Assert.That(rn2.GetChargingStationOperatorById(ChargingStationOperator_Id.Parse("DE*NEW"))!.Telephone,
                            Is.EqualTo(PhoneNumber.Parse("+49 3641 123456")));
                Assert.That(rn2.ETags,                                                          Is.EqualTo(cs1.AfterETags));

                // The removal remembers the operator it removed.
                Assert.That(cs1.Changes[0].OldValue,                                            Is.Null);
                Assert.That(cs1.Changes[1].OldValue,                                            Is.EqualTo(bdo.ToEmbeddedJSON()).Using<JToken>(JToken.DeepEquals));

                // The old version stays as it was.
                Assert.That(rn1.ChargingStationOperators.Select(o => o.Id.ToString()).Order(),  Is.EqualTo(new[] { "DE*BDO", "DE*GEF" }));

            }

        }

        #endregion

        #region Invalid_operator_changes_are_refused()

        [Test]
        public void Invalid_operator_changes_are_refused()
        {

            var rn1      = NewNetwork();
            var invalid  = NewOperator("DE*NEW", "New").ToEmbeddedJSON();
            invalid["telephone"] = "no phone number";

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.Add(NewOperator("DE*GEF", "Twice")) ]),
                            Throws.ArgumentException.With.Message.Contains("'DE*GEF' already exists"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.Remove(ChargingStationOperator_Id.Parse("DE*XYZ")) ]),
                            Throws.ArgumentException.With.Message.Contains("'DE*XYZ' does not exist"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.Add("ChargingStationOperator", "DE*XYZ", NewOperator("DE*NEW", "New").ToEmbeddedJSON()) ]),
                            Throws.ArgumentException.With.Message.Contains("is not the one to add, 'DE*XYZ'"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.Add("ChargingStationOperator", "DE*NEW", invalid) ]),
                            Throws.ArgumentException.With.Message.Contains("telephone number"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.Add("ChargingStationOperator", "DE*NEW", NewOperator("DE*NEW", "New").ToEmbeddedJSON(), "RoamingNetwork", "rn2") ]),
                            Throws.ArgumentException.With.Message.Contains("not to RoamingNetwork 'rn2'"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.AddElement("ChargingStationOperator", "DE*GEF", [ new POIElementPathSegment("brands", "b1") ], new JObject()) ]),
                            Throws.ArgumentException.With.Message.Contains("AddElement of a charging station operator is not supported yet"));

            }

        }

        #endregion

        #region Operator_properties_are_updated_and_removed()

        [Test]
        public void Operator_properties_are_updated_and_removed()
        {

            var rn1  = NewNetwork();
            var gef  = rn1.GetChargingStationOperatorById(ChargingStationOperator_Id.Parse("DE*GEF"))!;

            var cs1  = rn1.CreateChangeSet([
                           RoamingNetworkChange.UpdateProperty(gef, "telephone",     null, "+49 30 7654321"),
                           RoamingNetworkChange.UpdateProperty(gef, "name",          null, new JObject(new JProperty("en", "GraphDefined GmbH"))),
                           RoamingNetworkChange.UpdateProperty(gef, "dataLicenses",  null, new JArray(DataLicense.OpenDatabaseLicense.ToPOIJSON())),
                           RoamingNetworkChange.RemoveProperty(gef, "homepage")
                       ]);

            var rn2  = rn1.ApplyChangeSet(RoamingNetworkChangeSet.Parse(cs1.ToJSON().ToString()));
            var gef2 = rn2.GetChargingStationOperatorById(gef.Id)!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(gef2.Telephone,                               Is.EqualTo(PhoneNumber.Parse("+49 30 7654321")));
                Assert.That(gef2.Name[Languages.en],                      Is.EqualTo("GraphDefined GmbH"));
                Assert.That(gef2.DataLicenses.Select(l => l.Id.ToString()),  Is.EqualTo(new[] { "ODbL" }));
                Assert.That(gef2.Homepage,                                Is.Null);
                Assert.That(gef2.EMailAddress,                            Is.EqualTo(gef.EMailAddress));
                Assert.That(rn2.ETags,                                    Is.EqualTo(cs1.AfterETags));

                // The changes remember the values they found.
                Assert.That(cs1.Changes[0].OldValue!.Value<String>(),     Is.EqualTo(gef.Telephone.ToString()));
                Assert.That(cs1.Changes[3].OldValue!.Value<String>(),     Is.EqualTo(gef.Homepage.ToString()));

                // The other operator and the old version stay as they were.
                Assert.That(rn2.GetChargingStationOperatorById(ChargingStationOperator_Id.Parse("DE*BDO"))!.ToEmbeddedJSON(),
                            Is.EqualTo(rn1.GetChargingStationOperatorById(ChargingStationOperator_Id.Parse("DE*BDO"))!.ToEmbeddedJSON()).Using<JToken>(JToken.DeepEquals));
                Assert.That(gef.Telephone,                                Is.EqualTo(PhoneNumber.Parse("+49 3641 123456")));

            }

        }

        #endregion

        #region Invalid_operator_property_changes_are_refused()

        [Test]
        public void Invalid_operator_property_changes_are_refused()
        {

            var rn1  = NewNetwork();
            var gef  = rn1.GetChargingStationOperatorById(ChargingStationOperator_Id.Parse("DE*GEF"))!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(gef, "@id", null, "DE*XYZ") ]),
                            Throws.ArgumentException.With.Message.Contains("'@id' of a charging station operator can not be changed"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(gef, "unknown", null, "x") ]),
                            Throws.ArgumentException.With.Message.Contains("'unknown' of a charging station operator can not be changed"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(gef, "telephone", null, "no phone number") ]),
                            Throws.ArgumentException.With.Message.Contains("telephone number"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.RemoveProperty(gef, "name") ]),
                            Throws.ArgumentException.With.Message.Contains("can not be removed, only changed"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.RemoveProperty(gef, "homepage"), RoamingNetworkChange.RemoveProperty(gef, "homepage") ]),
                            Throws.ArgumentException.With.Message.Contains("Change 1: the property 'homepage' of the charging station operator 'DE*GEF' does not exist"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty("ChargingStationOperator", "DE*XYZ", "telephone", null, "+49 30 1234") ]),
                            Throws.ArgumentException.With.Message.Contains("'DE*XYZ' does not exist"));

            }

        }

        #endregion

        #region Concurrent_operator_property_changes()

        [Test]
        public void Concurrent_operator_property_changes()
        {

            var rn1  = NewNetwork();
            var gef  = rn1.GetChargingStationOperatorById(ChargingStationOperator_Id.Parse("DE*GEF"))!;

            var csA  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(gef, "telephone", null, "+49 30 1111111") ]);
            var csB  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(gef, "homepage",  null, "https://example.com") ]);
            var csC  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(gef, "telephone", null, "+49 30 2222222") ]);

            var rn2  = rn1.ApplyChangeSet(csA);

            using (Assert.EnterMultipleScope())
            {

                // B changes another property of the same operator and can be prepared again...
                var rn3 = rn2.ApplyChangeSet(rn2.CreateChangeSet(csB.Changes));
                Assert.That(rn3.GetChargingStationOperatorById(gef.Id)!.Telephone,  Is.EqualTo(PhoneNumber.Parse("+49 30 1111111")));
                Assert.That(rn3.GetChargingStationOperatorById(gef.Id)!.Homepage,   Is.EqualTo(URL.Parse("https://example.com")));

                // ...C changes the same property and conflicts.
                Assert.That(() => rn2.CreateChangeSet(csC.Changes),
                            Throws.ArgumentException.With.Message.Contains("'telephone' of the charging station operator 'DE*GEF' is no longer the expected old value"));

            }

        }

        #endregion

        #region Network_data_licenses_are_updated_and_removed()

        [Test]
        public void Network_data_licenses_are_updated_and_removed()
        {

            var rn1       = NewNetwork();

            var cs1       = rn1.CreateChangeSet([
                                RoamingNetworkChange.UpdateProperty(rn1, "dataLicenses", null, new JArray(DataLicense.CreativeCommons_BY_4.ToPOIJSON(), OwnLicense.ToPOIJSON()))
                            ]);

            var rn2       = rn1.ApplyChangeSet(RoamingNetworkChangeSet.Parse(cs1.ToJSON().ToString()));

            var cs2       = rn2.CreateChangeSet([ RoamingNetworkChange.RemoveProperty(rn2, "dataLicenses") ]);
            var rn3       = rn2.ApplyChangeSet(cs2);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn2.DataLicenses.Ordered().Select(l => l.Id.ToString()),  Is.EqualTo(new[] { "CC BY 4.0", "own-1.0" }));
                Assert.That(rn2.ETags,                                                 Is.EqualTo(cs1.AfterETags));
                Assert.That(cs1.Changes[0].OldValue,                                   Is.EqualTo(rn1.DataLicenses.ToPOIJSON()).Using<JToken>(JToken.DeepEquals));

                Assert.That(rn3.DataLicenses,                                          Is.Empty);
                Assert.That(rn3.ToJSON().ContainsKey("dataLicenses"),                  Is.False);
                Assert.That(cs2.Changes[0].OldValue,                                   Is.EqualTo(rn2.DataLicenses.ToPOIJSON()).Using<JToken>(JToken.DeepEquals));

                // The old version stays as it was.
                Assert.That(rn1.DataLicenses.Ordered().Select(l => l.Id.ToString()),  Is.EqualTo(new[] { "ODbL", "own-1.0" }));

            }

        }

        #endregion

        #region Invalid_network_data_license_changes_are_refused()

        [Test]
        public void Invalid_network_data_license_changes_are_refused()
        {

            var rn1  = NewNetwork();
            var rn0  = new RoamingNetwork(Id: RoamingNetwork_Id.Parse("rn1"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataLicenses", null, "ODbL") ]),
                            Throws.ArgumentException.With.Message.Contains("must be a JSON array"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataLicenses", null, new JArray(new JObject(new JProperty("description", new JObject())))) ]),
                            Throws.ArgumentException.With.Message.Contains("Invalid data license"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataLicenses", null, new JArray(OwnLicense.ToPOIJSON(), OwnLicense.ToPOIJSON())) ]),
                            Throws.ArgumentException.With.Message.Contains("'own-1.0' must not be given twice"));

                Assert.That(() => rn0.CreateChangeSet([ RoamingNetworkChange.RemoveProperty(rn0, "dataLicenses") ]),
                            Throws.ArgumentException.With.Message.Contains("'dataLicenses' does not exist"));

            }

        }

        #endregion

        #region Concurrent_network_data_license_changes_conflict()

        [Test]
        public void Concurrent_network_data_license_changes_conflict()
        {

            var rn1  = NewNetwork();
            var csA  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataLicenses", null, new JArray(DataLicense.CreativeCommons_BY_4.ToPOIJSON())) ]);
            var csB  = rn1.CreateChangeSet([ RoamingNetworkChange.RemoveProperty(rn1, "dataLicenses") ]);

            var rn2  = rn1.ApplyChangeSet(csA);

            Assert.That(() => rn2.CreateChangeSet(csB.Changes),
                        Throws.ArgumentException.With.Message.Contains("'dataLicenses' is no longer the expected old value"));

        }

        #endregion

        #region Concurrent_operator_changes()

        [Test]
        public void Concurrent_operator_changes()
        {

            var rn1     = NewNetwork();
            var bdo     = ChargingStationOperator_Id.Parse("DE*BDO");

            // A replaces DE*BDO, B removes it and C removes DE*GEF, all against the same version.
            var csA     = rn1.CreateChangeSet([ RoamingNetworkChange.Remove(bdo), RoamingNetworkChange.Add(NewOperator("DE*BDO", "Renamed")) ]);
            var csB     = rn1.CreateChangeSet([ RoamingNetworkChange.Remove(bdo) ]);
            var csC     = rn1.CreateChangeSet([ RoamingNetworkChange.Remove(ChargingStationOperator_Id.Parse("DE*GEF")) ]);

            var rn2     = rn1.ApplyChangeSet(csA);

            using (Assert.EnterMultipleScope())
            {

                // B would remove the operator A has replaced.
                Assert.That(() => rn2.CreateChangeSet(csB.Changes),
                            Throws.ArgumentException.With.Message.Contains("'DE*BDO' is no longer the expected old value"));

                // C touches another operator and can be prepared again.
                var rn3 = rn2.ApplyChangeSet(rn2.CreateChangeSet(csC.Changes));
                Assert.That(rn3.ChargingStationOperators.Single().Name[Languages.en],  Is.EqualTo("Renamed"));

            }

        }

        #endregion

        #region One_ETag_per_change_set_side_is_enough()

        [Test]
        public void One_ETag_per_change_set_side_is_enough()
        {

            var rn1   = NewNetwork();
            var cs1   = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "test2") ]);

            var only  = new RoamingNetworkChangeSet(cs1.Id,
                                                    cs1.RoamingNetworkId,
                                                    cs1.CreatedAt,
                                                    cs1.Changes,
                                                    [ cs1.BeforeETags.Single(eTag => eTag.Format == ETagFormat.JSON) ],
                                                    [ cs1.AfterETags. Single(eTag => eTag.Format == ETagFormat.CBOR) ]);

            Assert.That(rn1.TryApplyChangeSet(only, out var rn2, out var error),  Is.True, error);
            Assert.That(rn2?.DataSource,                                          Is.EqualTo("test2"));

        }

        #endregion

        #region ETags_are_checked_with_their_own_hash_algorithm()

        [Test]
        public void ETags_are_checked_with_their_own_hash_algorithm()
        {

            var rn1     = NewNetwork();
            var cs1     = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "test2") ]);
            var rn2     = rn1.ApplyChangeSet(cs1);

            var before  = ETag.Compute(ETagFormat.JSON, ETagHashAlgorithm.SHA512, CanonicalJSON.ToUTF8Bytes(rn1.ToJSON(IncludeETags: false)));
            var after   = ETag.Compute(ETagFormat.CBOR, ETagHashAlgorithm.SHA384, rn2.ToCBOR(IncludeETags: false).ToByteArray(CBORWriterOptions.Canonical));
            var wrong   = ETag.Compute(ETagFormat.JSON, ETagHashAlgorithm.SHA512, CanonicalJSON.ToUTF8Bytes(rn2.ToJSON(IncludeETags: false)));

            // ...and they survive the JSON text of their change set.
            var sha     = RoamingNetworkChangeSet.Parse(new RoamingNetworkChangeSet(cs1.Id, cs1.RoamingNetworkId, cs1.CreatedAt, cs1.Changes, [ before ], [ after ]).
                                                            ToJSON().ToString());

            var stale   = new RoamingNetworkChangeSet(cs1.Id, cs1.RoamingNetworkId, cs1.CreatedAt, cs1.Changes, [ wrong ], [ after ]);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(sha.BeforeETags.Single().HashAlgorithm,                  Is.EqualTo(ETagHashAlgorithm.SHA512));
                Assert.That(sha.AfterETags. Single().Digest.Length,                  Is.EqualTo(48));

                Assert.That(rn1.TryApplyChangeSet(sha,   out var rn3, out var error1),  Is.True, error1);
                Assert.That(rn3?.DataSource,                                            Is.EqualTo("test2"));

                Assert.That(rn1.TryApplyChangeSet(stale, out _,       out var error2),  Is.False);
                Assert.That(error2,                                                     Does.Contain("JSON ETag (SHA512) differs"));

            }

        }

        #endregion

        #region Operators_and_licenses_are_content()

        [Test]
        public void Operators_and_licenses_are_content()
        {

            var rn1       = NewNetwork();
            var eTags     = rn1.CalcETags();
            var json      = rn1.ToJSON(IncludeETags: false);

            // The same operators and licenses, given in another order.
            var reordered = new RoamingNetwork(
                                Id:                        rn1.Id,
                                Name:                      rn1.Name,
                                Description:               rn1.Description,
                                DataSource:                rn1.DataSource,
                                DataLicenses:              rn1.DataLicenses.Reverse(),
                                ChargingStationOperators:  rn1.ChargingStationOperators.Reverse()
                            );

            // One operator with another telephone number.
            var changed   = new RoamingNetwork(
                                Id:                        rn1.Id,
                                Name:                      rn1.Name,
                                Description:               rn1.Description,
                                DataSource:                rn1.DataSource,
                                DataLicenses:              rn1.DataLicenses,
                                ChargingStationOperators:  [ NewOperator("DE*GEF", "GraphDefined"), NewOperator("DE*BDO", "Another name") ]
                            );

            using (Assert.EnterMultipleScope())
            {

                Assert.That(json["chargingStationOperators"]!.Select(o => o["@id"]!.Value<String>()),  Is.EqualTo(new[] { "DE*BDO", "DE*GEF" }));
                Assert.That(json["dataLicenses"]!.            Select(l => l["@id"]!.Value<String>()),  Is.EqualTo(new[] { "ODbL", "own-1.0" }));
                Assert.That(json["dataLicenses"]![1]!["URLs"]!.Values<String>(),                      Is.EqualTo(new[] { "https://example.org/a", "https://example.org/z" }));
                Assert.That(json["chargingStationOperators"]![0]!["dataLicenses"]!.Count(),           Is.EqualTo(2));
                Assert.That(json["chargingStationOperators"]![0]!["roamingNetworkId"],                Is.Null);

                Assert.That(reordered.CalcETags(),  Is.EqualTo(eTags));
                Assert.That(changed.  CalcETags(),  Is.Not.EqualTo(eTags));

                // A change set keeps the operators and licenses.
                var rn2 = rn1.ApplyChangeSet(rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "test2") ]));
                Assert.That(rn2.ChargingStationOperators.Count(),  Is.EqualTo(2));
                Assert.That(rn2.DataLicenses.Count(),              Is.EqualTo(2));

            }

        }

        #endregion

        #region Views_with_ids_only()

        [Test]
        public void Views_with_ids_only()
        {

            var rn1          = NewNetwork();

            var operatorIds  = rn1.ToJSON(ExpandChargingStationOperatorIds: InfoStatus.ShowIdOnly, IncludeETags: false);
            var licenseIds   = rn1.ToJSON(ExpandDataLicenses:               InfoStatus.ShowIdOnly, IncludeETags: false);

            var knownOnly    = new RoamingNetwork(Id: rn1.Id, DataLicenses: [ DataLicense.OpenDatabaseLicense ]);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(operatorIds["chargingStationOperatorIds"]!.Values<String>(),  Is.EqualTo(new[] { "DE*BDO", "DE*GEF" }));

                Assert.That(RoamingNetwork.TryParse(operatorIds, out _, out var error1),  Is.False);
                Assert.That(error1,                                                       Does.Contain("only by their identifications"));

                Assert.That(RoamingNetwork.TryParse(licenseIds,  out _, out var error2),  Is.False);
                Assert.That(error2,                                                       Does.Contain("'own-1.0' is not well-known"));

                // Well-known licenses can be given by their identification alone.
                Assert.That(RoamingNetwork.Parse(knownOnly.ToJSON(ExpandDataLicenses: InfoStatus.ShowIdOnly)).CalcETags(),
                            Is.EqualTo(knownOnly.CalcETags()));

            }

        }

        #endregion

        #region JSON_and_CBOR_carry_the_same_content()

        /// <summary>
        /// One ETag is enough only while JSON and CBOR carry the same content:
        /// a network read back from either representation, and written to the other, keeps its ETags.
        /// </summary>
        [Test]
        public void JSON_and_CBOR_carry_the_same_content()
        {

            var rn1           = NewNetwork();
            var eTags         = rn1.CalcETags();

            var viaJSONtoCBOR = RoamingNetwork.Parse(CBORValue.Parse(RoamingNetwork.Parse(JObject.Parse(rn1.ToJSON().ToString())).
                                                                         ToCBOR().ToByteArray(CBORWriterOptions.Canonical)));

            var viaCBORtoJSON = RoamingNetwork.Parse(JObject.Parse(RoamingNetwork.Parse(CBORValue.Parse(rn1.ToCBOR().ToByteArray(CBORWriterOptions.Canonical))).
                                                                         ToJSON().ToString()));

            var minimal       = new RoamingNetwork(Id: RoamingNetwork_Id.Parse("rn0"));
            var minimalETags  = minimal.CalcETags();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viaJSONtoCBOR.CalcETags(),  Is.EqualTo(eTags));
                Assert.That(viaCBORtoJSON.CalcETags(),  Is.EqualTo(eTags));
                Assert.That(RoamingNetwork.Parse(CBORValue.Parse(minimal.ToCBOR().ToByteArray(CBORWriterOptions.Canonical))).CalcETags(),
                            Is.EqualTo(minimalETags));
                Assert.That(RoamingNetwork.Parse(JObject.Parse(minimal.ToJSON().ToString())).CalcETags(),
                            Is.EqualTo(minimalETags));
            }

        }

        #endregion

        #region A_signed_change_set_needs_a_verifier()

        [Test]
        public void A_signed_change_set_needs_a_verifier()
        {

            var rn1     = NewNetwork();
            var signed  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "test2") ]).
                              Sign(Alice, "alice", COSEAlgorithm.Ed25519);

            var publicKey = Alice.GeneratePublicKey();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn1.TryApplyChangeSet(signed, out _, out var error1),  Is.False);
                Assert.That(error1,                                                Does.Contain("requires a signature verifier"));

                Assert.That(rn1.TryApplyChangeSet(signed, out _, out var error2, (changeSet, signature) => false),  Is.False);
                Assert.That(error2,                                                                                Does.Contain("'alice') is invalid"));

                Assert.That(rn1.TryApplyChangeSet(signed,
                                                  out var rn2,
                                                  out var error3,
                                                  (changeSet, signature) => changeSet.VerifySignature(signature, publicKey, "alice", out _)),
                            Is.True,
                            error3);

                Assert.That(rn2?.DataSource, Is.EqualTo("test2"));

            }

        }

        #endregion

    }

}
