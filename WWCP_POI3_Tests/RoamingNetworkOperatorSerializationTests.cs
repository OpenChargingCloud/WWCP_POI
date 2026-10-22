/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
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
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.Mail;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The roaming network operator in JSON and in CBOR: everything it has is written and read back - its e-mail
    /// addresses among it - and its CBOR, built directly, is byte for byte the CBOR of its JSON, which the ETags of its
    /// roaming networks are calculated over.
    /// </summary>
    [TestFixture]
    public class RoamingNetworkOperatorSerializationTests
    {

        #region Data

        private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-10-19T12:00:00Z");

        #endregion

        #region (static) Full()

        /// <summary>
        /// An operator with everything it can have: the keys of three holders of a quorum of two, a key statement signed
        /// with identity keys of two of them, the certificates of their signing keys, an address, contacts and metadata.
        /// </summary>
        internal static RoamingNetworkOperator Full()
        {

            var network              = Proofs.StationProofTests.NewNetwork();
            var networkOperator      = network.Operator!;

            // A quorum of two of three holders, so that minConsensus is written too.
            var signingKeys          = new[] { "Alpha", "Beta", "Gamma" }.Select(holder => TestParties.QuorumKey(network.Id, holder, POIKeyKind.Signing)). ToArray();
            var identityKeys         = new[] { "Alpha", "Beta", "Gamma" }.Select(holder => TestParties.QuorumKey(network.Id, holder, POIKeyKind.Identity)).ToArray();

            return new RoamingNetworkOperator(
                       networkOperator.Id,
                       Name:                I18NString.Create(Languages.en, "Example Roaming"),
                       Description:         I18NString.Create(Languages.de, "Ein Roaming-Netz zum Testen"),
                       Address:             new Address("Hauptstraße", "07743", I18NString.Create(Languages.de, "Jena"), Country.Germany, HouseNumber: "1"),
                       Homepage:            URL.Parse("https://roaming.example.org"),
                       EMail:               EMailAddress.Parse("info@roaming.example.org"),
                       HotlinePhoneNumber:  PhoneNumber.Parse("+49 3641 123456"),
                       HotlineEMail:        EMailAddress.Parse("Example Hotline <hotline@roaming.example.org>"),
                       SigningKeys:         signingKeys. Select(key => key.Public),
                       IdentityKeys:        identityKeys.Select(key => key.Public),
                       KeyStatement:        RoamingNetworkKeyStatement.Create([ (identityKeys[0].Private, COSEAlgorithm.ES512),
                                                                                (identityKeys[1].Private, COSEAlgorithm.ES512) ],
                                                                              2,
                                                                              network.Id,
                                                                              networkOperator.Id,
                                                                              identityKeys.Select(key => key.Public),
                                                                              T0,
                                                                              T0.AddDays(30)),
                       MinConsensus:        2,
                       SigningKeyCertificates:  signingKeys.Select((key, index) => RoamingNetworkSigningKeyCertificate.Create(identityKeys[index].Private,
                                                                                                                                COSEAlgorithm.ES512,
                                                                                                                                network.Id,
                                                                                                                                networkOperator.Id,
                                                                                                                                key.Public,
                                                                                                                                T0)),
                       DataSource:          "test",
                       Created:             T0,
                       LastChange:          T0.AddHours(1),
                       CustomData:          CustomDataNew.ParseJObject(new JObject(new JProperty("vendor", new JObject(new JProperty("tier", 2)))))
                   );

        }

        #endregion


        #region EveryPropertyIsAPropertyKey()

        /// <summary>
        /// The operator with everything it can have writes only keys of its PropertyKeys and BasePropertyKeys, and every
        /// key of its own PropertyKeys: none of the metadata, the keys or the key statement under a name of its own.
        /// </summary>
        [Test]
        public void EveryPropertyIsAPropertyKey()
        {

            static String[] Keys(Type Type)

                => [ .. Type.GetFields().
                             Where (field => field.IsLiteral).
                             Select(field => (String) field.GetRawConstantValue()!) ];

            var ownKeys      = Keys(typeof(RoamingNetworkOperator.PropertyKeys));
            var allKeys      = ownKeys.Concat(Keys(typeof(RoamingNetworkOperator.BasePropertyKeys))).ToHashSet();

            foreach (var (format, written) in new[] { ("JSON",          Full().ToJSON()),
                                                      ("CBOR",          (JObject) CBORJSON.ToJSON(Full().ToCBOR())),
                                                      ("embedded JSON", Full().ToJSON(Embedded: true)),
                                                      ("embedded CBOR", (JObject) CBORJSON.ToJSON(Full().ToCBOR(Embedded: true))) })
            {

                var names = written.Properties().Select(property => property.Name).ToArray();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(names.Where(name => !allKeys.Contains(name)),  Is.Empty,                                   format);
                    Assert.That(ownKeys,                                       Is.SubsetOf(names),                         format);
                    Assert.That(names.Contains(RoamingNetworkOperator.BasePropertyKeys.Context),  Is.EqualTo(!format.StartsWith("embedded")), format);
                }

            }

        }

        #endregion

        #region TheEMailAddressesAreWrittenAndReadBack()

        [Test]
        public void TheEMailAddressesAreWrittenAndReadBack()
        {

            var json = Full().ToJSON();

            Assert.Multiple(() => {
                Assert.That(json.Value<String>(RoamingNetworkOperator.PropertyKeys.EMailAddress),         Is.EqualTo("info@roaming.example.org"));
                Assert.That(json.Value<String>(RoamingNetworkOperator.PropertyKeys.HotlineEMailAddress),  Is.EqualTo("Example Hotline <hotline@roaming.example.org>"));
                Assert.That(json.Value<String>(RoamingNetworkOperator.PropertyKeys.HotlinePhoneNumber),   Is.Not.Null, "the hotline phone number is where it was");
            });

            Assert.That(RoamingNetworkOperator.TryParse(json, out var again, out var errorResponse), Is.True, errorResponse);

            Assert.Multiple(() => {
                Assert.That(again!.EMail?.ToString(),         Is.EqualTo("info@roaming.example.org"));
                Assert.That(again.HotlineEMail?.ToString(),   Is.EqualTo("Example Hotline <hotline@roaming.example.org>"));
                Assert.That(again.HotlineEMail?.OwnerName,    Is.EqualTo("Example Hotline"));
                Assert.That(again.HotlinePhoneNumber,         Is.EqualTo(PhoneNumber.Parse("+49 3641 123456")));
                Assert.That(JToken.DeepEquals(again.ToJSON(), json), Is.True, "what was read is written as it was read");
            });

        }

        #endregion

        #region AnOperatorWithoutEMailAddressesWritesNone()

        /// <summary>
        /// Absent stays absent: an operator without e-mail addresses is written as before there were any, so that the
        /// ETags of the roaming networks it operates do not change.
        /// </summary>
        [Test]
        public void AnOperatorWithoutEMailAddressesWritesNone()
        {

            var json = new RoamingNetworkOperator(RoamingNetworkOperator_Id.Parse("RNO1"), Created: T0, LastChange: T0).ToJSON();

            Assert.Multiple(() => {
                Assert.That(json.ContainsKey(RoamingNetworkOperator.PropertyKeys.EMailAddress),         Is.False);
                Assert.That(json.ContainsKey(RoamingNetworkOperator.PropertyKeys.HotlineEMailAddress),  Is.False);
            });

        }

        #endregion

        #region AChangeSetSetsTheEMailAddresses()

        /// <summary>
        /// The e-mail addresses are properties a change set of the operator may set, as its hotline is.
        /// </summary>
        [Test]
        public void AChangeSetSetsTheEMailAddresses()
        {

            var history  = RoamingNetworkHistory.Start(Proofs.StationProofTests.NewNetwork());
            var network  = history.Head.RoamingNetwork;

            var result   = history.Commit(network.CreateSignedChangeSet(
                               [ RoamingNetworkChange.UpdateProperty(network.Operator!, RoamingNetworkOperator.PropertyKeys.EMailAddress,         null, "info@roaming.example.org"),
                                 RoamingNetworkChange.UpdateProperty(network.Operator!, RoamingNetworkOperator.PropertyKeys.HotlineEMailAddress,  null, "Example Hotline <hotline@roaming.example.org>") ],
                               CreatedAt: T0));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            Assert.Multiple(() => {
                Assert.That(history.Head.RoamingNetwork.Operator!.EMail?.ToString(),         Is.EqualTo("info@roaming.example.org"));
                Assert.That(history.Head.RoamingNetwork.Operator!.HotlineEMail?.OwnerName,   Is.EqualTo("Example Hotline"));
            });

        }

        #endregion

        #region AnEMailAddressThatIsNoneIsRefused()

        [TestCase(RoamingNetworkOperator.PropertyKeys.EMailAddress,         "no address")]
        [TestCase(RoamingNetworkOperator.PropertyKeys.HotlineEMailAddress,  "hotline@")]
        [TestCase(RoamingNetworkOperator.PropertyKeys.EMailAddress,         42)]
        public void AnEMailAddressThatIsNoneIsRefused(String PropertyName, Object Value)
        {

            var json = Full().ToJSON();
            json[PropertyName] = JToken.FromObject(Value);

            var cbor = CBORJSON.ToCBOR(json);

            Assert.Multiple(() => {
                Assert.That(RoamingNetworkOperator.TryParse(json, out _, out var jsonError),  Is.False);
                Assert.That(jsonError,                                                          Does.Contain("e-mail address"));
                Assert.That(RoamingNetworkOperator.TryParse(cbor, out _, out var cborError),  Is.False);
                Assert.That(cborError,                                                          Does.Contain("e-mail address"));
            });

        }

        #endregion


        #region TheCBORIsTheCBOROfTheJSON()

        /// <summary>
        /// The CBOR, built directly, is byte for byte what converting the JSON gave before: the CBOR ETags of every
        /// roaming network embedding the operator are calculated over these bytes.
        /// </summary>
        [Test]
        public void TheCBORIsTheCBOROfTheJSON()
        {

            foreach (var networkOperator in new[] { Full(),
                                                    new RoamingNetworkOperator(RoamingNetworkOperator_Id.Parse("RNO1"), Created: T0, LastChange: T0) })
            {

                var direct     = networkOperator.ToCBOR().ToByteArray(CBORWriterOptions.Canonical);
                var viaJSON    = CBORJSON.ToCBOR(networkOperator.ToJSON()).ToByteArray(CBORWriterOptions.Canonical);

                Assert.That(Convert.ToHexString(direct), Is.EqualTo(Convert.ToHexString(viaJSON)), networkOperator.Id.ToString());

            }

        }

        #endregion

        #region AnOperatorIsReadBackFromItsCBOR()

        [Test]
        public void AnOperatorIsReadBackFromItsCBOR()
        {

            var networkOperator = Full();
            var cbor            = CBORValue.Parse(networkOperator.ToCBOR().ToByteArray(CBORWriterOptions.Canonical));

            Assert.That(RoamingNetworkOperator.TryParse(cbor, out var again, out var errorResponse), Is.True, errorResponse);

            Assert.Multiple(() => {
                Assert.That(JToken.DeepEquals(again!.ToJSON(), networkOperator.ToJSON()),  Is.True, again.ToJSON().ToString());
                Assert.That(again.KeyStatement,                                             Is.Not.Null);
                Assert.That(again.KeyStatement!.TryVerify(again.IdentityKeys.Select(key => key.Id), again.IdentityKeys, out var error),
                                                                                            Is.True, error);
                Assert.That(again.HotlineEMail?.OwnerName,                                  Is.EqualTo("Example Hotline"));
            });

        }

        #endregion

        #region WhatIsNoOperatorInCBORIsRefused()

        [Test]
        public void WhatIsNoOperatorInCBORIsRefused()
        {

            var withoutId  = CBORJSON.ToCBOR(new JObject(new JProperty(RoamingNetworkOperator.BasePropertyKeys.Name, new JObject(new JProperty("en", "Nobody")))));
            var badKeys    = CBORJSON.ToCBOR(new JObject(new JProperty(RoamingNetworkOperator.BasePropertyKeys.Id, "RNO1"), new JProperty(RoamingNetworkOperator.PropertyKeys.SigningKeys, "no keys")));

            Assert.Multiple(() => {
                Assert.That(RoamingNetworkOperator.TryParse(CBORValue.FromText("RNO1"), out _, out var notAMap),  Is.False);
                Assert.That(notAMap,                                                                                     Does.Contain("must be a map"));
                Assert.That(RoamingNetworkOperator.TryParse(withoutId,                  out _, out var noId),     Is.False);
                Assert.That(noId,                                                                                        Is.Not.Null);
                Assert.That(RoamingNetworkOperator.TryParse(badKeys,                    out _, out var noKeys),   Is.False);
                Assert.That(noKeys,                                                                                      Is.Not.Null);
            });

        }

        #endregion

    }

}
