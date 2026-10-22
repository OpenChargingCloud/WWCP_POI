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

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A party of the roaming network that signs its own changes:
    /// the roaming network operator, charging station operators, e-mobility providers,
    /// grid operators and parking operators.
    /// </summary>
    public interface IPOIParty
    {

        /// <summary>
        /// The short keys signing its change sets and commits, e.g. secp256r1, in the order of their identifications.
        /// </summary>
        ImmutableArray<POIPublicKey>  SigningKeys     { get; }

        /// <summary>
        /// The long-term identity keys, e.g. secp521r1, signing changes of its own keys, in the order of their identifications.
        /// </summary>
        ImmutableArray<POIPublicKey>  IdentityKeys    { get; }

        /// <summary>
        /// How many holders of its keys of one kind sign what it owns: one, unless several holders share its keys as a
        /// quorum, as the roaming network operator may (SPECIFICATION.md, section 4.1).
        /// </summary>
        Byte                          MinConsensus
            => 1;

    }


    /// <summary>
    /// The signing keys and identity keys of a party: their checks and their JSON representation.
    /// </summary>
    public static class POIPartyKeys
    {

        /// <summary>
        /// The JSON key of the signing keys.
        /// </summary>
        public const String SigningKeysProperty   = "signingKeys";

        /// <summary>
        /// The JSON key of the identity keys.
        /// </summary>
        public const String IdentityKeysProperty  = "identityKeys";

        /// <summary>
        /// The JSON key of how many holders of its keys of one kind sign what a party owns.
        /// </summary>
        public const String MinConsensusProperty  = "minConsensus";

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization of the keys of a party.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its signing keys.
            /// </summary>
            public const String  SigningKeys    = SigningKeysProperty;

            /// <summary>
            /// The property of its identity keys.
            /// </summary>
            public const String  IdentityKeys   = IdentityKeysProperty;

        }

        #endregion


        #region IsKeyProperty(PropertyName)

        /// <summary>
        /// Whether the given property holds the keys of a party.
        /// </summary>
        public static Boolean IsKeyProperty(String? PropertyName)

            => PropertyName is PropertyKeys.SigningKeys or PropertyKeys.IdentityKeys;

        #endregion

        #region Check(SigningKeys, IdentityKeys, Party)

        /// <summary>
        /// The given keys in the order of their identifications; a key must not be both a signing and an identity key.
        /// </summary>
        internal static (ImmutableArray<POIPublicKey> SigningKeys, ImmutableArray<POIPublicKey> IdentityKeys) Check(IEnumerable<POIPublicKey>?  SigningKeys,
                                                                                                                    IEnumerable<POIPublicKey>?  IdentityKeys,
                                                                                                                    String                      Party)
        {

            var signingKeys   = POIPublicKey.Ordered(SigningKeys,  $"signing key of {Party}");
            var identityKeys  = POIPublicKey.Ordered(IdentityKeys, $"identity key of {Party}");

            if (signingKeys.Select(key => key.Id).Intersect(identityKeys.Select(key => key.Id)).FirstOrDefault() is String both)
                throw new ArgumentException($"The key '{both}' of {Party} must not be both a signing and an identity key!");

            // Usages restrict what a signing key signs; an identity key signs changes of keys only.
            if (identityKeys.FirstOrDefault(key => !key.Usages.IsEmpty) is POIPublicKey withUsages)
                throw new ArgumentException($"The identity key '{withUsages.Id}' of {Party} names no usages: they are of signing keys!");

            return (signingKeys, identityKeys);

        }

        #endregion

        #region Holders(Keys)

        /// <summary>
        /// The holders of the given keys, as a quorum counts them: the keys naming no holder have one.
        /// </summary>
        public static IReadOnlySet<String> Holders(IEnumerable<POIPublicKey> Keys)

            => Keys.Select(key => key.HolderOrUnnamed).ToHashSet(StringComparer.Ordinal);

        #endregion

        #region CheckQuorum(MinConsensus, SigningKeys, IdentityKeys, Party)

        /// <summary>
        /// The given quorum must fit the given keys of a party: of each kind it has, at least as many holders as must sign,
        /// and more than half of them, so that two quorums always share a holder (SPECIFICATION.md, section 4.1).
        /// </summary>
        internal static void CheckQuorum(Byte                       MinConsensus,
                                         IEnumerable<POIPublicKey>  SigningKeys,
                                         IEnumerable<POIPublicKey>  IdentityKeys,
                                         String                     Party)
        {

            if (MinConsensus < 1)
                throw new ArgumentException($"The minConsensus of {Party} must be at least 1!");

            foreach (var (kind, keys) in new[] { ("signing", SigningKeys), ("identity", IdentityKeys) })
            {

                var holders = Holders(keys).Count;

                if (holders == 0)
                {
                    if (MinConsensus > 1)
                        throw new ArgumentException($"The minConsensus {MinConsensus} of {Party} needs {kind} keys of as many holders!");
                    continue;
                }

                if (MinConsensus > holders)
                    throw new ArgumentException($"The minConsensus {MinConsensus} of {Party} needs {kind} keys of as many holders, not of {holders}!");

                if (2 * MinConsensus <= holders)
                    throw new ArgumentException($"The minConsensus {MinConsensus} of {Party} must be more than half of the {holders} holders of its {kind} keys!");

            }

        }

        #endregion

        #region AddTo(JSON, Party)

        /// <summary>
        /// Add the keys of the given party to its JSON representation, and return it.
        /// </summary>
        internal static JObject AddTo(JObject    JSON,
                                    IPOIParty  Party)
        {

            if (Party.SigningKeys.Length > 0)
                JSON.Add(PropertyKeys.SigningKeys,   new JArray(Party.SigningKeys. Select(key => key.ToJSON(Embedded: true))));

            if (Party.IdentityKeys.Length > 0)
                JSON.Add(PropertyKeys.IdentityKeys,  new JArray(Party.IdentityKeys.Select(key => key.ToJSON(Embedded: true))));

            return JSON;

        }

        #endregion

        #region TryParse(JSON, out SigningKeys, out IdentityKeys, out ErrorResponse)

        /// <summary>
        /// Try to parse the keys of a party within its JSON representation.
        /// </summary>
        internal static Boolean TryParse(JObject                           JSON,
                                       out ImmutableArray<POIPublicKey>  SigningKeys,
                                       out ImmutableArray<POIPublicKey>  IdentityKeys,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            IdentityKeys = [];

            return POIJSON.TryParseObjects(JSON, PropertyKeys.SigningKeys,  "signing key",   POIPublicKey.TryParse, out SigningKeys,  out ErrorResponse) &&
                   POIJSON.TryParseObjects(JSON, PropertyKeys.IdentityKeys, "identity key",  POIPublicKey.TryParse, out IdentityKeys, out ErrorResponse);

        }

        #endregion

    }

}
