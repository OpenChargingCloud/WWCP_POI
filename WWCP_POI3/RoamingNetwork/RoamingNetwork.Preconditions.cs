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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The ETags of the entities of a version, and the preconditions of change sets naming them.
    /// </summary>
    /// <remarks>
    /// The ETag of an entity is the one of its representation as its roaming network embeds it, with everything it
    /// embeds: SHA-256 over its canonical JSON or canonical CBOR, the HTTP ETag POI_API serves it with as it is given,
    /// with ?view=stored for charging pools, stations and EVSEs. A change set may name the ETags entities must still
    /// have: it applies, is rebased onto another version and merged with another tip only while they do, e.g. while
    /// the charging pool a client read and changed is as the client read it.
    /// </remarks>
    public sealed partial class RoamingNetwork
    {

        #region TryGetEntityETag(EntityType, EntityId, out ETag, Format = JSON, HashAlgorithm = SHA256)

        /// <summary>
        /// The ETag of the entity of the given type and identification in this version, as this roaming network
        /// embeds it, e.g. the one of a charging pool with its stations and EVSEs.
        /// </summary>
        /// <param name="EntityType">An entity type, e.g. ChargingPool.</param>
        /// <param name="EntityId">The identification of the entity, in any spelling its type considers equal.</param>
        /// <param name="ETag">Its ETag.</param>
        /// <param name="Format">The format of the ETag.</param>
        /// <param name="HashAlgorithm">The hash algorithm of the ETag.</param>
        public Boolean TryGetEntityETag(String             EntityType,
                                        String             EntityId,
                                        out ETag           ETag,
                                        ETagFormat         Format          = ETagFormat.JSON,
                                        ETagHashAlgorithm  HashAlgorithm   = ETagHashAlgorithm.SHA256)
        {

            if (Flatten(this).TryGetValue(MergeKey.Of(EntityType, EntityId), out var entity))
            {
                ETag = EntityETag(entity.Full, Format, HashAlgorithm);
                return true;
            }

            ETag = default;
            return false;

        }

        #endregion

        #region Precondition    (EntityType, EntityId, Format = JSON)

        /// <summary>
        /// The precondition that the entity of the given type and identification still has the ETag it has in this version.
        /// </summary>
        /// <param name="EntityType">An entity type, e.g. ChargingPool.</param>
        /// <param name="EntityId">The identification of the entity.</param>
        /// <param name="Format">The format of the ETag.</param>
        public RoamingNetworkPrecondition Precondition(String      EntityType,
                                                       String      EntityId,
                                                       ETagFormat  Format   = ETagFormat.JSON)

            => TryGetEntityETag(EntityType, EntityId, out var eTag, Format)
                   ? new RoamingNetworkPrecondition(EntityType, EntityId, eTag)
                   : throw new ArgumentException($"The {EntityType} '{EntityId}' does not exist in this version!", nameof(EntityId));

        #endregion

        #region (internal static) EntitiesOf(Version)

        /// <summary>
        /// The entities of the given version, by their type and identification, as preconditions and merges find them.
        /// </summary>
        internal static IReadOnlyDictionary<MergeKey, MergeEntity> EntitiesOf(RoamingNetwork Version)

            => Flatten(Version);

        #endregion

        #region (internal static) BrokenPreconditions(Preconditions, Base, Other)

        /// <summary>
        /// The given preconditions of a change set of one branch the other branch broke: they name an entity the other
        /// branch changed or removed since the merge base of both, with what it did. A precondition holds in a merge only
        /// while the other branch leaves the entity as the merge base has it.
        /// </summary>
        /// <param name="Preconditions">The preconditions of a change set of one branch.</param>
        /// <param name="Base">The merge base of both branches.</param>
        /// <param name="Other">The entities of the version of the other branch.</param>
        internal static IEnumerable<(RoamingNetworkPrecondition Precondition, String What)> BrokenPreconditions(ImmutableArray<RoamingNetworkPrecondition>   Preconditions,
                                                                                                              MergeBase                                    Base,
                                                                                                              IReadOnlyDictionary<MergeKey, MergeEntity>   Other)
        {

            if (Preconditions.IsDefaultOrEmpty)
                yield break;

            var compared = new HashSet<MergeKey>();

            foreach (var precondition in Preconditions)
            {

                var key = MergeKey.Of(precondition.EntityType, precondition.EntityId);

                // An entity named again is compared once.
                if (!compared.Add(key))
                    continue;

                Base.Entities.TryGetValue(key, out var before);

                if (!Other.TryGetValue(key, out var after))
                    yield return (precondition, "removed it");

                else if (before is null || !JToken.DeepEquals(before.Full, after.Full))
                    yield return (precondition, "changed it");

            }

        }

        #endregion

        #region (private) TryCheckPreconditions(Preconditions, out ErrorResponse)

        /// <summary>
        /// Whether every entity the given preconditions name exists in this version, with the ETag they name.
        /// </summary>
        /// <param name="Preconditions">The preconditions of a change set.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        private Boolean TryCheckPreconditions(ImmutableArray<RoamingNetworkPrecondition>  Preconditions,
                                              [NotNullWhen(false)] out String?            ErrorResponse)
        {

            ErrorResponse = null;

            if (Preconditions.IsDefaultOrEmpty)
                return true;

            var entities  = Flatten(this);

            // An ETag once per entity, format and hash algorithm: an entity named again costs nothing.
            var eTags     = new Dictionary<(MergeKey Key, ETagFormat Format, ETagHashAlgorithm HashAlgorithm), ETag>();

            for (var index = 0; index < Preconditions.Length; index++)
            {

                var precondition  = Preconditions[index];
                var key           = MergeKey.Of(precondition.EntityType, precondition.EntityId);

                if (!entities.TryGetValue(key, out var entity))
                {
                    ErrorResponse = $"Precondition {index}: the {precondition.EntityType} '{precondition.EntityId}' does not exist!";
                    return false;
                }

                var eTagKey = (key, precondition.ETag.Format, precondition.ETag.HashAlgorithm);

                if (!eTags.TryGetValue(eTagKey, out var eTag))
                {
                    eTag = EntityETag(entity.Full, precondition.ETag.Format, precondition.ETag.HashAlgorithm);
                    eTags.Add(eTagKey, eTag);
                }

                if (!eTag.Equals(precondition.ETag))
                {
                    ErrorResponse = $"Precondition {index}: the {precondition.EntityType} '{precondition.EntityId}' changed, it no longer has the ETag {precondition.ETag}!";
                    return false;
                }

            }

            return true;

        }

        #endregion

        #region (private static) EntityETag(JSON, Format, HashAlgorithm)

        /// <summary>
        /// The ETag of the given representation of an entity: over its canonical JSON, or over the canonical CBOR of its keys and values.
        /// </summary>
        private static ETag EntityETag(JObject            JSON,
                                       ETagFormat         Format,
                                       ETagHashAlgorithm  HashAlgorithm)

            => ETag.Compute(Format,
                            HashAlgorithm,
                            Format == ETagFormat.CBOR
                                ? CBORJSON.ToCBOR(JSON).ToByteArray(CBORWriterOptions.Canonical)
                                : CanonicalJSON.ToUTF8Bytes(JSON));

        #endregion

    }

}
