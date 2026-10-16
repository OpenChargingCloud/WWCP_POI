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
    /// embeds: SHA-256 over its canonical JSON or canonical CBOR, the HTTP ETag POI_API serves it with. A change set
    /// may name the ETags entities must still have: it applies, and is rebased onto another version, only while they
    /// do, e.g. while the charging pool a client read and changed is as the client read it.
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

            var entities = Flatten(this);

            for (var index = 0; index < Preconditions.Length; index++)
            {

                var precondition = Preconditions[index];

                if (!entities.TryGetValue(MergeKey.Of(precondition.EntityType, precondition.EntityId), out var entity))
                {
                    ErrorResponse = $"Precondition {index}: the {precondition.EntityType} '{precondition.EntityId}' does not exist!";
                    return false;
                }

                if (!EntityETag(entity.Full, precondition.ETag.Format, precondition.ETag.HashAlgorithm).Equals(precondition.ETag))
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
