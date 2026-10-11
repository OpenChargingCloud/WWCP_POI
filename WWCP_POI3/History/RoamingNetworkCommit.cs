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
    /// One step in the history of a roaming network: a change set applied to the version of its parent commit,
    /// the revision it leads to and the ETags of the resulting version.
    /// A genesis commit has neither a parent nor a change set: it names the version a history starts with.
    /// Its identification is the SHA-256 digest of its canonical JSON content without signatures,
    /// so it names its whole ancestry, and signatures added later do not change it.
    /// </summary>
    public sealed class RoamingNetworkCommit
    {

        #region Data

        /// <summary>
        /// The versioned profile of the content a commit identification is calculated over.
        /// </summary>
        public const String IdentityProfile = "wwcp-poi-commit-v1";

        #endregion

        #region Properties

        /// <summary>
        /// The identification of this commit, calculated from its content.
        /// </summary>
        public RoamingNetworkCommit_Id    Id                  { get; }

        /// <summary>
        /// The identification of the parent commit, or null for a genesis commit.
        /// </summary>
        public RoamingNetworkCommit_Id?   ParentId            { get; }

        /// <summary>
        /// The roaming network of this commit.
        /// </summary>
        public RoamingNetwork_Id          RoamingNetworkId    { get; }

        /// <summary>
        /// The revision this commit leads to: one more than its parent's.
        /// </summary>
        public UInt64                     Revision            { get; }

        /// <summary>
        /// The ETags of the version this commit leads to, in JSON and CBOR.
        /// </summary>
        public ImmutableArray<ETag>       ETags               { get; }

        /// <summary>
        /// The applied change set, or null for a genesis commit.
        /// </summary>
        public RoamingNetworkChangeSet?   ChangeSet           { get; }

        /// <summary>
        /// Whether this commit starts a history.
        /// </summary>
        public Boolean                    IsGenesis
            => ChangeSet is null;

        #endregion

        #region Constructor(s)

        private RoamingNetworkCommit(RoamingNetworkCommit_Id?  ParentId,
                                     RoamingNetwork_Id         RoamingNetworkId,
                                     UInt64                    Revision,
                                     ImmutableArray<ETag>      ETags,
                                     RoamingNetworkChangeSet?  ChangeSet)
        {

            if (ETags.IsDefaultOrEmpty || ETags.Any(eTag => !eTag.IsValid))
                throw new ArgumentException("A commit requires valid ETags of its version.", nameof(ETags));

            if ((ParentId is null) != (ChangeSet is null))
                throw new ArgumentException("A commit has both a parent and a change set, or, as a genesis commit, neither.");

            if (ChangeSet is not null && ChangeSet.RoamingNetworkId != RoamingNetworkId)
                throw new ArgumentException("The change set is for another roaming network.", nameof(ChangeSet));

            this.ParentId          = ParentId;
            this.RoamingNetworkId  = RoamingNetworkId;
            this.Revision          = Revision;
            this.ETags             = ETags;
            this.ChangeSet         = ChangeSet;
            this.Id                = CalcId();

        }

        #endregion


        #region (static) Genesis(RoamingNetwork)

        /// <summary>
        /// The genesis commit of the given version of a roaming network, starting its history.
        /// </summary>
        /// <param name="RoamingNetwork">The first version of a roaming network.</param>
        public static RoamingNetworkCommit Genesis(RoamingNetwork RoamingNetwork)

            => new (null,
                    RoamingNetwork.Id,
                    RoamingNetwork.Revision,
                    RoamingNetwork.CalcETags(),
                    null);

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

            => new (Parent.Id,
                    Parent.RoamingNetworkId,
                    Parent.Revision + 1,
                    RoamingNetwork.CalcETags(),
                    ChangeSet);

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

                using var reader = new JsonTextReader(new StringReader(Text)) {
                                       DateParseHandling   = DateParseHandling.None,
                                       FloatParseHandling  = FloatParseHandling.Decimal,
                                       MaxDepth            = Limits.MaxDepth + 1
                                   };

                if (JToken.ReadFrom(reader) is not JObject json)
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

                if (!RoamingNetworkCommit_Id.TryParse(JSON["@id"]?.Type == JTokenType.String ? JSON["@id"]!.Value<String>() : null, out var id))
                {
                    ErrorResponse = "Missing or invalid commit identification!";
                    return false;
                }

                #endregion

                #region Parse ParentId            [optional]

                RoamingNetworkCommit_Id? parentId = null;

                if (JSON["parentId"] is JToken parentIdJSON)
                {

                    if (parentIdJSON.Type != JTokenType.String || !RoamingNetworkCommit_Id.TryParse(parentIdJSON.Value<String>(), out var parsedParentId))
                    {
                        ErrorResponse = "Invalid parent commit identification!";
                        return false;
                    }

                    parentId = parsedParentId;

                }

                #endregion

                #region Parse RoamingNetworkId    [mandatory]

                if (!JSON.ParseMandatory("roamingNetworkId",
                                         "roaming network identification",
                                         RoamingNetwork_Id.TryParse,
                                         out RoamingNetwork_Id roamingNetworkId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse Revision            [mandatory]

                if (JSON["revision"] is not JValue { Type: JTokenType.Integer } revisionJSON ||
                    revisionJSON.Value<Int64>() < 0)
                {
                    ErrorResponse = "Missing or invalid revision!";
                    return false;
                }

                var revision = (UInt64) revisionJSON.Value<Int64>();

                #endregion

                #region Parse ETags               [mandatory]

                if (JSON["eTags"] is not JArray eTagsJSON)
                {
                    ErrorResponse = "The ETags must be a JSON array!";
                    return false;
                }

                var eTags = ImmutableArray.CreateBuilder<ETag>(eTagsJSON.Count);

                foreach (var eTagJSON in eTagsJSON)
                    eTags.Add(ETag.Parse(eTagJSON));

                #endregion

                #region Parse ChangeSet           [optional]

                RoamingNetworkChangeSet? changeSet = null;

                if (JSON["changeSet"] is JToken changeSetJSON)
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


                var commit = new RoamingNetworkCommit(
                                 parentId,
                                 roamingNetworkId,
                                 revision,
                                 eTags.MoveToImmutable(),
                                 changeSet
                             );

                if (commit.Id != id)
                {
                    ErrorResponse = $"The commit identification '{id}' is not the one of its content, '{commit.Id}'!";
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

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this commit, its change set with its signatures.
        /// </summary>
        public JObject ToJSON()
        {

            var json = Content(IncludeSignatures: true);

            json.Remove("profile");
            json.AddFirst(new JProperty("@id", Id.ToString()));

            return json;

        }

        #endregion

        #region ToCBOR()

        /// <summary>
        /// Return the CBOR representation of this commit: the keys and values of its JSON representation.
        /// Write it with CBORWriterOptions.Canonical for deterministic bytes.
        /// </summary>
        public CBORValue ToCBOR()

            => CBORJSON.ToCBOR(ToJSON());

        #endregion


        #region (private) Content(IncludeSignatures)

        /// <summary>
        /// The content of this commit; without signatures, it is what its identification is calculated over.
        /// </summary>
        private JObject Content(Boolean IncludeSignatures)
        {

            var json = new JObject(
                           new JProperty("profile",           IdentityProfile),
                           new JProperty("roamingNetworkId",  RoamingNetworkId.ToString()),
                           new JProperty("revision",          Revision),
                           new JProperty("eTags",             new JArray(ETags.Select(eTag => eTag.ToJSON())))
                       );

            if (ParentId.HasValue)
                json.Add("parentId",   ParentId.Value.ToString());

            if (ChangeSet is not null)
                json.Add("changeSet",  ChangeSet.ToJSON(IncludeSignatures));

            return json;

        }

        #endregion

        #region (private) CalcId()

        private RoamingNetworkCommit_Id CalcId()

            => RoamingNetworkCommit_Id.FromDigest(
                   SHA256.HashData(
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

            => $"Commit {Id} (revision {Revision}{(ParentId.HasValue ? $", parent {ParentId}" : ", genesis")})";

        #endregion

    }

}
