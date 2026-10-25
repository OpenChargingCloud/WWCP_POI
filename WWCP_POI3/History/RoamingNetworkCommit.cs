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
using System.Security.Cryptography;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// One step in the history of a roaming network: a change set applied to the version of its first parent commit,
    /// the revision it leads to and the ETags of the resulting version.
    /// A genesis commit has neither parents nor a change set: it names the version a history starts with.
    /// A merge commit has two parents, its change set leads from the version of the first one to the merge of both,
    /// as everyone calculates it: it is not signed by owners, but checked by calculating it again.
    /// Its identification is the digest of its canonical JSON content without signatures, with the identification hash
    /// of its epoch, SHA-256 in the first one, so it names its whole ancestry, and signatures added later do not change it.
    /// From the second epoch on, its content names its epoch, and the genesis of an epoch the profiles of the epoch.
    /// </summary>
    public sealed partial class RoamingNetworkCommit
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object, which names the version of the content a commit identification is calculated
        /// over, and is part of it also where it is embedded: its ETags include the effectiveTree ETags of its version
        /// (SPECIFICATION.md, section 8.3).
        /// </summary>
        public const String JSONLDContext   = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/commit/v3";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its identification.
            /// </summary>
            public const String  Id                = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context           = "@context";

            /// <summary>
            /// The property of the identification of its roaming network.
            /// </summary>
            public const String  RoamingNetworkId  = "roamingNetworkId";

            /// <summary>
            /// The property of its revision.
            /// </summary>
            public const String  Revision          = "revision";

            /// <summary>
            /// The property of the ETags of its version.
            /// </summary>
            public const String  ETags             = "eTags";

            /// <summary>
            /// The property of its epoch, from the second one on.
            /// </summary>
            public const String  Epoch             = "epoch";

            /// <summary>
            /// The property of the profiles of the epoch it starts.
            /// </summary>
            public const String  Profiles          = RoamingNetworkEpochProfiles.JSONProperty;

            /// <summary>
            /// The property of the identifications of its parent commits.
            /// </summary>
            public const String  ParentIds         = "parentIds";

            /// <summary>
            /// The property of its change set.
            /// </summary>
            public const String  ChangeSet         = "changeSet";

            /// <summary>
            /// The property of its signatures.
            /// </summary>
            public const String  Signatures        = "signatures";

            /// <summary>
            /// The property of the commit in the signature input of a commit signature.
            /// </summary>
            public const String  Commit            = "commit";

        }

        #endregion


        /// <summary>
        /// The identification of this commit, calculated from its content.
        /// </summary>
        public RoamingNetworkCommit_Id    Id                  { get; }

        /// <summary>
        /// The identifications of the parent commits: none for a genesis commit, two for a merge commit.
        /// The change set leads from the version of the first one.
        /// </summary>
        public ImmutableArray<RoamingNetworkCommit_Id>  ParentIds  { get; }

        /// <summary>
        /// The identification of the first parent commit, whose version the change set is applied to,
        /// or null for a genesis commit.
        /// </summary>
        public RoamingNetworkCommit_Id?   ParentId
            => ParentIds.IsEmpty ? null : ParentIds[0];

        /// <summary>
        /// The roaming network of this commit.
        /// </summary>
        public RoamingNetwork_Id          RoamingNetworkId    { get; }

        /// <summary>
        /// The revision this commit leads to: one more than the highest one of its parents.
        /// </summary>
        public UInt64                     Revision            { get; }

        /// <summary>
        /// The ETags of the version this commit leads to, in JSON and CBOR, and its effectiveTree ETags.
        /// </summary>
        public ImmutableArray<ETag>       ETags               { get; }

        /// <summary>
        /// The applied change set, or null for a genesis commit.
        /// </summary>
        public RoamingNetworkChangeSet?   ChangeSet           { get; }

        /// <summary>
        /// The signatures of this commit, e.g. of the leader and of a roaming network operator.
        /// They are not part of the content the identification is calculated over.
        /// </summary>
        public ImmutableArray<RoamingNetworkCommitSignature>  Signatures  { get; }

        /// <summary>
        /// The epoch of this commit (SPECIFICATION.md, section 11): 1 for the first one, which its content does not name.
        /// </summary>
        public UInt32                     Epoch               { get; }

        /// <summary>
        /// The hash its identification is calculated with, the identification hash of its epoch.
        /// </summary>
        public ETagHashAlgorithm          IdentificationHash  { get; }

        /// <summary>
        /// The profiles of the epoch this commit starts, for the genesis of an epoch after the first one; null otherwise.
        /// </summary>
        public RoamingNetworkEpochProfiles?  Profiles         { get; }

        /// <summary>
        /// Whether this commit starts a history.
        /// </summary>
        public Boolean                    IsGenesis
            => ChangeSet is null;

        /// <summary>
        /// Whether this commit merges two branches.
        /// </summary>
        public Boolean                    IsMerge
            => ParentIds.Length > 1;

        #endregion

        #region Constructor(s)

        private RoamingNetworkCommit(ImmutableArray<RoamingNetworkCommit_Id>  ParentIds,
                                     RoamingNetwork_Id         RoamingNetworkId,
                                     UInt64                    Revision,
                                     ImmutableArray<ETag>      ETags,
                                     RoamingNetworkChangeSet?  ChangeSet,
                                     UInt32                    Epoch,
                                     ETagHashAlgorithm         IdentificationHash,
                                     RoamingNetworkEpochProfiles?  Profiles,
                                     ImmutableArray<RoamingNetworkCommitSignature>  Signatures   = default)
        {

            if (ETags.IsDefaultOrEmpty || ETags.Any(eTag => !eTag.IsValid))
                throw new ArgumentException("A commit requires valid ETags of its version.", nameof(ETags));

            if (!ETags.Any(eTag => eTag.Format == ETagFormat.JSON) ||
                !ETags.Any(eTag => eTag.Format == ETagFormat.CBOR) ||
                !ETags.Any(eTag => eTag.Format == ETagFormat.EffectiveTree))
            {
                throw new ArgumentException("A commit requires the json, cbor and effectiveTree ETags of its version.", nameof(ETags));
            }

            ParentIds = ParentIds.IsDefault ? [] : ParentIds;

            if (ParentIds.IsEmpty != (ChangeSet is null))
                throw new ArgumentException("A commit has both parents and a change set, or, as a genesis commit, neither.");

            if (ParentIds.Length > 2)
                throw new ArgumentException("A commit has at most two parents.", nameof(ParentIds));

            if (ParentIds.Distinct().Count() != ParentIds.Length)
                throw new ArgumentException("A commit must not name a parent twice.", nameof(ParentIds));

            if (ChangeSet is not null && ChangeSet.RoamingNetworkId != RoamingNetworkId)
                throw new ArgumentException("The change set is for another roaming network.", nameof(ChangeSet));

            if (Epoch == 0)
                throw new ArgumentException("The first epoch is 1.", nameof(Epoch));

            if ((Profiles is not null) != (ParentIds.IsEmpty && Epoch > 1))
                throw new ArgumentException("The genesis of an epoch after the first one names the profiles of the epoch, no other commit does.", nameof(Profiles));

            if (Profiles is not null ? Profiles.IdentificationHash != IdentificationHash : Epoch == 1 && IdentificationHash != ETagHashAlgorithm.SHA256)
                throw new ArgumentException("A commit is identified with the identification hash of its epoch.", nameof(IdentificationHash));

            if (ParentIds.Any(parentId => parentId.HashAlgorithm != IdentificationHash))
                throw new ArgumentException("A commit and its parents are identified with the same hash: the one of their epoch.", nameof(ParentIds));

            this.Epoch               = Epoch;
            this.IdentificationHash  = IdentificationHash;
            this.Profiles            = Profiles;

            this.ParentIds         = ParentIds;
            this.RoamingNetworkId  = RoamingNetworkId;
            this.Revision          = Revision;
            this.ETags             = ETags;
            this.ChangeSet         = ChangeSet;
            this.Signatures        = Signatures.IsDefault ? [] : Signatures;
            this.Id                = CalcId();

        }

        #endregion


        #region (static) Genesis(RoamingNetwork)

        /// <summary>
        /// The genesis commit of the given version of a roaming network, starting its history.
        /// </summary>
        /// <param name="RoamingNetwork">The first version of a roaming network.</param>
        public static RoamingNetworkCommit Genesis(RoamingNetwork RoamingNetwork)

            => new ([],
                    RoamingNetwork.Id,
                    RoamingNetwork.Revision,
                    RoamingNetwork.CalcETags(),
                    null,
                    1,
                    ETagHashAlgorithm.SHA256,
                    null);

        #endregion

        #region (static) Genesis(RoamingNetwork, Epoch)

        /// <summary>
        /// The genesis commit of the given epoch, starting it with the given version of a roaming network
        /// (SPECIFICATION.md, section 11): it names the epoch and its profiles, and is identified with its identification hash.
        /// </summary>
        /// <param name="RoamingNetwork">The version the epoch starts with.</param>
        /// <param name="Epoch">The epoch, after the first one.</param>
        public static RoamingNetworkCommit Genesis(RoamingNetwork       RoamingNetwork,
                                                   RoamingNetworkEpoch  Epoch)

            => Epoch.Number == 1
                   ? Genesis(RoamingNetwork)
                   : new ([],
                          RoamingNetwork.Id,
                          RoamingNetwork.Revision,
                          RoamingNetwork.CalcETags(),
                          null,
                          Epoch.Number,
                          Epoch.Profiles.IdentificationHash,
                          Epoch.Profiles);

        #endregion

        #region (static) Next   (Parent, ChangeSet, RoamingNetwork)

        /// <summary>
        /// The commit of the given change set, applied to the version of the given parent commit.
        /// </summary>
        /// <param name="Parent">The parent commit.</param>
        /// <param name="ChangeSet">The applied change set.</param>
        /// <param name="RoamingNetwork">The resulting version.</param>
        public static RoamingNetworkCommit Next(RoamingNetworkCommit     Parent,
                                                RoamingNetworkChangeSet  ChangeSet,
                                                RoamingNetwork           RoamingNetwork)

            => new ([ Parent.Id ],
                    Parent.RoamingNetworkId,
                    Parent.Revision + 1,
                    RoamingNetwork.CalcETags(),
                    ChangeSet,
                    Parent.Epoch,
                    Parent.IdentificationHash,
                    null);

        #endregion

        #region (static) Merge  (Left, Right, ChangeSet, RoamingNetwork)

        /// <summary>
        /// The commit merging the given right commit into the given left one:
        /// the change set leads from the version of the left one to the merge of both.
        /// </summary>
        /// <param name="Left">The first parent commit, whose version the change set is applied to.</param>
        /// <param name="Right">The second parent commit.</param>
        /// <param name="ChangeSet">The change set leading to the merge.</param>
        /// <param name="RoamingNetwork">The resulting version.</param>
        public static RoamingNetworkCommit Merge(RoamingNetworkCommit     Left,
                                                 RoamingNetworkCommit     Right,
                                                 RoamingNetworkChangeSet  ChangeSet,
                                                 RoamingNetwork           RoamingNetwork)

            => Left.Epoch == Right.Epoch
                   ? new ([ Left.Id, Right.Id ],
                          Left.RoamingNetworkId,
                          Math.Max(Left.Revision, Right.Revision) + 1,
                          RoamingNetwork.CalcETags(),
                          ChangeSet,
                          Left.Epoch,
                          Left.IdentificationHash,
                          null)
                   : throw new ArgumentException("Commits of different epochs are never merged.", nameof(Right));

        #endregion


        #region Parse    (Text, Limits = null)

        /// <summary>
        /// Parse the given JSON text of a commit. Dates stay text, so signed values keep their exact representation.
        /// </summary>
        /// <param name="Text">The JSON text to parse.</param>
        /// <param name="Limits">Optional limits for its change set, the default ones otherwise.</param>
        public static RoamingNetworkCommit Parse(String                          Text,
                                                 RoamingNetworkChangeSetLimits?  Limits   = null)
        {

            if (TryParse(Text, out var commit, out var errorResponse, Limits))
                return commit;

            throw new ArgumentException("The given JSON representation of a commit is invalid: " + errorResponse, nameof(Text));

        }

        #endregion

        #region TryParse (Text, out Commit, out ErrorResponse, Limits = null)

        /// <summary>
        /// Try to parse the given JSON text of a commit. Dates stay text, so signed values keep their exact representation.
        /// </summary>
        /// <param name="Text">The JSON text to parse.</param>
        /// <param name="Commit">The parsed commit.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Limits">Optional limits for its change set, the default ones otherwise.</param>
        public static Boolean TryParse(String                                          Text,
                                       [NotNullWhen(true)]  out RoamingNetworkCommit?  Commit,
                                       [NotNullWhen(false)] out String?                ErrorResponse,
                                       RoamingNetworkChangeSetLimits?                  Limits   = null)
        {

            Commit   = null;
            Limits ??= RoamingNetworkChangeSetLimits.Default;

            try
            {

                if (Text is null || Text.Length > Limits.MaxBytes)
                {
                    ErrorResponse = $"The commit must be given and must not exceed the limit of {Limits.MaxBytes} bytes!";
                    return false;
                }

                if (POIJSON.Read(Text, Limits.MaxDepth + 1) is not JObject json)
                {
                    ErrorResponse = "The given JSON text must be a JSON object!";
                    return false;
                }

                return TryParse(json, out Commit, out ErrorResponse, Limits);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region TryParse (JSON, out Commit, out ErrorResponse, Limits = null)

        /// <summary>
        /// Try to parse the given JSON representation of a commit.
        /// Its identification must be the one calculated from its content.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Commit">The parsed commit.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Limits">Optional limits for its change set, the default ones otherwise.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkCommit?  Commit,
                                       [NotNullWhen(false)] out String?                ErrorResponse,
                                       RoamingNetworkChangeSetLimits?                  Limits   = null)
        {

            Commit = null;

            try
            {

                if (JSON is null)
                {
                    ErrorResponse = "The given JSON object must not be null!";
                    return false;
                }

                #region Parse Id                  [mandatory]

                if (!RoamingNetworkCommit_Id.TryParse(JSON[PropertyKeys.Id]?.Type == JTokenType.String ? JSON[PropertyKeys.Id]!.Value<String>() : null, out var id))
                {
                    ErrorResponse = "Missing or invalid commit identification!";
                    return false;
                }

                #endregion

                #region Parse Context             [optional]

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the commit '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                #endregion

                #region Parse ParentIds           [optional]

                var parentIds = ImmutableArray.CreateBuilder<RoamingNetworkCommit_Id>();

                if (JSON[PropertyKeys.ParentIds] is JToken parentIdsJSON)
                {

                    if (parentIdsJSON is not JArray { Count: > 0 } parentIdsArray)
                    {
                        ErrorResponse = "The parent commit identifications must be a nonempty JSON array!";
                        return false;
                    }

                    foreach (var parentIdJSON in parentIdsArray)
                    {

                        if (parentIdJSON.Type != JTokenType.String || !RoamingNetworkCommit_Id.TryParse(parentIdJSON.Value<String>(), out var parsedParentId))
                        {
                            ErrorResponse = "Invalid parent commit identification!";
                            return false;
                        }

                        parentIds.Add(parsedParentId);

                    }

                }

                #endregion

                #region Parse RoamingNetworkId    [mandatory]

                if (!JSON.ParseMandatory(PropertyKeys.RoamingNetworkId,
                                         "roaming network identification",
                                         RoamingNetwork_Id.TryParse,
                                         out RoamingNetwork_Id roamingNetworkId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse Revision            [mandatory]

                if (JSON[PropertyKeys.Revision] is not JValue { Type: JTokenType.Integer } revisionJSON ||
                    revisionJSON.Value<Int64>() < 0)
                {
                    ErrorResponse = "Missing or invalid revision!";
                    return false;
                }

                var revision = (UInt64) revisionJSON.Value<Int64>();

                #endregion

                #region Parse ETags               [mandatory]

                if (JSON[PropertyKeys.ETags] is not JArray eTagsJSON)
                {
                    ErrorResponse = "The ETags must be a JSON array!";
                    return false;
                }

                var eTags = ImmutableArray.CreateBuilder<ETag>(eTagsJSON.Count);

                foreach (var eTagJSON in eTagsJSON)
                    eTags.Add(ETag.ParseHex(eTagJSON));

                #endregion

                #region Parse ChangeSet           [optional]

                RoamingNetworkChangeSet? changeSet = null;

                if (JSON[PropertyKeys.ChangeSet] is JToken changeSetJSON)
                {

                    if (changeSetJSON is not JObject changeSetObject)
                    {
                        ErrorResponse = "The change set must be a JSON object!";
                        return false;
                    }

                    if (!RoamingNetworkChangeSet.TryParse(changeSetObject, out changeSet, out var changeSetError, Limits))
                    {
                        ErrorResponse = "Invalid change set: " + changeSetError;
                        return false;
                    }

                }

                #endregion

                #region Parse Signatures          [optional]

                var signatures = ImmutableArray.CreateBuilder<RoamingNetworkCommitSignature>();

                if (JSON[PropertyKeys.Signatures] is JToken signaturesJSON)
                {

                    if (signaturesJSON is not JArray signaturesArray)
                    {
                        ErrorResponse = "The signatures must be a JSON array!";
                        return false;
                    }

                    foreach (var signatureJSON in signaturesArray)
                    {

                        if (signatureJSON is not JObject signatureObject)
                        {
                            ErrorResponse = $"Invalid signature {signatures.Count}: it must be a JSON object!";
                            return false;
                        }

                        if (!RoamingNetworkCommitSignature.TryParse(signatureObject, out var signature, out var signatureError))
                        {
                            ErrorResponse = $"Invalid signature {signatures.Count}: " + signatureError;
                            return false;
                        }

                        signatures.Add(signature);

                    }

                }

                #endregion


                #region Parse Epoch, Profiles     [optional]

                UInt32 epoch = 1;

                if (JSON[PropertyKeys.Epoch] is JToken epochJSON)
                {

                    if (epochJSON is not JValue { Type: JTokenType.Integer } epochValue ||
                        epochValue.Value<Decimal>() < 2 || epochValue.Value<Decimal>() > UInt32.MaxValue)
                    {
                        ErrorResponse = "Invalid epoch: a commit names it from the second one on!";
                        return false;
                    }

                    epoch = epochValue.Value<UInt32>();

                }

                RoamingNetworkEpochProfiles? profiles = null;

                if (JSON[PropertyKeys.Profiles] is JToken profilesJSON &&
                    !RoamingNetworkEpochProfiles.TryParse(profilesJSON, out profiles, out var profilesError))
                {
                    ErrorResponse = "Invalid profiles of the epoch: " + profilesError;
                    return false;
                }

                #endregion


                var commit = new RoamingNetworkCommit(
                                 parentIds.ToImmutable(),
                                 roamingNetworkId,
                                 revision,
                                 eTags.MoveToImmutable(),
                                 changeSet,
                                 epoch,
                                 id.HashAlgorithm,
                                 profiles,
                                 [ .. signatures.Distinct() ]
                             );

                if (commit.Id != id)
                {
                    ErrorResponse = $"The commit identification '{id}' is not the one of its content, '{commit.Id}'!";
                    return false;
                }

                // A commit with one parent: its change set identified by its content, with changes (SPECIFICATION.md,
                // sections 4 and 5.2); only the change set of a merge is identified by its parents.
                if (commit.ParentIds.Length == 1 && changeSet is not null && (changeSet.IsMerge || changeSet.Changes.Length == 0))
                {
                    ErrorResponse = changeSet.IsMerge
                                        ? $"The change set '{changeSet.Id}' of the commit '{id}' is identified as the change set of a merge, which only a merge commit has!"
                                        : $"The change set '{changeSet.Id}' of the commit '{id}' has no changes: it would change nothing!";
                    return false;
                }

                Commit         = commit;
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                Commit         = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region TryParse (CBOR, out Commit, out ErrorResponse, Limits = null)

        /// <summary>
        /// Try to parse the given CBOR representation of a commit:
        /// the keys and values of its JSON representation.
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="Commit">The parsed commit.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Limits">Optional limits for its change set, the default ones otherwise.</param>
        public static Boolean TryParse(CBORValue                                       CBOR,
                                       [NotNullWhen(true)]  out RoamingNetworkCommit?  Commit,
                                       [NotNullWhen(false)] out String?                ErrorResponse,
                                       RoamingNetworkChangeSetLimits?                  Limits   = null)
        {

            Commit   = null;
            Limits ??= RoamingNetworkChangeSetLimits.Default;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map ||
                    CBORJSON.ToJSON(CBOR, new CBORJSONOptions { MaxDepth = Limits.MaxDepth + 1 }) is not JObject json)
                {
                    ErrorResponse = "The CBOR representation of a commit must be a map with text keys!";
                    return false;
                }

                return TryParse(json, out Commit, out ErrorResponse, Limits);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false, IncludeSignatures = true, CustomCommitSerializer = null)

        /// <summary>
        /// Return the JSON representation of this commit, its change set with its signatures,
        /// and, unless excluded, with the signatures of this commit.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeSignatures">Whether to include the signatures of this commit.</param>
        /// <param name="CustomCommitSerializer">An optional custom serializer of commits.</param>
        public JObject ToJSON(Boolean                                                 Embedded                 = false,
                              Boolean                                                 IncludeSignatures        = true,
                              CustomJObjectSerializerDelegate<RoamingNetworkCommit>?  CustomCommitSerializer   = null)
        {

            var json = Content(IncludeSignatures: true);

            if (Embedded)
                json.Remove(PropertyKeys.Context);

            json.AddFirst(new JProperty(PropertyKeys.Id, Id.ToString()));

            if (IncludeSignatures && !Signatures.IsEmpty)
                json.Add(PropertyKeys.Signatures, new JArray(Signatures.Select(signature => signature.ToJSON(Embedded: true))));

            return CustomCommitSerializer is not null
                       ? CustomCommitSerializer(this, json)
                       : json;

        }

        #endregion

        #region ToCBOR(Embedded = false, CustomCommitSerializer = null)

        /// <summary>
        /// Return the CBOR representation of this commit: the keys and values of its JSON representation.
        /// Write it with CBORWriterOptions.Canonical for deterministic bytes.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomCommitSerializer">An optional custom serializer of commits.</param>
        public CBORValue ToCBOR(Boolean                                              Embedded                 = false,
                                CustomCBORSerializerDelegate<RoamingNetworkCommit>?  CustomCommitSerializer   = null)
        {

            var cbor = new CBORMap {
                           { CBORValue.FromText(PropertyKeys.Id),                CBORValue.FromText(Id.ToString()) }
                       };

            if (!Embedded)
                cbor.Add(CBORValue.FromText(PropertyKeys.Context),           CBORValue.FromText(JSONLDContext));

            cbor.Add(CBORValue.FromText(PropertyKeys.RoamingNetworkId),      CBORValue.FromText(RoamingNetworkId.ToString()));
            cbor.Add(CBORValue.FromText(PropertyKeys.Revision),              CBORValue.FromUInt64(Revision));

            // ETags as in the JSON representation: arrays of texts, their digests in hex.
            cbor.Add(CBORValue.FromText(PropertyKeys.ETags),                 CBORValue.FromArray(ETags.Select(eTag => CBORJSON.ToCBOR(eTag.ToJSON()))));

            if (Epoch > 1)
                cbor.Add(CBORValue.FromText(PropertyKeys.Epoch),             CBORValue.FromUInt64(Epoch));

            // The profiles and the signatures have no CBOR representation of their own: theirs are the keys and values of
            // their JSON representation, and so is the change set's, as a commit always wrote it.
            if (Profiles is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.Profiles),          CBORJSON.ToCBOR(Profiles.ToJSON(Embedded: true)));

            if (!ParentIds.IsEmpty)
                cbor.Add(CBORValue.FromText(PropertyKeys.ParentIds),         CBORValue.FromArray(ParentIds.Select(parentId => CBORValue.FromText(parentId.ToString()))));

            if (ChangeSet is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.ChangeSet),         CBORJSON.ToCBOR(ChangeSet.ToJSON(Embedded: true, IncludeSignatures: true)));

            if (!Signatures.IsEmpty)
                cbor.Add(CBORValue.FromText(PropertyKeys.Signatures),        CBORValue.FromArray(Signatures.Select(signature => CBORJSON.ToCBOR(signature.ToJSON(Embedded: true)))));

            return CustomCommitSerializer is not null
                       ? CustomCommitSerializer(this, cbor)
                       : cbor;

        }

        #endregion


        #region (private) Content(IncludeSignatures)

        /// <summary>
        /// The content of this commit; without signatures, it is what its identification is calculated over.
        /// </summary>
        private JObject Content(Boolean IncludeSignatures)
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Context,           JSONLDContext),
                           new JProperty(PropertyKeys.RoamingNetworkId,  RoamingNetworkId.ToString()),
                           new JProperty(PropertyKeys.Revision,          Revision),
                           new JProperty(PropertyKeys.ETags,             new JArray(ETags.Select(eTag => eTag.ToJSON())))
                       );

            if (Epoch > 1)
                json.Add(PropertyKeys.Epoch,      Epoch);

            if (Profiles is not null)
                json.Add(PropertyKeys.Profiles,   Profiles.ToJSON(Embedded: true));

            if (!ParentIds.IsEmpty)
                json.Add(PropertyKeys.ParentIds,  new JArray(ParentIds.Select(parentId => parentId.ToString())));

            if (ChangeSet is not null)
                json.Add(PropertyKeys.ChangeSet,  ChangeSet.ToJSON(Embedded: true, IncludeSignatures: IncludeSignatures));

            return json;

        }

        #endregion

        #region (private) CalcId()

        private RoamingNetworkCommit_Id CalcId()

            => RoamingNetworkCommit_Id.FromDigest(
                   POIMerkleTree.Hash(
                       IdentificationHash,
                       CanonicalJSON.ToUTF8Bytes(
                           Content(IncludeSignatures: false)
                       )
                   )
               );

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"Commit {Id} ({(Epoch > 1 ? $"epoch {Epoch}, " : "")}revision {Revision}{(ParentIds.IsEmpty ? ", genesis" : $", parents {String.Join(", ", ParentIds)}")})";

        #endregion

    }

}
