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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The metadata of a commit, without its changes: small enough to list a history
    /// and to decide which commits to fetch completely.
    /// </summary>
    public sealed record RoamingNetworkCommitInfo
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/commitInfo/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of the identification of the commit.
            /// </summary>
            public const String  Id               = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context          = "@context";

            /// <summary>
            /// The property of the revision the commit leads to.
            /// </summary>
            public const String  Revision         = "revision";

            /// <summary>
            /// The property of the ETags of the version the commit leads to.
            /// </summary>
            public const String  ETags            = "eTags";

            /// <summary>
            /// The property of the number of changes of the applied change set.
            /// </summary>
            public const String  NumberOfChanges  = "numberOfChanges";

            /// <summary>
            /// The property of the identifications of the parent commits.
            /// </summary>
            public const String  ParentIds        = "parentIds";

            /// <summary>
            /// The property of the identification of the applied change set.
            /// </summary>
            public const String  ChangeSetId      = "changeSetId";

            /// <summary>
            /// The property of the commit timestamp of the applied change set.
            /// </summary>
            public const String  CreatedAt        = "createdAt";

            /// <summary>
            /// The property of the description of the applied change set.
            /// </summary>
            public const String  Description      = "description";

            /// <summary>
            /// The property of the key identifications of the signatures of the applied change set.
            /// </summary>
            public const String  SignatureKeyIds  = "signatureKeyIds";

        }

        #endregion


        /// <summary>
        /// The identification of the commit.
        /// </summary>
        public RoamingNetworkCommit_Id         Id                    { get; }

        /// <summary>
        /// The identification of the parent commit, or null for a genesis commit.
        /// </summary>
        public RoamingNetworkCommit_Id?        ParentId
            => ParentIds.IsEmpty ? null : ParentIds[0];

        /// <summary>
        /// The identifications of the parent commits: none for a genesis commit, two for a merge commit.
        /// </summary>
        public ImmutableArray<RoamingNetworkCommit_Id>  ParentIds  { get; }

        /// <summary>
        /// The revision the commit leads to.
        /// </summary>
        public UInt64                          Revision              { get; }

        /// <summary>
        /// The ETags of the version the commit leads to.
        /// </summary>
        public ImmutableArray<ETag>            ETags                 { get; }

        /// <summary>
        /// The identification of the applied change set, or null for a genesis commit.
        /// </summary>
        public ChangeSet_Id?                   ChangeSetId           { get; }

        /// <summary>
        /// The commit timestamp of the applied change set, or null for a genesis commit.
        /// </summary>
        public DateTimeOffset?                 CreatedAt             { get; }

        /// <summary>
        /// The description of the applied change set.
        /// </summary>
        public ImmutableI18NString             Description           { get; }

        /// <summary>
        /// The number of changes of the applied change set.
        /// </summary>
        public Int32                           NumberOfChanges       { get; }

        /// <summary>
        /// The key identifications of the signatures of the applied change set.
        /// </summary>
        public ImmutableArray<String>          SignatureKeyIds       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// The metadata of the given commit.
        /// </summary>
        /// <param name="Commit">A commit.</param>
        public RoamingNetworkCommitInfo(RoamingNetworkCommit Commit)
        {

            this.Id               = Commit.Id;
            this.ParentIds        = Commit.ParentIds;
            this.Revision         = Commit.Revision;
            this.ETags            = Commit.ETags;
            this.ChangeSetId      = Commit.ChangeSet?.Id;
            this.CreatedAt        = Commit.ChangeSet?.CreatedAt;
            this.Description      = Commit.ChangeSet?.Description ?? ImmutableI18NString.Empty;
            this.NumberOfChanges  = Commit.ChangeSet?.Changes.Length ?? 0;
            this.SignatureKeyIds  = Commit.ChangeSet?.Signatures.Select(signature => signature.KeyId).ToImmutableArray() ?? [];

        }

        #endregion


        #region ToJSON(Embedded = false, CustomCommitInfoSerializer = null)

        /// <summary>
        /// Return a JSON representation of these commit metadata.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomCommitInfoSerializer">An optional custom serializer of commit metadata.</param>
        public JObject ToJSON(Boolean                                                     Embedded                     = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkCommitInfo>?  CustomCommitInfoSerializer   = null)
        {

            var json = JSONObject.Create(

                                 new JProperty(PropertyKeys.Id,               Id.ToString()),

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,          JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.Revision,         Revision),
                                 new JProperty(PropertyKeys.ETags,            new JArray(ETags.Select(eTag => eTag.ToJSON()))),
                                 new JProperty(PropertyKeys.NumberOfChanges,  NumberOfChanges)

                       );

            if (!ParentIds.IsEmpty)
                json.Add(PropertyKeys.ParentIds,        new JArray(ParentIds.Select(parentId => parentId.ToString())));

            if (ChangeSetId.HasValue)
                json.Add(PropertyKeys.ChangeSetId,      ChangeSetId.Value.ToString());

            if (CreatedAt.HasValue)
                json.Add(PropertyKeys.CreatedAt,        CreatedAt.Value.ToISO8601());

            if (Description.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Description,      Description.ToJSON());

            if (SignatureKeyIds.Length > 0)
                json.Add(PropertyKeys.SignatureKeyIds,  new JArray(SignatureKeyIds));

            return CustomCommitInfoSerializer is not null
                       ? CustomCommitInfoSerializer(this, json)
                       : json;

        }

        #endregion

        #region ToCBOR(Embedded = false, CustomCommitInfoSerializer = null)

        /// <summary>
        /// Return a CBOR representation of these commit metadata: the keys and values of the JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomCommitInfoSerializer">An optional custom serializer of commit metadata.</param>
        public CBORValue ToCBOR(Boolean                                                  Embedded                     = false,
                                CustomCBORSerializerDelegate<RoamingNetworkCommitInfo>?  CustomCommitInfoSerializer   = null)
        {

            var cbor = new CBORMap {
                           { CBORValue.FromText(PropertyKeys.Id),                CBORValue.FromText(Id.ToString()) }
                       };

            if (!Embedded)
                cbor.Add(CBORValue.FromText(PropertyKeys.Context),           CBORValue.FromText(JSONLDContext));

            cbor.Add(CBORValue.FromText(PropertyKeys.Revision),              CBORValue.FromUInt64(Revision));

            // ETags as in the JSON representation: arrays of texts, their digests in hex.
            cbor.Add(CBORValue.FromText(PropertyKeys.ETags),                 CBORValue.FromArray(ETags.Select(eTag => CBORJSON.ToCBOR(eTag.ToJSON()))));
            cbor.Add(CBORValue.FromText(PropertyKeys.NumberOfChanges),       CBORValue.FromInt64(NumberOfChanges));

            if (!ParentIds.IsEmpty)
                cbor.Add(CBORValue.FromText(PropertyKeys.ParentIds),         CBORValue.FromArray(ParentIds.Select(parentId => CBORValue.FromText(parentId.ToString()))));

            if (ChangeSetId.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.ChangeSetId),       CBORValue.FromText(ChangeSetId.Value.ToString()));

            if (CreatedAt.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.CreatedAt),         CBORValue.FromText(CreatedAt.Value.ToISO8601()));

            // The description as in the JSON representation.
            if (Description.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(PropertyKeys.Description),       CBORJSON.ToCBOR(Description.ToJSON()));

            if (SignatureKeyIds.Length > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.SignatureKeyIds),   CBORValue.FromArray(SignatureKeyIds.Select(keyId => CBORValue.FromText(keyId))));

            return CustomCommitInfoSerializer is not null
                       ? CustomCommitInfoSerializer(this, cbor)
                       : cbor;

        }

        #endregion

    }

}
