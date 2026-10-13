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


        #region Party(Network) / Party(EntityType, EntityId)

        /// <summary>
        /// The roaming network operator of the given roaming network.
        /// </summary>
        public static POIPartyId Party(RoamingNetwork Network)
            => new (nameof(RoamingNetwork), Network.Id.ToString());

        /// <summary>
        /// The roaming network operator of the given roaming network.
        /// </summary>
        public static POIPartyId Party(RoamingNetwork_Id NetworkId)
            => new (nameof(RoamingNetwork), NetworkId.ToString());

        /// <summary>
        /// A charging station operator.
        /// </summary>
        public static POIPartyId Party(ChargingStationOperator_Id OperatorId)
            => new (nameof(ChargingStationOperator), OperatorId.ToString());

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


        #region WithKeys(JSON, Party)

        private static JObject WithKeys(JObject     JSON,
                                        POIPartyId  Party)
        {
            JSON[POIPartyKeys.SigningKeysProperty]   = new JArray(Key(Party, POIKeyKind.Signing). Public.ToJSON());
            JSON[POIPartyKeys.IdentityKeysProperty]  = new JArray(Key(Party, POIKeyKind.Identity).Public.ToJSON());
            return JSON;
        }

        #endregion

        #region WithKeys(this Network)

        /// <summary>
        /// The given roaming network with the test keys of its roaming network operator and of all its parties.
        /// Its runtime status is not taken along.
        /// </summary>
        public static RoamingNetwork WithKeys(this RoamingNetwork Network)
        {

            var json = WithKeys(Network.ToJSON(), Party(Network));

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

            return ChangeSet;

        }

        #endregion

        #region CreateSignedChangeSet(this Network, Changes, ...)

        /// <summary>
        /// Create a change set of the given changes against the given network, signed by all its parties with their test keys.
        /// </summary>
        public static RoamingNetworkChangeSet CreateSignedChangeSet(this RoamingNetwork                Network,
                                                                    IEnumerable<RoamingNetworkChange>  Changes,
                                                                    ImmutableI18NString?               Description   = null,
                                                                    JObject?                           Metadata      = null,
                                                                    ChangeSet_Id?                      Id            = null,
                                                                    DateTimeOffset?                    CreatedAt     = null)

            => Network.CreateChangeSet(Changes, Description, Metadata, Id, CreatedAt).
                       SignedByAll(Network);

        #endregion

    }

}
