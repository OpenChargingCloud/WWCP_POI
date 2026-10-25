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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.ChangeSets
{

    /// <summary>
    /// Changes and change sets with JToken values, their JSON and their signatures.
    /// </summary>
    [TestFixture]
    public class ChangeSetTests
    {

        #region Data

        private static readonly Ed25519PrivateKeyParameters Alice = new (Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"), 0);

        private static RoamingNetwork NewNetwork()

            => new (
                   Id:           RoamingNetwork_Id.Parse("rn1"),
                   Name:         I18NString.Create(Languages.en, "Roaming Network 1"),
                   DataSource:   "test"
               );

        private static RoamingNetworkChangeSet NewChangeSet()
        {

            var rn1   = NewNetwork();
            var eTags = rn1.CalcETags();

            return new RoamingNetworkChangeSet(
                       Id:                ChangeSet_Id.Parse("cs1"),
                       RoamingNetworkId:  rn1.Id,
                       CreatedAt:         DateTimeOffset.Parse("2026-10-11T12:34:56.7891234Z"),
                       Changes:           [
                                              RoamingNetworkChange.UpdateProperty(rn1, "dataSource", OldValue: "test", NewValue: "test2"),
                                              RoamingNetworkChange.UpdateProperty(rn1, "name",       OldValue: null,   NewValue: new JObject(new JProperty("en", "Network 1"))),
                                              RoamingNetworkChange.RemoveProperty(rn1, "description")
                                          ],
                       BeforeETags:       eTags,
                       AfterETags:        eTags,
                       Description:       I18NString.Create(Languages.en, "Hello world!"),
                       // A text that looks like a date must stay this exact text.
                       Metadata:          new JObject(new JProperty("validFrom", "2026-01-01T00:00:00Z"))
                   ).WithContentId();

        }

        #endregion


        #region A_change_reads_back_its_JSON()

        [Test]
        public void A_change_reads_back_its_JSON()
        {

            var change = RoamingNetworkChange.UpdateElementProperty(
                             EntityType:    "ChargingStationOperator",
                             EntityId:      "DE*GEF",
                             ElementPath:   [ new POIElementPathSegment("addresses", "home") ],
                             PropertyName:  "city",
                             OldValue:      new JObject(new JProperty("de", "Jena")),
                             NewValue:      new JObject(new JProperty("de", "Erfurt"))
                         );

            var parsed = RoamingNetworkChange.Parse(change.ToJSON());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed,                                          Is.EqualTo(change));
                Assert.That(JToken.DeepEquals(parsed.ToJSON(), change.ToJSON()),  Is.True);
                Assert.That(parsed.ElementPath.Single().ElementId,           Is.EqualTo("home"));
            }

        }

        #endregion

        #region A_change_refuses_JSON_null()

        [Test]
        public void A_change_refuses_JSON_null()
        {

            var rn1 = NewNetwork();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, JValue.CreateNull()),
                            Throws.ArgumentException);

                Assert.That(RoamingNetworkChange.TryParse(JObject.Parse("""
                                {
                                    "kind":          "UpdateProperty",
                                    "entityType":    "RoamingNetwork",
                                    "entityId":      "rn1",
                                    "propertyName":  "dataSource",
                                    "newValue":      null
                                }
                                """), out _, out var error),
                            Is.False);

                Assert.That(error, Does.Contain("RemoveProperty"));

            }

        }

        #endregion

        #region A_change_refuses_an_unknown_kind()

        [Test]
        public void A_change_refuses_an_unknown_kind()
        {

            Assert.That(RoamingNetworkChange.TryParse(JObject.Parse("""{ "kind": "Rename", "entityType": "RoamingNetwork", "entityId": "rn1" }"""),
                                                      out _,
                                                      out var error),
                        Is.False);

            Assert.That(error, Does.Contain("Rename"));

        }

        #endregion

        #region The_values_of_a_change_are_its_own()

        [Test]
        public void The_values_of_a_change_are_its_own()
        {

            var given   = new JObject(new JProperty("en", "Network 1"));
            var change  = RoamingNetworkChange.UpdateProperty(NewNetwork(), "name", null, given);

            given["en"] = "changed by the caller";
            change.NewValue!["en"] = "changed by a reader";

            Assert.That(change.NewValue!["en"]!.Value<String>(), Is.EqualTo("Network 1"));

        }

        #endregion


        #region A_change_set_reads_back_its_JSON_text()

        [Test]
        public void A_change_set_reads_back_its_JSON_text()
        {

            var cs1   = NewChangeSet();
            var text  = cs1.ToJSON().ToString(Newtonsoft.Json.Formatting.Indented);
            var cs2   = RoamingNetworkChangeSet.Parse(text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cs2.Id,                                        Is.EqualTo(cs1.Id));
                Assert.That(cs2.RoamingNetworkId,                          Is.EqualTo(cs1.RoamingNetworkId));
                Assert.That(cs2.CreatedAt,                                 Is.EqualTo(DateTimeOffset.Parse("2026-10-11T12:34:56.789Z")));
                Assert.That(cs2.Changes,                                   Is.EqualTo(cs1.Changes));
                Assert.That(cs2.BeforeETags,                               Is.EqualTo(cs1.BeforeETags));
                Assert.That(cs2.AfterETags,                                Is.EqualTo(cs1.AfterETags));
                Assert.That(cs2.Description,                               Is.EqualTo(cs1.Description));
                Assert.That(cs2.Metadata["validFrom"]!.Type,               Is.EqualTo(JTokenType.String));
                Assert.That(CanonicalJSON.Serialize(cs2.ToJSON()),         Is.EqualTo(CanonicalJSON.Serialize(cs1.ToJSON())));
            }

        }

        #endregion

        #region A_change_set_refuses_an_invalid_change()

        [Test]
        public void A_change_set_refuses_an_invalid_change()
        {

            var json = NewChangeSet().ToJSON();
            ((JArray) json["changes"]!)[1]["kind"] = "Rename";

            Assert.That(RoamingNetworkChangeSet.TryParse(json, out _, out var error), Is.False);
            Assert.That(error,                                                        Does.Contain("Invalid change 1"));

        }

        #endregion

        #region A_signed_change_set_reads_back_its_CBOR()

        [Test]
        public void A_signed_change_set_reads_back_its_CBOR()
        {

            var rn1     = NewNetwork();
            var eTags   = rn1.CalcETags();

            // Integers, floats, texts that look like dates, empty values and an expected absent value.
            var cs1     = new RoamingNetworkChangeSet(
                              Id:                ChangeSet_Id.Parse("cs-cbor"),
                              RoamingNetworkId:  rn1.Id,
                              CreatedAt:         DateTimeOffset.Parse("2026-10-11T12:34:56.789Z"),
                              Changes:           [
                                                     RoamingNetworkChange.UpdateProperty("ChargingStationOperator", "DE*GEF", "geoLocation",
                                                                                         JValue.CreateNull(),
                                                                                         new JObject(new JProperty("lat", 50.9271), new JProperty("lng", 11.5892))),
                                                     RoamingNetworkChange.UpdateProperty(rn1, "dataLicenses", null, new JArray()),
                                                     RoamingNetworkChange.UpdateProperty(rn1, "customData",   null, new JObject(new JProperty("count", 42), new JProperty("since", "2026-01-01T00:00:00Z")))
                                                 ],
                              BeforeETags:       eTags,
                              AfterETags:        eTags,
                              Description:       I18NString.Create(Languages.en, "Hello CBOR!"),
                              Metadata:          new JObject(new JProperty("validFrom", "2026-01-01T00:00:00Z"))
                          ).WithContentId().Sign(Alice, "alice", COSEAlgorithm.Ed25519);

            var bytes   = cs1.ToCBOR().ToByteArray(CBORWriterOptions.Canonical);
            var cs2     = RoamingNetworkChangeSet.ParseCBOR(bytes);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(CanonicalJSON.Serialize(cs2.ToJSON()),  Is.EqualTo(CanonicalJSON.Serialize(cs1.ToJSON())));
                Assert.That(cs2.Changes,                            Is.EqualTo(cs1.Changes));
                Assert.That(cs2.Changes[0].OldValue!.Type,          Is.EqualTo(JTokenType.Null));
                Assert.That(cs2.Metadata["validFrom"]!.Type,        Is.EqualTo(JTokenType.String));

                Assert.That(cs2.VerifySignature(cs2.Signatures[0], Alice.GeneratePublicKey(), "alice", out var error),  Is.True, error);

                // CBOR is smaller than JSON text.
                Assert.That(bytes.Length,  Is.LessThan(System.Text.Encoding.UTF8.GetByteCount(cs1.ToJSON().ToString(Newtonsoft.Json.Formatting.None))));

            }

        }

        #endregion

        #region Limits_are_checked_while_reading()

        [Test]
        public void Limits_are_checked_while_reading()
        {

            var cs1    = NewChangeSet().Sign(Alice, "alice", COSEAlgorithm.Ed25519).Sign(Alice, "alice2", COSEAlgorithm.Ed25519);
            var text   = cs1.ToJSON().ToString();
            var bytes  = cs1.ToCBOR().ToByteArray(CBORWriterOptions.Canonical);

            using (Assert.EnterMultipleScope())
            {

                // Within the default limits.
                Assert.That(RoamingNetworkChangeSet.TryParse    (text,  out _, out var error0),                                                        Is.True, error0);
                Assert.That(RoamingNetworkChangeSet.TryParseCBOR(bytes, out _, out var error1),                                                        Is.True, error1);

                Assert.That(RoamingNetworkChangeSet.TryParse    (text,  out _, out var error2, new RoamingNetworkChangeSetLimits { MaxBytes = 100 }),  Is.False);
                Assert.That(error2,                                                                                                                    Does.Contain("limit of 100 bytes"));

                Assert.That(RoamingNetworkChangeSet.TryParseCBOR(bytes, out _, out var error3, new RoamingNetworkChangeSetLimits { MaxBytes = 100 }),  Is.False);
                Assert.That(error3,                                                                                                                    Does.Contain("limit of 100 bytes"));

                Assert.That(RoamingNetworkChangeSet.TryParse    (text,  out _, out var error4, new RoamingNetworkChangeSetLimits { MaxChanges = 2 }),  Is.False);
                Assert.That(error4,                                                                                                                    Does.Contain("limit of 2 changes"));

                Assert.That(RoamingNetworkChangeSet.TryParseCBOR(bytes, out _, out var error5, new RoamingNetworkChangeSetLimits { MaxSignatures = 1 }),  Is.False);
                Assert.That(error5,                                                                                                                    Does.Contain("limit of 1 signatures"));

                // The changes are 3 levels deep: change set, changes array, change, its value.
                Assert.That(RoamingNetworkChangeSet.TryParse    (text,  out _, out _,          new RoamingNetworkChangeSetLimits { MaxDepth = 3 }),    Is.False);
                Assert.That(RoamingNetworkChangeSet.TryParseCBOR(bytes, out _, out _,          new RoamingNetworkChangeSetLimits { MaxDepth = 3 }),    Is.False);
                Assert.That(RoamingNetworkChangeSet.TryParse    (cs1.ToJSON(), out _, out var error6, new RoamingNetworkChangeSetLimits { MaxDepth = 3 }),  Is.False);
                Assert.That(error6,                                                                                                                    Does.Contain("nesting depth of 3"));

            }

        }

        #endregion

        #region A_change_set_is_identified_by_its_content()

        /// <summary>
        /// A change set is identified by the SHA-256 of its canonical JSON with its context, without its identification and its
        /// signatures: the same content has the same identification, another content another one, and signing it changes it not.
        /// </summary>
        [Test]
        public void A_change_set_is_identified_by_its_content()
        {

            var rn1     = NewNetwork();
            var at      = DateTimeOffset.Parse("2026-10-25T12:00:00Z");
            var cs1     = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "A") ], CreatedAt: at);
            var same    = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "A") ], CreatedAt: at);
            var other   = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "B") ], CreatedAt: at);
            var signed  = cs1.Sign(Alice, "alice", COSEAlgorithm.Ed25519);

            var content = signed.ToJSON();
            content.Remove("@id");
            content.Remove("signatures");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(cs1.Id.ToString(),           Does.Match("^[0-9a-f]{64}$"));
                Assert.That(cs1.Id.ToString(),           Is.EqualTo(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(CanonicalJSON.ToUTF8Bytes(content)))));
                Assert.That(cs1.IdentificationError(),   Is.Null);
                Assert.That(same.Id,                     Is.EqualTo(cs1.Id));
                Assert.That(other.Id,                    Is.Not.EqualTo(cs1.Id));
                Assert.That(signed.Id,                   Is.EqualTo(cs1.Id));
                Assert.That(signed.ContentId,            Is.EqualTo(cs1.Id));
                Assert.That(signed.IdentificationError(), Is.Null);

            }

        }

        #endregion

        #region A_signature_survives_the_JSON_text()

        [Test]
        public void A_signature_survives_the_JSON_text()
        {

            var signed     = NewChangeSet().Sign(Alice, "alice", COSEAlgorithm.Ed25519);
            var parsed     = RoamingNetworkChangeSet.Parse(signed.ToJSON().ToString());

            // Changed, it is no longer identified by its content; identified anew, its signature signs the one before.
            var tampered   = signed.ToJSON();
            tampered["description"]!["en"] = "Hello moon!";

            var forged     = RoamingNetworkChangeSet.Parse(new RoamingNetworkChangeSet(signed.Id, signed.RoamingNetworkId, signed.CreatedAt, signed.Changes, signed.BeforeETags, signed.AfterETags,
                                                                                       ImmutableI18NString.Create(Languages.en, "Hello moon!"), signed.Metadata, Preconditions: signed.Preconditions).
                                                               WithContentId().
                                                               WithSignatures(signed.Signatures).
                                                               ToJSON().ToString());

            using (Assert.EnterMultipleScope())
            {

                Assert.That(RoamingNetworkChangeSet.TryParse(tampered.ToString(), out _, out var tamperedError),  Is.False);
                Assert.That(tamperedError,                                                                        Does.Contain("is not identified by its content"));

                Assert.That(parsed.Signatures,  Is.EqualTo(signed.Signatures));

                Assert.That(parsed.VerifySignature(parsed.Signatures[0], Alice.GeneratePublicKey(), "alice", out var error1),  Is.True,  error1);
                Assert.That(forged.VerifySignature(forged.Signatures[0], Alice.GeneratePublicKey(), "alice", out var error2),  Is.False);
                Assert.That(parsed.VerifySignature(parsed.Signatures[0], Alice.GeneratePublicKey(), "bob",   out var error3),  Is.False);

            }

        }

        #endregion

    }

}
