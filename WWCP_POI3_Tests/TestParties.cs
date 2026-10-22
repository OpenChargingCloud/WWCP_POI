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

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Deterministic keys of the parties of test networks, derived from their identifications:
    /// a secp256r1 signing key and a secp521r1 identity key each, and the signing of change sets with them.
    /// </summary>
    public static class TestParties
    {

        #region Data

        private static readonly ConcurrentDictionary<(POIPartyId, POIKeyKind), (ECPrivateKeyParameters Private, POIPublicKey Public)> keys = [];

        #endregion


        #region OperatorId(NetworkId)

        /// <summary>
        /// The identification of the test operator of the given roaming network: every network has one of its own,
        /// with keys of its own.
        /// </summary>
        public static RoamingNetworkOperator_Id OperatorId(RoamingNetwork_Id NetworkId)
            => RoamingNetworkOperator_Id.Parse($"{NetworkId}-RNO");

        #endregion

        #region Party(Network) / Party(EntityType, EntityId)

        /// <summary>
        /// The roaming network operator of the given roaming network: the one it embeds, or its test operator.
        /// </summary>
        public static POIPartyId Party(RoamingNetwork Network)
            => Network.Operator is not null
                   ? Network.OperatorParty
                   : Party(Network.Id);

        /// <summary>
        /// The test operator of the given roaming network.
        /// </summary>
        public static POIPartyId Party(RoamingNetwork_Id NetworkId)
            => new (nameof(RoamingNetworkOperator), OperatorId(NetworkId).ToString());

        /// <summary>
        /// A charging station operator.
        /// </summary>
        public static POIPartyId Party(ChargingStationOperator_Id OperatorId)
            => new (nameof(ChargingStationOperator), OperatorId.ToString());

        /// <summary>
        /// A grid operator.
        /// </summary>
        public static POIPartyId Party(GridOperator_Id OperatorId)
            => new (nameof(GridOperator), OperatorId.ToString());

        /// <summary>
        /// A parking operator.
        /// </summary>
        public static POIPartyId Party(ParkingOperator_Id OperatorId)
            => new (nameof(ParkingOperator), OperatorId.ToString());

        #endregion

        #region Key(Party, Kind)

        /// <summary>
        /// The private key and the public key of the given kind of the given party:
        /// secp256r1 for signing, secp521r1 for its identity.
        /// </summary>
        public static (ECPrivateKeyParameters Private, POIPublicKey Public) Key(POIPartyId  Party,
                                                                                POIKeyKind  Kind = POIKeyKind.Signing)

            => keys.GetOrAdd((Party, Kind), static key => {

                   var curve       = key.Item2 == POIKeyKind.Signing ? COSECurve.P256 : COSECurve.P521;
                   var parameters  = curve.DomainParameters!;
                   var seed        = SHA512.HashData(Encoding.UTF8.GetBytes($"{key.Item1.EntityType}:{key.Item1.EntityId}:{key.Item2}"));
                   var d           = new BigInteger(1, seed).Mod(parameters.N.Subtract(BigInteger.One)).Add(BigInteger.One);
                   var privateKey  = new ECPrivateKeyParameters(d, parameters);

                   return (privateKey, new POIPublicKey(COSEKey.From(privateKey)));

               });

        #endregion


        #region QuorumKey(NetworkId, Holder, Kind = Signing, Number = 1)

        /// <summary>
        /// The private key and the public key of the given kind of the given holder of the quorum of the test operator of
        /// the given roaming network, naming its holder: secp256r1 for signing, secp521r1 for its identity. Another number
        /// is another key of the same holder, e.g. its next one.
        /// </summary>
        public static (ECPrivateKeyParameters Private, POIPublicKey Public) QuorumKey(RoamingNetwork_Id  NetworkId,
                                                                                      String             Holder,
                                                                                      POIKeyKind         Kind     = POIKeyKind.Signing,
                                                                                      Int32              Number   = 1)
        {

            var (privateKey, _) = Key(new POIPartyId(nameof(RoamingNetworkOperator), $"{OperatorId(NetworkId)}/{Holder}/{Number}"), Kind);

            return (privateKey, POIPublicKey.From(privateKey, Holder: Holder));

        }

        #endregion

        #region WithKeys(JSON, Party)

        private static JObject WithKeys(JObject     JSON,
                                        POIPartyId  Party)
        {

            JSON[POIPartyKeys.IdentityKeysProperty]  = new JArray(Key(Party, POIKeyKind.Identity).Public.ToJSON());

            // The signing keys of the roaming network operator are those of its certificates.
            if (Party.EntityType == nameof(RoamingNetworkOperator))
            {
                JSON.Remove(POIPartyKeys.SigningKeysProperty);
                JSON[RoamingNetworkSigningKeyCertificate.JSONProperty] = new JArray(RoamingNetworkSigningKeyCertificate.Create(Key(Party, POIKeyKind.Identity).Private,
                                                                                                                               COSEAlgorithm.ES512,
                                                                                                                               RoamingNetworkOperator_Id.Parse(Party.EntityId),
                                                                                                                               Key(Party, POIKeyKind.Signing).Public,
                                                                                                                               DateTimeOffset.Parse("2026-01-01T00:00:00Z")).ToJSON(Embedded: true));
            }
            else
                JSON[POIPartyKeys.SigningKeysProperty]   = new JArray(Key(Party, POIKeyKind.Signing). Public.ToJSON());

            return JSON;

        }

        #endregion

        #region WithKeys(this Network)

        /// <summary>
        /// The given roaming network with the test keys of its roaming network operator and of all its parties;
        /// a network without an operator gets its test operator. Its runtime status is not taken along.
        /// </summary>
        public static RoamingNetwork WithKeys(this RoamingNetwork Network)
        {

            var json = Network.ToJSON();

            json[RoamingNetwork.PropertyKeys.Operator] = WithKeys(json[RoamingNetwork.PropertyKeys.Operator] as JObject ?? new JObject(new JProperty("@id",      OperatorId(Network.Id).ToString()),
                                                                                                                                      new JProperty("created",  json["created"])),
                                                                 Party(Network));

            foreach (var (array, type) in new[] { ("chargingStationOperators", nameof(ChargingStationOperator)),
                                                  ("eMobilityProviders",       nameof(EMobilityProvider)),
                                                  ("gridOperators",            nameof(GridOperator)),
                                                  ("parkingOperators",         nameof(ParkingOperator)) })
                foreach (var party in (json[array] as JArray ?? []).OfType<JObject>())
                    WithKeys(party, new POIPartyId(type, party["@id"]!.Value<String>()!));

            if (!RoamingNetwork.TryParse(json, out var network, out var error))
                throw new ArgumentException("The roaming network with test keys is invalid: " + error);

            return network;

        }

        #endregion

        #region WithKeys(this Operator / Provider)

        /// <summary>
        /// The given charging station operator with its test keys.
        /// </summary>
        public static ChargingStationOperator WithKeys(this ChargingStationOperator Operator)

            => ChargingStationOperator.TryParse(WithKeys(Operator.ToEmbeddedJSON(), new POIPartyId(nameof(ChargingStationOperator), Operator.Id.ToString())), out var withKeys, out var error)
                   ? withKeys
                   : throw new ArgumentException(error);

        /// <summary>
        /// The given e-mobility provider with its test keys.
        /// </summary>
        public static EMobilityProvider WithKeys(this EMobilityProvider Provider)

            => EMobilityProvider.TryParse(WithKeys(Provider.ToJSON(), new POIPartyId(nameof(EMobilityProvider), Provider.Id.ToString())), out var withKeys, out var error)
                   ? withKeys
                   : throw new ArgumentException(error);

        /// <summary>
        /// The given grid operator with its test keys.
        /// </summary>
        public static GridOperator WithKeys(this GridOperator Operator)

            => GridOperator.TryParse(WithKeys(Operator.ToJSON(), new POIPartyId(nameof(GridOperator), Operator.Id.ToString())), out var withKeys, out var error)
                   ? withKeys
                   : throw new ArgumentException(error);

        /// <summary>
        /// The given parking operator with its test keys.
        /// </summary>
        public static ParkingOperator WithKeys(this ParkingOperator Operator)

            => ParkingOperator.TryParse(WithKeys(Operator.ToJSON(), new POIPartyId(nameof(ParkingOperator), Operator.Id.ToString())), out var withKeys, out var error)
                   ? withKeys
                   : throw new ArgumentException(error);

        #endregion


        #region SignedBy(this ChangeSet, Party, Kind = Signing)

        /// <summary>
        /// The given change set, signed with the test key of the given kind of the given party.
        /// </summary>
        public static RoamingNetworkChangeSet SignedBy(this RoamingNetworkChangeSet  ChangeSet,
                                                       POIPartyId                    Party,
                                                       POIKeyKind                    Kind = POIKeyKind.Signing)
        {

            var (privateKey, publicKey) = Key(Party, Kind);

            return ChangeSet.Sign(privateKey,
                                  publicKey.Id,
                                  Kind == POIKeyKind.Signing ? COSEAlgorithm.ES256 : COSEAlgorithm.ES512);

        }

        #endregion

        #region SignedByAll(this ChangeSet, Network)

        /// <summary>
        /// The given change set, signed with the test keys of every party of the given network
        /// holding them, signing and identity keys: whatever it changes, its owners signed it.
        /// </summary>
        public static RoamingNetworkChangeSet SignedByAll(this RoamingNetworkChangeSet  ChangeSet,
                                                          RoamingNetwork                Network)
        {

            foreach (var (partyId, party) in Network.Parties)
                foreach (var kind in new[] { POIKeyKind.Signing, POIKeyKind.Identity })
                    if ((kind == POIKeyKind.Signing ? party.SigningKeys : party.IdentityKeys).Any(key => key.Id == Key(partyId, kind).Public.Id))
                        ChangeSet = ChangeSet.SignedBy(partyId, kind);

            return ChangeSet.ProvenByNewIdentityKeys();

        }

        #endregion

        #region ProvenByNewIdentityKeys(this ChangeSet)

        /// <summary>
        /// The given change set, signed with every test key that a change of it enters as an identity key, of a party it
        /// adds or of new identity keys of a party: the proof that the party holds it (SPECIFICATION.md, section 4.1).
        /// </summary>
        public static RoamingNetworkChangeSet ProvenByNewIdentityKeys(this RoamingNetworkChangeSet ChangeSet)
        {

            foreach (var change in ChangeSet.Changes)
            {

                var identityKeys = change.Kind == RoamingNetworkChangeKind.Add && change.NewValue is JObject party
                                       ? party[POIPartyKeys.IdentityKeysProperty] as JArray
                                       : change.Kind == RoamingNetworkChangeKind.UpdateProperty && change.EntityPropertyName == POIPartyKeys.IdentityKeysProperty
                                             ? change.NewValue as JArray
                                             : null;

                foreach (var keyId in (identityKeys ?? []).Select(identityKey => identityKey["@id"]?.Value<String>()))
                {

                    // A test key of any party and kind, as a test may take one for another, e.g. its next identity key.
                    var testKey = keys.FirstOrDefault(key => key.Value.Public.Id == keyId);

                    if (testKey.Value.Private is not null && !ChangeSet.Signatures.Any(signature => signature.KeyId == keyId))
                        ChangeSet = ChangeSet.Sign(testKey.Value.Private,
                                                   testKey.Value.Public.Id,
                                                   testKey.Key.Item2 == POIKeyKind.Signing ? COSEAlgorithm.ES256 : COSEAlgorithm.ES512);

                }

            }

            return ChangeSet;

        }

        #endregion

        #region CreateSignedChangeSet(this Network, Changes, ...)

        /// <summary>
        /// Create a change set of the given changes against the given network, signed by all its parties with their test keys.
        /// The network's JSON is held to whole numbers as integers on the way, see WholeNumbersAreIntegers().
        /// </summary>
        public static RoamingNetworkChangeSet CreateSignedChangeSet(this RoamingNetwork                Network,
                                                                    IEnumerable<RoamingNetworkChange>  Changes,
                                                                    ImmutableI18NString?               Description   = null,
                                                                    JObject?                           Metadata      = null,
                                                                    ChangeSet_Id?                      Id            = null,
                                                                    DateTimeOffset?                    CreatedAt     = null)
        {

            WholeNumbersAreIntegers(Network.ToJSON(), $"The roaming network '{Network.Id}'");

            return Network.CreateChangeSet(Changes, Description, Metadata, Id, CreatedAt).
                           SignedByAll(Network);

        }

        #endregion

        #region SameByValue(Expected, Actual)

        /// <summary>
        /// Whether the given JSON is the same, its numbers compared by what they are worth, whatever their JSON type and
        /// decimal places: a round trip through OCPI or DATEX II may compute a price of 6.0 for the price 6, which is the
        /// same price, though another content.
        /// </summary>
        public static Boolean SameByValue(JToken  Expected,
                                          JToken  Actual)
        {

            static JToken ByValue(JToken JSON)
            {

                var values = JSON is JContainer container
                                 ? container.DescendantsAndSelf().OfType<JValue>().ToArray()
                                 : JSON is JValue single ? [ single ] : [];

                foreach (var value in values)
                    if (value.Type is JTokenType.Integer or JTokenType.Float)
                        value.Value = Convert.ToDouble(value.Value, System.Globalization.CultureInfo.InvariantCulture);

                return JSON;

            }

            return JToken.DeepEquals(ByValue(Expected.DeepClone()), ByValue(Actual.DeepClone()));

        }

        #endregion

        #region WholeNumbersAreIntegers(JSON, What)

        /// <summary>
        /// Fail where the given JSON holds a decimal without decimal places: ToString() would write 22000 as 22000.0, a
        /// decimal with one decimal place, which is read back as another content. Every writer of POI3 writes a whole
        /// number as the integer it is. Asked of every network a test makes a change set of, so that a writer that
        /// forgets it is found by whichever test gives its property a value.
        /// </summary>
        /// <param name="JSON">The JSON, e.g. of a roaming network.</param>
        /// <param name="What">What it is the JSON of, for the failure.</param>
        public static void WholeNumbersAreIntegers(JToken  JSON,
                                                   String  What)
        {

            var values = JSON is JContainer container
                             ? container.DescendantsAndSelf().OfType<JValue>()
                             : JSON is JValue single ? [ single ] : [];

            var decimals = values.Where (value => value.Value is Decimal number && number.Scale == 0).
                                  Select(value => $"'{value.Path}' ({value.Value})").
                                  ToArray();

            if (decimals.Length > 0)
                throw new NUnit.Framework.AssertionException($"{What} writes whole numbers as decimals, which ToString() writes with a decimal place: {String.Join(", ", decimals)}!");

        }

        #endregion

    }

}
