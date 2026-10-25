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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.WWCP.POI.tests.Proofs;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The quorum of a roaming network operator (SPECIFICATION.md, sections 4.1, 8.5 and 8.6): its keys held by several
    /// holders, e.g. companies, of which minConsensus must sign what it owns, its key statement included, and attest a
    /// version, so that none of them changes the network or vouches for it alone; more than half of them, so that two
    /// quorums always share a holder. Holders are counted, not keys.
    /// </summary>
    [TestFixture]
    public class QuorumTests
    {

        #region Data

        private static readonly DateTimeOffset      T0       = DateTimeOffset.Parse("2026-10-21T12:00:00Z");
        private static readonly ChargingStation_Id  S1       = ChargingStation_Id.Parse("DE*GEF*S1");
        private static readonly String[]            Holders  = [ "Alpha", "Beta", "Gamma" ];

        #endregion

        #region (static) Key(Network, Holder, Kind, Number = 1)

        private static (ECPrivateKeyParameters Private, POIPublicKey Public) Key(RoamingNetwork  Network,
                                                                                 String          Holder,
                                                                                 POIKeyKind      Kind,
                                                                                 Int32           Number   = 1)

            => TestParties.QuorumKey(Network.Id, Holder, Kind, Number);

        #endregion

        #region (static) Network(MinConsensus = 2, ExtraSigningKeyOf = null, ExtraIdentityKeyOf = null)

        /// <summary>
        /// The network of the station proof tests, its operator with the signing and identity keys of three holders and
        /// the given minConsensus; optionally one holder with a second signing key, e.g. while it renews its key, and one
        /// with a second identity key, valid from a day before T0 for two years, its first one until 30 days after T0.
        /// </summary>
        private static RoamingNetwork Network(Byte     MinConsensus         = 2,
                                              String?  ExtraSigningKeyOf    = null,
                                              String?  ExtraIdentityKeyOf   = null)
        {

            var network       = StationProofTests.NewNetwork();
            var json          = network.ToJSON(IncludeETags: false);
            var operatorJSON  = (JObject) json[RoamingNetwork.PropertyKeys.Operator]!;

            var signingKeys   = Holders.Select(holder => Key(network, holder, POIKeyKind.Signing).Public).ToList();

            if (ExtraSigningKeyOf is not null)
                signingKeys.Add(Key(network, ExtraSigningKeyOf, POIKeyKind.Signing, 2).Public);

            var identityKeys  = Holders.Select(holder => holder == ExtraIdentityKeyOf
                                                             ? POIPublicKey.From(Key(network, holder, POIKeyKind.Identity).Private, NotAfter: T0.AddDays(30), Holder: holder)
                                                             : Key(network, holder, POIKeyKind.Identity).Public).ToList();

            if (ExtraIdentityKeyOf is not null)
                identityKeys.Add(POIPublicKey.From(Key(network, ExtraIdentityKeyOf, POIKeyKind.Identity, 2).Private,
                                                   NotBefore:  T0.AddDays(-1),
                                                   NotAfter:   T0.AddYears(2),
                                                   Holder:     ExtraIdentityKeyOf));

            operatorJSON[POIPartyKeys.IdentityKeysProperty]  = new JArray(identityKeys.Select(key => key.ToJSON(Embedded: true)));

            // Its signing keys are those of its certificates: each holder certifies its own with its first identity key.
            operatorJSON.Remove(POIPartyKeys.SigningKeysProperty);
            operatorJSON[RoamingNetworkSigningKeyCertificate.JSONProperty] = new JArray(signingKeys.Select(key => Certificate(network, key).ToJSON(Embedded: true)));

            if (MinConsensus > 1)
                operatorJSON[POIPartyKeys.MinConsensusProperty] = MinConsensus;

            return RoamingNetwork.TryParse(json, out var quorumNetwork, out var error)
                       ? quorumNetwork
                       : throw new ArgumentException(error);

        }

        #endregion

        #region (static) Certificate(Network, SigningKey, By = null, Number = 1)

        /// <summary>
        /// The certificate of the given signing key of the operator of the given network by the given identity key of its
        /// holder, the first one by default.
        /// </summary>
        private static RoamingNetworkSigningKeyCertificate Certificate(RoamingNetwork  Network,
                                                                       POIPublicKey    SigningKey,
                                                                       String?         By       = null,
                                                                       Int32           Number   = 1)

            => RoamingNetworkSigningKeyCertificate.Create(Key(Network, By ?? SigningKey.Holder!, POIKeyKind.Identity, Number).Private,
                                                          COSEAlgorithm.ES512,
                                                          TestParties.OperatorId(Network.Id),
                                                          SigningKey,
                                                          T0);

        #endregion

        #region (static) Signed(ChangeSet, Network, Kind, Holders)

        /// <summary>
        /// The given change set signed by the keys of the given kind of the given holders; "Alpha#2" names the second key
        /// of Alpha.
        /// </summary>
        private static RoamingNetworkChangeSet Signed(RoamingNetworkChangeSet  ChangeSet,
                                                      RoamingNetwork           Network,
                                                      POIKeyKind               Kind,
                                                      params String[]          Holders)
        {

            foreach (var holder in Holders)
            {

                var parts                    = holder.Split('#');
                var (privateKey, publicKey)  = Key(Network, parts[0], Kind, parts.Length > 1 ? Int32.Parse(parts[1]) : 1);

                ChangeSet = ChangeSet.Sign(privateKey, publicKey.Id, Kind == POIKeyKind.Signing ? COSEAlgorithm.ES256 : COSEAlgorithm.ES512);

            }

            return ChangeSet;

        }

        #endregion

        #region (static) Statement(Network, Holders)

        /// <summary>
        /// A key statement of the operator of the given network, signed by the identity keys of the given holders.
        /// </summary>
        private static RoamingNetworkKeyStatement Statement(RoamingNetwork   Network,
                                                            params String[]  Holders)

            => RoamingNetworkKeyStatement.Create([ .. Holders.Select(holder => ((Org.BouncyCastle.Crypto.AsymmetricKeyParameter) Key(Network, holder, POIKeyKind.Identity).Private, COSEAlgorithm.ES512)) ],
                                                 Network.Operator!.MinConsensus,
                                                 Network.Id,
                                                 Network.Operator!.Id,
                                                 Network.Operator!.IdentityKeys,
                                                 T0,
                                                 T0.AddDays(30));

        #endregion

        #region (static) HoldersOf(Network)

        /// <summary>
        /// The @ids of the identity keys of the operator of the given network per holder, as a trust anchor names them.
        /// </summary>
        private static String[][] HoldersOf(RoamingNetwork Network)

            => [ .. Network.Operator!.IdentityKeys.GroupBy(key => key.HolderOrUnnamed, StringComparer.Ordinal).
                                                   Select (holder => holder.Select(key => key.Id).ToArray()) ];

        #endregion


        #region The_quorum_needs_more_than_half_of_the_holders()

        /// <summary>
        /// minConsensus is more than half of the holders of each kind of keys of the operator, and at most as many: so 2 of
        /// 3, 3 of 4 or 3 of 5; an odd number of holders is not needed. Keys naming no holder have one.
        /// </summary>
        [Test]
        public void The_quorum_needs_more_than_half_of_the_holders()
        {

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Network(2).Operator!.MinConsensus,  Is.EqualTo(2));
                Assert.That(Network(3).Operator!.MinConsensus,  Is.EqualTo(3));
                Assert.That(() => Network(1),                   Throws.ArgumentException.With.Message.Contains("more than half of the 3 holders"));
                Assert.That(() => Network(4),                   Throws.ArgumentException.With.Message.Contains("as many holders, not of 3"));
                Assert.That(StationProofTests.NewNetwork().Operator!.MinConsensus,  Is.EqualTo(1), "the keys of one holder, naming none");
                Assert.That(Network(2).Operator!.ToJSON()[POIPartyKeys.MinConsensusProperty]!.Value<Int32>(),             Is.EqualTo(2));
                Assert.That(StationProofTests.NewNetwork().Operator!.ToJSON().ContainsKey(POIPartyKeys.MinConsensusProperty),  Is.False, "written unless one");
            }

        }

        #endregion

        #region A_change_of_the_operator_needs_its_quorum()

        /// <summary>
        /// What the operator owns, e.g. the roaming network itself, needs signing keys of two of its three holders: one is
        /// not enough, nor one holder signing with two keys of its own.
        /// </summary>
        [Test]
        public void A_change_of_the_operator_needs_its_quorum()
        {

            var network    = Network(2, ExtraSigningKeyOf: "Alpha");
            var changeSet  = network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network, "dataSource", null, "quorum") ], CreatedAt: T0);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(network.TryApplyChangeSet(Signed(changeSet, network, POIKeyKind.Signing, "Alpha"),            out _, out var error1),  Is.False);
                Assert.That(error1,  Does.Contain("needs signatures by signing keys of 2 holders of"));

                Assert.That(network.TryApplyChangeSet(Signed(changeSet, network, POIKeyKind.Signing, "Alpha", "Alpha#2"), out _, out var error2),  Is.False, "one holder, two keys");
                Assert.That(error2,  Does.Contain("needs signatures by signing keys of 2 holders of"));

                Assert.That(network.TryApplyChangeSet(Signed(changeSet, network, POIKeyKind.Signing, "Alpha", "Gamma"),   out var changed, out var error3),  Is.True, error3);
                Assert.That(changed!.DataSource,  Is.EqualTo("quorum"));

            }

        }

        #endregion

        #region Who_is_a_member_needs_the_quorum(Member)

        /// <summary>
        /// What the operator adds and removes (SPECIFICATION.md, section 4.1): the parties, charging station operators,
        /// e-mobility providers, grid operators and parking operators, with their keys, and the manufacturers.
        /// </summary>
        public static IEnumerable<TestCaseData> Members()
        {

            yield return new TestCaseData("DE*NEW",    (Func<RoamingNetworkChange>) (() => RoamingNetworkChange.Add(new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*NEW"), Name: I18NString.Create(Languages.en, "A new operator"), Created: T0).WithKeys())),
                                                       (Func<RoamingNetworkChange>) (() => RoamingNetworkChange.Remove(ChargingStationOperator_Id.Parse("DE*NEW")))).
                             SetName("Who_is_a_member_needs_the_quorum(ChargingStationOperator)");

            yield return new TestCaseData("DE-NEW",    (Func<RoamingNetworkChange>) (() => RoamingNetworkChange.Add(new EMobilityProvider(EMobilityProvider_Id.Parse("DE-NEW"), Name: I18NString.Create(Languages.en, "A new provider"), Created: T0).WithKeys())),
                                                       (Func<RoamingNetworkChange>) (() => RoamingNetworkChange.Remove(EMobilityProvider_Id.Parse("DE-NEW")))).
                             SetName("Who_is_a_member_needs_the_quorum(EMobilityProvider)");

            yield return new TestCaseData("DE*NEW",    (Func<RoamingNetworkChange>) (() => RoamingNetworkChange.Add(new GridOperator(GridOperator_Id.Parse("DE*NEW"), Name: I18NString.Create(Languages.en, "A new grid operator"), Created: T0).WithKeys())),
                                                       (Func<RoamingNetworkChange>) (() => RoamingNetworkChange.Remove(GridOperator_Id.Parse("DE*NEW")))).
                             SetName("Who_is_a_member_needs_the_quorum(GridOperator)");

            yield return new TestCaseData("DE*NEW",    (Func<RoamingNetworkChange>) (() => RoamingNetworkChange.Add(new ParkingOperator(ParkingOperator_Id.Parse("DE*NEW"), Name: I18NString.Create(Languages.en, "A new parking operator"), Created: T0).WithKeys())),
                                                       (Func<RoamingNetworkChange>) (() => RoamingNetworkChange.Remove(ParkingOperator_Id.Parse("DE*NEW")))).
                             SetName("Who_is_a_member_needs_the_quorum(ParkingOperator)");

            yield return new TestCaseData("NEWCO",     (Func<RoamingNetworkChange>) (() => RoamingNetworkChange.Add(new ChargingStationManufacturer(ChargingStationManufacturer_Id.Parse("NEWCO"), Name: I18NString.Create(Languages.en, "A new manufacturer"), Created: T0).WithKeys())),
                                                       (Func<RoamingNetworkChange>) (() => RoamingNetworkChange.Remove(ChargingStationManufacturer_Id.Parse("NEWCO")))).
                             SetName("Who_is_a_member_needs_the_quorum(ChargingStationManufacturer)");

            yield return new TestCaseData("NEWMETERS", (Func<RoamingNetworkChange>) (() => RoamingNetworkChange.Add(new EnergyMeterManufacturer(EnergyMeterManufacturer_Id.Parse("NEWMETERS"), Name: ImmutableI18NString.Create(Languages.en, "A new meter manufacturer"), Created: T0).WithKeys())),
                                                       (Func<RoamingNetworkChange>) (() => RoamingNetworkChange.Remove(EnergyMeterManufacturer_Id.Parse("NEWMETERS")))).
                             SetName("Who_is_a_member_needs_the_quorum(EnergyMeterManufacturer)");

        }

        /// <summary>
        /// Who is a member is the operator's (SPECIFICATION.md, section 4.1): adding one, and removing it, needs signing
        /// keys of two of its three holders; one holder alone is not enough, nor with two keys.
        /// </summary>
        [TestCaseSource(nameof(Members))]
        public void Who_is_a_member_needs_the_quorum(String                      Id,
                                                     Func<RoamingNetworkChange>  Add,
                                                     Func<RoamingNetworkChange>  Remove)
        {

            var network      = Network(2, ExtraSigningKeyOf: "Alpha");
            var add          = network.CreateChangeSet([ Add() ], CreatedAt: T0).ProvenByNewIdentityKeys();

            // Whether the network names it: by its @id in its JSON.
            Boolean Names(RoamingNetwork Network)
                => Network.ToJSON(IncludeETags: false).ToString(Newtonsoft.Json.Formatting.None).Contains($"\"@id\":\"{Id}\"");

            Assert.That(Names(network), Is.False);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(network.TryApplyChangeSet(Signed(add, network, POIKeyKind.Signing, "Alpha"),            out _, out var error1),  Is.False, "one holder adds");
                Assert.That(error1,  Does.Contain("needs signatures by signing keys of 2 holders of"));

                Assert.That(network.TryApplyChangeSet(Signed(add, network, POIKeyKind.Signing, "Alpha", "Alpha#2"), out _, out var error2),  Is.False, "one holder, two keys, adds");
                Assert.That(error2,  Does.Contain("needs signatures by signing keys of 2 holders of"));

            }

            Assert.That(network.TryApplyChangeSet(Signed(add, network, POIKeyKind.Signing, "Alpha", "Beta"), out var added, out var error3), Is.True, error3);
            Assert.That(Names(added!), Is.True);

            var remove       = added!.CreateChangeSet([ Remove() ], CreatedAt: T0.AddMinutes(1));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(added.TryApplyChangeSet(Signed(remove, added, POIKeyKind.Signing, "Gamma"),             out _, out var error4),  Is.False, "one holder removes");
                Assert.That(error4,  Does.Contain("needs signatures by signing keys of 2 holders of"));

                Assert.That(added.TryApplyChangeSet(Signed(remove, added, POIKeyKind.Signing, "Gamma", "Gamma"),    out _, out var error5),  Is.False, "one holder, twice, removes");
                Assert.That(error5,  Does.Contain("needs signatures by signing keys of 2 holders of"));

                Assert.That(added.TryApplyChangeSet(Signed(remove, added, POIKeyKind.Signing, "Beta", "Gamma"),     out var removed, out var error6),  Is.True, error6);
                Assert.That(Names(removed!),  Is.False);

            }

        }

        #endregion

        #region New_identity_keys_prove_their_possession()

        /// <summary>
        /// Every identity key a change enters anew signs its change set as well (SPECIFICATION.md, section 4.1): those of
        /// a party added, so that none is added without asking for it, and new ones of a party later, also one valid only
        /// later; the proof of possession only, no signature of an owner. A party asking to be added signs the change set
        /// first, and the holders of the operator then as their quorum needs.
        /// </summary>
        [Test]
        public void New_identity_keys_prove_their_possession()
        {

            var network      = Network(2);
            var newId        = ChargingStationOperator_Id.Parse("DE*NEW");
            var newParty     = TestParties.Party(newId);
            var secondKey    = COSEKeyPair.NewEd448();
            var newOperator  = new ChargingStationOperator(newId,
                                                           Name:          I18NString.Create(Languages.en, "A new operator"),
                                                           SigningKeys:   [ TestParties.Key(newParty).Public ],
                                                           IdentityKeys:  [ TestParties.Key(newParty, POIKeyKind.Identity).Public, POIPublicKey.From(secondKey.Private) ],
                                                           Created:       T0);

            var add          = network.CreateChangeSet([ RoamingNetworkChange.Add(newOperator) ], CreatedAt: T0);

            // Signed by the new party with both its identity keys: the proof that it holds them, and that it asks.
            var askedFor     = add.SignedBy(newParty, POIKeyKind.Identity).
                                   Sign(secondKey.Private, POIPublicKey.From(secondKey.Private).Id, secondKey.Algorithm);

            using (Assert.EnterMultipleScope())
            {

                // The quorum of the operator alone: no party is added without asking for it.
                Assert.That(network.TryApplyChangeSet(Signed(add, network, POIKeyKind.Signing, "Alpha", "Beta"), out _, out var error1),  Is.False);
                Assert.That(error1,  Does.Contain("needs a signature by the new identity key").And.Contain("of the ChargingStationOperator 'DE*NEW', as the proof that it holds it"));

                // One of its two identity keys: the other is missing.
                Assert.That(network.TryApplyChangeSet(Signed(add.SignedBy(newParty, POIKeyKind.Identity), network, POIKeyKind.Signing, "Alpha", "Beta"), out _, out var error2),  Is.False);
                Assert.That(error2,  Does.Contain($"the new identity key '{POIPublicKey.From(secondKey.Private).Id}'"));

                // Asked for, but not decided: its own signatures are no signatures of an owner.
                Assert.That(network.TryApplyChangeSet(askedFor,                                                out _, out var error3),  Is.False);
                Assert.That(error3,  Does.Contain("needs signatures by signing keys of 2 holders of"));

                Assert.That(network.TryApplyChangeSet(Signed(askedFor, network, POIKeyKind.Signing, "Alpha"), out _, out var error4),  Is.False);
                Assert.That(error4,  Does.Contain("needs signatures by signing keys of 2 holders of"));

                // While it waits, what is missing is named, the proofs included.
                Assert.That(network.TryCheckCoSigning(Signed(add, network, POIKeyKind.Signing, "Alpha"), out var missing, out var signedByAnOwner, out var error5),  Is.True, error5);
                Assert.That(signedByAnOwner,  Is.True);
                Assert.That(missing,          Has.Count.EqualTo(3));

            }

            Assert.That(network.TryApplyChangeSet(Signed(askedFor, network, POIKeyKind.Signing, "Alpha", "Beta"), out var added, out var error), Is.True, error);
            Assert.That(added!.TryGetChargingStationOperatorById(newId, out var addedOperator), Is.True);
            Assert.That(addedOperator!.IdentityKeys.Select(key => key.Id),  Is.EqualTo(newOperator.IdentityKeys.Select(key => key.Id)));

            // Later, a third identity key, valid only from next year: signed by one of its identity keys, as its owner,
            // and by the new one, as its proof.
            var thirdKey      = COSEKeyPair.NewES512();
            var third         = POIPublicKey.From(thirdKey.Private, NotBefore: T0.AddYears(1), NotAfter: T0.AddYears(3));
            var oldKeys       = new JArray(addedOperator.IdentityKeys.Select(key => key.ToJSON(Embedded: true)));
            var newKeys       = new JArray(addedOperator.IdentityKeys.Append(third).Select(key => key.ToJSON(Embedded: true)));
            var renew         = added.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(addedOperator, POIPartyKeys.IdentityKeysProperty, oldKeys, newKeys) ],
                                                      CreatedAt: T0.AddMinutes(1)).
                                      SignedBy(newParty, POIKeyKind.Identity);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(added.TryApplyChangeSet(renew, out _, out var error6),  Is.False, "without the new key");
                Assert.That(error6,  Does.Contain($"needs a signature by the new identity key '{third.Id}'"));

                Assert.That(added.TryApplyChangeSet(renew.Sign(thirdKey.Private, third.Id, thirdKey.Algorithm), out var renewed, out var error7),  Is.True, error7);
                Assert.That(renewed!.GetChargingStationOperatorById(newId)!.IdentityKeys,  Has.Length.EqualTo(3));

                // The new key alone is not its owner.
                var byNewKeyOnly  = added.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(addedOperator, POIPartyKeys.IdentityKeysProperty, oldKeys, newKeys) ],
                                                          CreatedAt: T0.AddMinutes(1)).
                                          Sign(thirdKey.Private, third.Id, thirdKey.Algorithm);

                Assert.That(added.TryApplyChangeSet(byNewKeyOnly, out _, out var error8),  Is.False, "by the new key alone");
                Assert.That(error8,  Does.Contain("an identity key of the ChargingStationOperator 'DE*NEW'"));

            }

        }

        #endregion

        #region A_holder_renews_its_own_signing_key_alone()

        /// <summary>
        /// Each holder certifies its own signing keys (SPECIFICATION.md, sections 4.1 and 8.6): replacing its signing key
        /// and its certificate needs one of its identity keys only, not the quorum; the keys of another holder, also among
        /// its own, still need the quorum, and so does its signing key alone.
        /// </summary>
        [Test]
        public void A_holder_renews_its_own_signing_key_alone()
        {

            var network      = Network(2);
            var op           = network.Operator!;
            var alpha1       = Key(network, "Alpha", POIKeyKind.Signing).Public;
            var alpha2       = Key(network, "Alpha", POIKeyKind.Signing, 2).Public;
            var beta1        = Key(network, "Beta",  POIKeyKind.Signing).Public;
            var beta2        = Key(network, "Beta",  POIKeyKind.Signing, 2).Public;

            // The signing keys, its certificates, with the given ones replaced.
            RoamingNetworkChangeSet Replacing(params (POIPublicKey Old, POIPublicKey New)[] Keys)

                => network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(op, RoamingNetworkSigningKeyCertificate.JSONProperty, null,
                                                                                 new JArray(op.SigningKeyCertificates.Where (certificate => !Keys.Any(replaced => replaced.Old.Id == certificate.SigningKey.Id)).
                                                                                                                      Concat(Keys.Select(replaced => Certificate(network, replaced.New))).
                                                                                                                      Select(certificate => certificate.ToJSON(Embedded: true)))) ],
                                           CreatedAt: T0);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(network.TryApplyChangeSet(Signed(Replacing((alpha1, alpha2)), network, POIKeyKind.Identity, "Alpha"),  out var renewed, out var error1),  Is.True, error1);
                Assert.That(renewed!.Operator!.SigningKeys,                          Does.Contain(alpha2).And.Not.Contain(alpha1));
                Assert.That(renewed.Operator.SigningKeyCertificates.Select(certificate => certificate.SigningKey),  Does.Contain(alpha2));

                Assert.That(network.TryApplyChangeSet(Signed(Replacing((alpha1, alpha2), (beta1, beta2)), network, POIKeyKind.Identity, "Alpha"),  out _, out var error2),  Is.False, "the key of Beta by Alpha");
                Assert.That(error2,  Does.Contain("2 holders of"));

                Assert.That(network.TryApplyChangeSet(Signed(Replacing((beta1, beta2)), network, POIKeyKind.Identity, "Alpha"),  out _, out _),  Is.False, "the key of Beta by Alpha alone");

                Assert.That(network.TryApplyChangeSet(Signed(Replacing((alpha1, alpha2)), network, POIKeyKind.Signing, "Alpha"),  out _, out _),  Is.False, "by its signing key alone");

                Assert.That(network.TryApplyChangeSet(Signed(Replacing((alpha1, alpha2), (beta1, beta2)), network, POIKeyKind.Identity, "Alpha", "Beta"),  out _, out var error3),  Is.True, error3);

            }

        }

        #endregion

        #region A_holder_with_two_identity_keys_has_one_voice()

        /// <summary>
        /// A holder may name several identity keys, e.g. with overlapping notBefore and notAfter while it replaces one: the
        /// quorum counts holders, not keys, for what minConsensus may be, for a change of the quorum, and for a key
        /// statement.
        /// </summary>
        [Test]
        public void A_holder_with_two_identity_keys_has_one_voice()
        {

            var network  = Network(2, ExtraIdentityKeyOf: "Alpha");
            var toThree  = network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network.Operator!, POIPartyKeys.MinConsensusProperty, 2, 3) ], CreatedAt: T0);

            RoamingNetworkKeyStatement StatementOf(params (String Holder, Int32 Number)[] Signers)

                => RoamingNetworkKeyStatement.Create([ .. Signers.Select(signer => ((Org.BouncyCastle.Crypto.AsymmetricKeyParameter) Key(network, signer.Holder, POIKeyKind.Identity, signer.Number).Private, COSEAlgorithm.ES512)) ],
                                                     network.Operator!.MinConsensus,
                                                     network.Id,
                                                     network.Operator!.Id,
                                                     network.Operator!.IdentityKeys,
                                                     T0,
                                                     T0.AddDays(30));

            // Trusted per holder, as a trust anchor of the operator names them.
            var trusted  = HoldersOf(network);

            using (Assert.EnterMultipleScope())
            {

                // Four identity keys of three holders: 3 of them is the most, and 2 is more than half.
                Assert.That(network.Operator!.IdentityKeys.Length,                      Is.EqualTo(4));
                Assert.That(Network(3, ExtraIdentityKeyOf: "Alpha").Operator!.MinConsensus,  Is.EqualTo(3));
                Assert.That(() => Network(4, ExtraIdentityKeyOf: "Alpha"),              Throws.ArgumentException.With.Message.Contains("as many holders, not of 3"));

                // A change of the quorum: two keys of Alpha are one voice.
                Assert.That(network.TryApplyChangeSet(Signed(toThree, network, POIKeyKind.Identity, "Alpha", "Alpha#2"),  out _, out var error1),  Is.False, "one holder, two identity keys");
                Assert.That(error1,  Does.Contain("identity keys of 2 holders"));

                Assert.That(network.TryApplyChangeSet(Signed(toThree, network, POIKeyKind.Identity, "Alpha#2", "Beta"),   out var changed, out var error2),  Is.True, error2);
                Assert.That(changed!.Operator!.MinConsensus,  Is.EqualTo(3));

                // A key statement: the same.
                Assert.That(StatementOf(("Alpha", 1), ("Alpha", 2)).TryVerify(trusted, null, out _, out var error3, At: T0),  Is.False, "one holder, two identity keys");
                Assert.That(error3,  Does.Contain("of 1 holder, not of 2"));

                Assert.That(StatementOf(("Alpha", 2), ("Gamma", 1)).TryVerify(trusted, null, out _, out var error4, At: T0),  Is.True, error4);

            }

        }

        #endregion

        #region The_quorum_is_changed_by_identity_keys_of_its_holders()

        /// <summary>
        /// The minConsensus of the operator is changed as its keys: by its identity keys, of as many holders as the version
        /// before needs; and only to a quorum the keys allow.
        /// </summary>
        [Test]
        public void The_quorum_is_changed_by_identity_keys_of_its_holders()
        {

            var network    = Network(2);
            var toThree    = network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network.Operator!, POIPartyKeys.MinConsensusProperty, 2, 3) ], CreatedAt: T0);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(network.TryApplyChangeSet(Signed(toThree, network, POIKeyKind.Signing,  "Alpha", "Beta"),  out _, out var error1),  Is.False, "signing keys");
                Assert.That(error1,  Does.Contain("identity keys of 2 holders"));

                Assert.That(network.TryApplyChangeSet(Signed(toThree, network, POIKeyKind.Identity, "Alpha"),          out _, out var error2),  Is.False, "one holder");
                Assert.That(error2,  Does.Contain("identity keys of 2 holders"));

                Assert.That(network.TryApplyChangeSet(Signed(toThree, network, POIKeyKind.Identity, "Alpha", "Beta"),  out var changed, out var error3),  Is.True, error3);
                Assert.That(changed!.Operator!.MinConsensus,  Is.EqualTo(3));

                Assert.That(() => network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network.Operator!, POIPartyKeys.MinConsensusProperty, 2, 1) ], CreatedAt: T0),
                            Throws.ArgumentException.With.Message.Contains("more than half"));

            }

        }

        #endregion

        #region The_operator_recovers_keys_of_a_party_by_its_quorum()

        /// <summary>
        /// The identity keys of the operator recover the keys of other parties: by as many holders as its quorum needs.
        /// </summary>
        [Test]
        public void The_operator_recovers_keys_of_a_party_by_its_quorum()
        {

            var network    = Network(2);
            var gef        = network.GetChargingStationOperatorById(ChargingStationOperator_Id.Parse("DE*GEF"))!;
            var newKey     = TestParties.Key(new POIPartyId(nameof(ChargingStationOperator), "DE*GEF/recovered"), POIKeyKind.Signing).Public;
            var changeSet  = network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(gef, POIPartyKeys.SigningKeysProperty, null, new JArray(newKey.ToJSON(Embedded: true))) ], CreatedAt: T0);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(network.TryApplyChangeSet(Signed(changeSet, network, POIKeyKind.Identity, "Beta"),           out _, out var error1),  Is.False);
                Assert.That(error1,  Does.Contain("identity keys of 2 holders of"));

                Assert.That(network.TryApplyChangeSet(Signed(changeSet, network, POIKeyKind.Identity, "Beta", "Gamma"),  out var changed, out var error2),  Is.True, error2);
                Assert.That(changed!.GetChargingStationOperatorById(gef.Id)!.SigningKeys,  Is.EqualTo(new[] { newKey }));

            }

        }

        #endregion

        #region A_key_statement_is_signed_by_the_quorum()

        /// <summary>
        /// The key statement of an operator with a quorum names its identity keys and its minConsensus and is signed by
        /// identity keys of as many holders (context …/keyStatement/v3): signed by one of them, the version is refused; the
        /// others add their signatures each on its own, and it reads back as written.
        /// </summary>
        [Test]
        public void A_key_statement_is_signed_by_the_quorum()
        {

            var network    = Network(2);
            var byAlpha    = Statement(network, "Alpha");
            var byTwo      = byAlpha.WithSignature(Key(network, "Beta", POIKeyKind.Identity).Private, COSEAlgorithm.ES512);

            RoamingNetworkChangeSet Stating(RoamingNetworkKeyStatement KeyStatement)

                => Signed(network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network.Operator!, RoamingNetworkKeyStatement.JSONProperty, null, KeyStatement.ToJSON(Embedded: true)) ], CreatedAt: T0),
                          network, POIKeyKind.Signing, "Alpha", "Beta");

            Assert.That(RoamingNetworkKeyStatement.TryParse(byTwo.ToJSON(), out var read, out var parseError), Is.True, parseError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(byTwo.ToJSON()["@context"]!.Value<String>(),       Is.EqualTo(RoamingNetworkKeyStatement.JSONLDContext));
                Assert.That(byTwo.ToJSON()["minConsensus"]!.Value<Int32>(),    Is.EqualTo(2));
                Assert.That(byTwo.Signatures.Select(signature => signature.KeyId),  Is.EqualTo(new[] { Key(network, "Alpha", POIKeyKind.Identity).Public.Id, Key(network, "Beta", POIKeyKind.Identity).Public.Id }));
                Assert.That(JToken.DeepEquals(read!.ToJSON(), byTwo.ToJSON()), Is.True);

                Assert.That(byTwo.  TryVerify(network.Operator!.IdentityKeys.Select(key => key.Id), null, out var error1),  Is.True, error1);
                Assert.That(byAlpha.TryVerify(network.Operator!.IdentityKeys.Select(key => key.Id), null, out var error2),  Is.False);
                Assert.That(error2,  Does.Contain("of 1 holder, not of 2"));

                Assert.That(() => Stating(byAlpha),  Throws.ArgumentException.With.Message.Contains("identity keys of 2 of its holders"));
                Assert.That(network.TryApplyChangeSet(Stating(byTwo), out var stated, out var error3),  Is.True, error3);
                Assert.That(stated!.Operator!.KeyStatement!.Signatures,  Has.Length.EqualTo(2));

            }

        }

        #endregion

        #region A_proof_needs_attestations_of_the_quorum()

        /// <summary>
        /// An app pinning the trust anchor of a roaming network with a quorum trusts a proof only when the key statement is
        /// signed by identity keys of minConsensus holders and the root is attested by signing keys of as many.
        /// </summary>
        [Test]
        public void A_proof_needs_attestations_of_the_quorum()
        {

            // Attested now: a history keeps only attestations valid by its clock, so a fixed time in the past would let the
            // attestation of one holder drop the one of another.
            var now = Timestamp.Now;

            RoamingNetworkHistory AttestedBy(params String[] Holders)
            {

                var history    = RoamingNetworkHistory.Start(Network(2));
                var network    = history.Head.RoamingNetwork;
                var statement  = RoamingNetworkKeyStatement.Create([ .. new[] { "Alpha", "Beta" }.Select(holder => ((Org.BouncyCastle.Crypto.AsymmetricKeyParameter) Key(network, holder, POIKeyKind.Identity).Private, COSEAlgorithm.ES512)) ],
                                                                   network.Operator!.MinConsensus,
                                                                   network.Id,
                                                                   network.Operator!.Id,
                                                                   network.Operator!.IdentityKeys,
                                                                   now.AddMinutes(-1),
                                                                   now.AddDays(30));
                var result     = history.Commit(Signed(network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network.Operator!, RoamingNetworkKeyStatement.JSONProperty, null, statement.ToJSON(Embedded: true)) ], CreatedAt: now.AddMinutes(-1)),
                                                       network, POIKeyKind.Signing, "Alpha", "Beta"));

                Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

                history.AttestationSigners = [ .. Holders.Select(holder => new RoamingNetworkCommitSigner(Key(network, holder, POIKeyKind.Signing).Private,
                                                                                                          Key(network, holder, POIKeyKind.Signing).Public.Id,
                                                                                                          COSEAlgorithm.ES256,
                                                                                                          RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
                                                                                                          holder)) ];

                Assert.That(history.TryAttest(out _, out var error, now), Is.True, error);

                return history;

            }

            var at        = now.AddMinutes(1);
            var byOne     = AttestedBy("Alpha");
            var byTwo     = AttestedBy("Alpha", "Gamma");
            var network   = byTwo.Head.RoamingNetwork;
            var anchor    = new RoamingNetworkTrustAnchor(network.Id,
                                                          [ URL.Parse($"https://hub.example/poi/RNs/{network.Id}") ],
                                                          [ .. network.Operator!.IdentityKeys.Select(key => key.Id) ],
                                                          network.Operator!.MinConsensus);

            Assert.That(byOne.TryGetChargingStationProof(S1, out var proofOfOne, out var error, At: at), Is.True, error);
            Assert.That(byTwo.TryGetChargingStationProof(S1, out var proofOfTwo, out error,     At: at), Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(proofOfTwo!.Verify(anchor, out var attestation, out var error1, at),  Is.True, error1);
                Assert.That(attestation,                                                         Is.Not.Null);

                Assert.That(proofOfOne!.Verify(anchor, out _, out var error2, at),  Is.False);
                Assert.That(error2,  Does.Contain("of 1 holder"));

                // A verifier pinning the keys without the minConsensus still needs the one the key statement names.
                Assert.That(proofOfOne.Verify(anchor.Keys, out _, out _, at),  Is.False);

            }

        }

        #endregion

        #region A_trust_anchor_names_the_keys_per_holder()

        /// <summary>
        /// A trust anchor names the identity keys of the operator per holder: one entry for each, the @id of its key, or the
        /// array of the @ids of its keys where it holds several, as in JSON, CBOR and the URL (holders separated by commas,
        /// the keys of one holder by semicolons); its minConsensus counts holders (SPECIFICATION.md, section 8.6).
        /// </summary>
        [Test]
        public void A_trust_anchor_names_the_keys_per_holder()
        {

            var network  = Network(2, ExtraIdentityKeyOf: "Alpha");
            var alpha    = new[] { Key(network, "Alpha", POIKeyKind.Identity).Public.Id, Key(network, "Alpha", POIKeyKind.Identity, 2).Public.Id }.Order(StringComparer.Ordinal).ToArray();
            var url      = URL.Parse($"https://hub.example/poi/RNs/{network.Id}");
            var anchor   = new RoamingNetworkTrustAnchor(network.Id, [ url ], HoldersOf(network), 2);

            Assert.That(RoamingNetworkTrustAnchor.TryParse(anchor.ToQRCode(),           out var fromQRCode, out var error1),  Is.True, error1);
            Assert.That(RoamingNetworkTrustAnchor.TryParse(URL.Parse(anchor.ToURL()),   out var fromURL,    out var error2),  Is.True, error2);
            Assert.That(RoamingNetworkTrustAnchor.TryParse(anchor.ToJSON(),             out var fromJSON,   out var error3),  Is.True, error3);

            var keysJSON = (JArray) anchor.ToJSON()[RoamingNetworkTrustAnchor.PropertyKeys.Keys]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(anchor.Holders,                Has.Count.EqualTo(3));
                Assert.That(anchor.Keys,                   Has.Length.EqualTo(4));
                Assert.That(anchor.Holders.Any(keys => keys.SequenceEqual(alpha)),  Is.True, "Alpha with both its keys");

                Assert.That(keysJSON.Count(holder => holder.Type == JTokenType.Array),   Is.EqualTo(1));
                Assert.That(keysJSON.Count(holder => holder.Type == JTokenType.String),  Is.EqualTo(2));
                Assert.That(anchor.ToURL(),                Does.Contain(String.Join(";", alpha)));

                foreach (var read in new[] { fromQRCode!, fromURL!, fromJSON! })
                    Assert.That(read.Holders.Select(keys => String.Join(";", keys)),  Is.EqualTo(anchor.Holders.Select(keys => String.Join(";", keys))));

                // Each @id once, every holder with a key, and at most as many must sign as there are holders.
                Assert.That(() => new RoamingNetworkTrustAnchor(network.Id, [ url ], [ alpha, [ alpha[0] ] ]),
                            Throws.ArgumentException.With.Message.Contains("each once"));
                Assert.That(() => new RoamingNetworkTrustAnchor(network.Id, [ url ], [ alpha, [] ]),
                            Throws.ArgumentException.With.Message.Contains("every holder with at least one"));
                Assert.That(() => new RoamingNetworkTrustAnchor(network.Id, [ url ], [ alpha ], 2),
                            Throws.ArgumentException.With.Message.Contains("as many holders, not of 1"));

                // Pinned without holders, each key is a holder of its own.
                Assert.That(new RoamingNetworkTrustAnchor(network.Id, [ url ], anchor.Keys, 2).Holders,  Has.Count.EqualTo(4));

            }

        }

        #endregion

        #region A_holder_does_not_count_twice_by_naming_its_key_as_another_s()

        /// <summary>
        /// The holders of a quorum are counted as the verifier knows them, e.g. by its trust anchor, never as the key
        /// statement it checks names them: a holder of two trusted identity keys, which writes a key statement naming one of
        /// them as another holder's and signs it with both, is one holder, and the statement is refused, as it names the
        /// holder of a trusted key otherwise than it is trusted (docs/csharp-review.md of WWCP_POI.TS, finding 1).
        /// </summary>
        [Test]
        public void A_holder_does_not_count_twice_by_naming_its_key_as_another_s()
        {

            var network   = Network(2, ExtraIdentityKeyOf: "Alpha");
            var trusted   = HoldersOf(network);
            var alpha2    = Key(network, "Alpha", POIKeyKind.Identity, 2);

            RoamingNetworkKeyStatement StatementOf(IEnumerable<POIPublicKey> IdentityKeys, params (String Holder, Int32 Number)[] Signers)

                => RoamingNetworkKeyStatement.Create([ .. Signers.Select(signer => ((Org.BouncyCastle.Crypto.AsymmetricKeyParameter) Key(network, signer.Holder, POIKeyKind.Identity, signer.Number).Private, COSEAlgorithm.ES512)) ],
                                                     2,
                                                     network.Id,
                                                     network.Operator!.Id,
                                                     IdentityKeys,
                                                     T0,
                                                     T0.AddDays(30));

            // Alpha names its second identity key as one of Beta.
            var asBeta    = network.Operator!.IdentityKeys.Select(key => key.Id == alpha2.Public.Id
                                                                             ? POIPublicKey.From(alpha2.Private, NotBefore: T0.AddDays(-1), NotAfter: T0.AddYears(2), Holder: "Beta")
                                                                             : key).ToArray();

            var forged    = StatementOf(asBeta,                          ("Alpha", 1), ("Alpha", 2));
            var byAlpha   = StatementOf(network.Operator!.IdentityKeys,  ("Alpha", 1), ("Alpha", 2));
            var byTwo     = StatementOf(network.Operator!.IdentityKeys,  ("Alpha", 2), ("Beta",  1));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(forged. TryVerify(trusted, null, out _, out var error1, 2, T0),  Is.False);
                Assert.That(error1,  Does.Contain("names the holder of the trusted identity key").And.Contain("otherwise than it is trusted"));

                Assert.That(byAlpha.TryVerify(trusted, null, out _, out var error2, 2, T0),  Is.False);
                Assert.That(error2,  Does.Contain("of 1 holder, not of 2"));

                Assert.That(byTwo.  TryVerify(trusted, null, out var holderOf, out var error3, 2, T0),  Is.True, error3);
                Assert.That(holderOf![alpha2.Public.Id],  Is.EqualTo("Alpha"));

                // A verifier pinning the keys of one holder as two refuses a statement naming them as one holder's.
                Assert.That(byTwo.TryVerify(network.Operator!.IdentityKeys.Select(key => key.Id), null, out var error4, 2, T0),  Is.False);
                Assert.That(error4,  Does.Contain("otherwise than it is trusted"));

                // In a version, the statement names the keys as the operator does, with their holders.
                Assert.That(() => network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network.Operator!, RoamingNetworkKeyStatement.JSONProperty, null, StatementOf(asBeta, ("Alpha", 2), ("Beta", 1)).ToJSON(Embedded: true)) ], CreatedAt: T0),
                            Throws.ArgumentException.With.Message.Contains($"names the identity key '{alpha2.Public.Id}', which is not one of its identity keys"));

            }

        }

        #endregion

        #region A_key_keeps_its_holder()

        /// <summary>
        /// A key keeps its holder: a version naming a key of a party with another holder than the version before, also by
        /// the quorum, is refused; a holder handing a key over gives the other a new key instead (SPECIFICATION.md, 4.1).
        /// </summary>
        [Test]
        public void A_key_keeps_its_holder()
        {

            var network    = Network(2, ExtraIdentityKeyOf: "Alpha");
            var alpha2     = Key(network, "Alpha", POIKeyKind.Identity, 2);
            var before     = new JArray(network.Operator!.IdentityKeys.Select(key => key.ToJSON(Embedded: true)));
            var toBeta     = new JArray(network.Operator!.IdentityKeys.Select(key => (key.Id == alpha2.Public.Id
                                                                                          ? POIPublicKey.From(alpha2.Private, NotBefore: T0.AddDays(-1), NotAfter: T0.AddYears(2), Holder: "Beta")
                                                                                          : key).ToJSON(Embedded: true)));
            var changeSet  = network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network.Operator!, POIPartyKeys.IdentityKeysProperty, before, toBeta) ], CreatedAt: T0);

            Assert.That(network.TryApplyChangeSet(Signed(changeSet, network, POIKeyKind.Identity, "Alpha", "Beta", "Gamma"), out _, out var error), Is.False);
            Assert.That(error,  Does.Contain($"The key '{alpha2.Public.Id}' of the RoamingNetworkOperator").And.Contain("keeps its holder 'Alpha', not 'Beta'"));

        }

        #endregion

        #region A_trust_anchor_is_CBOR_in_a_QR_code()

        /// <summary>
        /// The trust anchor of a roaming network with a quorum, with its name, description and further information: the
        /// text of its QR code is WWCP/TA-2: and the Base45 of its CBOR, all of the alphanumeric mode of QR codes, and reads
        /// back as it was; the identity keys are their 32 bytes in CBOR.
        /// </summary>
        [Test]
        public void A_trust_anchor_is_CBOR_in_a_QR_code()
        {

            var network  = Network(2);
            var anchor   = new RoamingNetworkTrustAnchor(network.Id,
                                                         [ URL.Parse($"https://hub.example/poi/RNs/{network.Id}") ],
                                                         [ .. network.Operator!.IdentityKeys.Select(key => key.Id) ],
                                                         2,
                                                         ImmutableI18NString.Create(Languages.de, "Beispielnetz").Set(Languages.en, "Example network"),
                                                         ImmutableI18NString.Create(Languages.en, "Charging stations of the example operators"));

            var qrCode   = anchor.ToQRCode();

            Assert.That(RoamingNetworkTrustAnchor.TryParse(qrCode, out var read, out var error), Is.True, error);
            Assert.That(anchor.ToCBOR().TryGetValue(CBORValue.FromText(RoamingNetworkTrustAnchor.PropertyKeys.Keys), out var identityKeysCBOR), Is.True);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(qrCode,                    Does.StartWith(RoamingNetworkTrustAnchor.QRCodePrefix));
                Assert.That(qrCode.All(character => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:".Contains(character)),  Is.True, "the alphanumeric mode");
                Assert.That(qrCode.Length,             Is.LessThan(1000));

                Assert.That(read!.Id,                  Is.EqualTo(network.Id));
                Assert.That(read.URLs,                 Is.EqualTo(anchor.URLs));
                Assert.That(read.Keys,                 Is.EqualTo(anchor.Keys));
                Assert.That(read.MinConsensus,         Is.EqualTo((Byte) 2));
                Assert.That(read.Name,                 Is.EqualTo(anchor.Name));
                Assert.That(read.Description,          Is.EqualTo(anchor.Description));
                Assert.That(JToken.DeepEquals(read.ToJSON(), anchor.ToJSON()),  Is.True);

                Assert.That(identityKeysCBOR.AsArray().All(id => id.TryGetBytes(out var bytes) && bytes.Length == 32),  Is.True);

                Assert.That(anchor.ToURL(),            Does.EndWith("&minConsensus=2"));
                Assert.That(RoamingNetworkTrustAnchor.TryParse(URL.Parse(anchor.ToURL()), out var fromURI, out error),  Is.True, error);
                Assert.That(fromURI!.MinConsensus,     Is.EqualTo((Byte) 2));

                Assert.That(() => new RoamingNetworkTrustAnchor(anchor.Id, anchor.URLs, anchor.Keys, 4),
                            Throws.ArgumentException.With.Message.Contains("needs identity keys of as many holders, not of 3"));

                var otherNetwork = anchor.ToJSON();
                otherNetwork[RoamingNetworkTrustAnchor.PropertyKeys.Id] = "rn9";
                Assert.That(RoamingNetworkTrustAnchor.TryParse(otherNetwork, out _, out var error2),  Is.False);
                Assert.That(error2,  Does.Contain("names the URL of"));

            }

        }

        #endregion

    }

}
