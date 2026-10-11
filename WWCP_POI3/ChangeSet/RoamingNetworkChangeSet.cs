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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// An immutable, serializable batch of changes to one roaming network.
    /// Its before/after content identifiers bind the batch to its source and resulting POI data:
    /// the before ETags identify the exact version the changes were prepared against.
    /// </summary>
    public sealed partial record RoamingNetworkChangeSet
    {

        #region Data

        private readonly JObject metadata;

        #endregion

        #region Properties

        /// <summary>
        /// The unique change set identifier.
        /// </summary>
        public ChangeSet_Id                                      Id                  { get; }

        /// <summary>
        /// The roaming network targeted by this batch.
        /// </summary>
        public RoamingNetwork_Id                                 RoamingNetworkId    { get; }

        /// <summary>
        /// The commit timestamp applied to modified entities, to the millisecond.
        /// </summary>
        public DateTimeOffset                                    CreatedAt           { get; }

        /// <summary>
        /// The immutable operations applied in their declared order.
        /// </summary>
        public ImmutableArray<RoamingNetworkChange>              Changes             { get; }

        /// <summary>
        /// The canonical JSON and CBOR identifiers of the expected source POI content.
        /// </summary>
        public ImmutableArray<ETag>                              BeforeETags         { get; }

        /// <summary>
        /// The canonical JSON and CBOR identifiers required after all operations.
        /// </summary>
        public ImmutableArray<ETag>                              AfterETags          { get; }

        /// <summary>
        /// Immutable commit descriptions by language, included in the signature.
        /// </summary>
        public ImmutableI18NString                               Description         { get; }

        /// <summary>
        /// Application metadata, included in the signature.
        /// Every read returns an independent copy.
        /// </summary>
        public JObject                                           Metadata
            => (JObject) metadata.DeepClone();

        /// <summary>
        /// Equal peer signatures over the complete batch; an empty array means unsigned.
        /// </summary>
        public ImmutableArray<RoamingNetworkChangeSetSignature>  Signatures          { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new change set.
        /// </summary>
        /// <param name="Id">The unique change set identifier.</param>
        /// <param name="RoamingNetworkId">The roaming network targeted by this batch.</param>
        /// <param name="CreatedAt">The commit timestamp, kept to the millisecond as in its JSON representation.</param>
        /// <param name="Changes">The operations, applied in their declared order.</param>
        /// <param name="BeforeETags">The ETags of the expected source POI content.</param>
        /// <param name="AfterETags">The ETags required after all operations.</param>
        /// <param name="Description">An optional multi-language description.</param>
        /// <param name="Metadata">Optional application metadata.</param>
        /// <param name="Signatures">Optional peer signatures.</param>
        public RoamingNetworkChangeSet(ChangeSet_Id                                       Id,
                                       RoamingNetwork_Id                                  RoamingNetworkId,
                                       DateTimeOffset                                     CreatedAt,
                                       ImmutableArray<RoamingNetworkChange>               Changes,
                                       ImmutableArray<ETag>                               BeforeETags,
                                       ImmutableArray<ETag>                               AfterETags,
                                       ImmutableI18NString?                               Description   = null,
                                       JObject?                                           Metadata      = null,
                                       ImmutableArray<RoamingNetworkChangeSetSignature>?  Signatures    = null)
        {

            if (Changes.IsDefault || Changes.Any(change => change is null))
                throw new ArgumentException("Changes must be an initialized collection without null entries.", nameof(Changes));

            if (BeforeETags.IsDefault || BeforeETags.Any(eTag => !eTag.IsValid))
                throw new ArgumentException("The before ETags must be an initialized collection of valid ETags.", nameof(BeforeETags));

            if (AfterETags. IsDefault || AfterETags. Any(eTag => !eTag.IsValid))
                throw new ArgumentException("The after ETags must be an initialized collection of valid ETags.",  nameof(AfterETags));

            var signatures = Signatures is { IsDefault: false } givenSignatures
                                 ? givenSignatures
                                 : [];

            if (signatures.Any(signature => signature is null))
                throw new ArgumentException("Signatures must not contain null entries.", nameof(Signatures));

            this.Id                = Id;
            this.RoamingNetworkId  = RoamingNetworkId;
            this.CreatedAt         = DateTimeOffset.FromUnixTimeMilliseconds(CreatedAt.ToUnixTimeMilliseconds());
            this.Changes           = Changes;
            this.BeforeETags       = BeforeETags;
            this.AfterETags        = AfterETags;
            this.Description       = Description ?? ImmutableI18NString.Empty;
            this.metadata          = (JObject?) Metadata?.DeepClone() ?? [];
            this.Signatures        = signatures;

        }

        #endregion


        #region WithSignature (Signature)

        /// <summary>
        /// Append an equal peer signature envelope without changing the batch's signed content.
        /// </summary>
        /// <param name="Signature">A peer signature.</param>
        public RoamingNetworkChangeSet WithSignature(RoamingNetworkChangeSetSignature Signature)
        {

            ArgumentNullException.ThrowIfNull(Signature);

            return WithSignatures(Signatures.Add(Signature));

        }

        #endregion

        #region WithSignatures(Signatures)

        /// <summary>
        /// Return an immutable copy with the supplied peer signature array.
        /// </summary>
        /// <param name="Signatures">The peer signatures.</param>
        public RoamingNetworkChangeSet WithSignatures(ImmutableArray<RoamingNetworkChangeSetSignature> Signatures)

            => new (Id,
                    RoamingNetworkId,
                    CreatedAt,
                    Changes,
                    BeforeETags,
                    AfterETags,
                    Description,
                    metadata,
                    Signatures);

        #endregion


        #region Parse    (Text)

        /// <summary>
        /// Parse the given JSON text of a change set.
        /// Dates stay text, so signed values keep their exact representation.
        /// </summary>
        /// <param name="Text">The JSON text to parse.</param>
        public static RoamingNetworkChangeSet Parse(String Text)
        {

            if (TryParse(Text,
                         out var changeSet,
                         out var errorResponse))
            {
                return changeSet;
            }

            throw new ArgumentException("The given JSON representation of a change set is invalid: " + errorResponse,
                                        nameof(Text));

        }

        #endregion

        #region TryParse (Text, out ChangeSet, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON text of a change set.
        /// Dates stay text, so signed values keep their exact representation.
        /// </summary>
        /// <param name="Text">The JSON text to parse.</param>
        /// <param name="ChangeSet">The parsed change set.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(String                                             Text,
                                       [NotNullWhen(true)]  out RoamingNetworkChangeSet?  ChangeSet,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)
        {

            ChangeSet = null;

            try
            {

                using var reader = new JsonTextReader(new StringReader(Text)) {
                                       DateParseHandling   = DateParseHandling.None,
                                       FloatParseHandling  = FloatParseHandling.Decimal
                                   };

                if (JToken.ReadFrom(reader) is not JObject json)
                {
                    ErrorResponse = "The given JSON text must be a JSON object!";
                    return false;
                }

                return TryParse(json,
                                out ChangeSet,
                                out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region Parse    (JSON)

        /// <summary>
        /// Parse the given JSON representation of a change set.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        public static RoamingNetworkChangeSet Parse(JObject JSON)
        {

            if (TryParse(JSON,
                         out var changeSet,
                         out var errorResponse))
            {
                return changeSet;
            }

            throw new ArgumentException("The given JSON representation of a change set is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region TryParse (JSON, out ChangeSet, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a change set.
        /// Parse JSON text with Parse(Text), as dates read as dates
        /// would change the signed representation of their values.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChangeSet">The parsed change set.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkChangeSet?  ChangeSet,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)
        {

            ChangeSet = null;

            try
            {

                if (JSON is null)
                {
                    ErrorResponse = "The given JSON object must not be null!";
                    return false;
                }

                #region Parse Id                  [mandatory]

                if (!JSON.ParseMandatory("@id",
                                         "change set identification",
                                         ChangeSet_Id.TryParse,
                                         out ChangeSet_Id id,
                                         out ErrorResponse))
                {
                    return false;
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

                #region Parse CreatedAt           [mandatory]

                if (!JSON.ParseMandatory("createdAt",
                                         "creation timestamp",
                                         out DateTimeOffset createdAt,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse Changes             [mandatory]

                if (JSON["changes"] is not JArray changesJSON)
                {
                    ErrorResponse = "The changes must be a JSON array!";
                    return false;
                }

                var changes = ImmutableArray.CreateBuilder<RoamingNetworkChange>(changesJSON.Count);

                foreach (var changeJSON in changesJSON)
                {

                    if (changeJSON is not JObject changeObject)
                    {
                        ErrorResponse = $"Change {changes.Count} must be a JSON object!";
                        return false;
                    }

                    if (!RoamingNetworkChange.TryParse(changeObject, out var change, out var changeError))
                    {
                        ErrorResponse = $"Invalid change {changes.Count}: {changeError}";
                        return false;
                    }

                    changes.Add(change);

                }

                #endregion

                #region Parse BeforeETags         [mandatory]

                if (!TryParseETags(JSON, "beforeETags", out var beforeETags, out ErrorResponse))
                    return false;

                #endregion

                #region Parse AfterETags          [mandatory]

                if (!TryParseETags(JSON, "afterETags",  out var afterETags,  out ErrorResponse))
                    return false;

                #endregion

                #region Parse Description         [optional]

                if (JSON.ParseOptionalJSON("description",
                                           "change set description",
                                           ImmutableI18NString.TryParse,
                                           out ImmutableI18NString? description,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse Metadata            [optional]

                var metadata = JSON["metadata"];

                if (metadata is not null && metadata is not JObject)
                {
                    ErrorResponse = "The metadata must be a JSON object!";
                    return false;
                }

                #endregion

                #region Parse Signatures          [optional]

                var signatures = ImmutableArray<RoamingNetworkChangeSetSignature>.Empty;

                if (JSON["signatures"] is JToken signaturesJSON)
                {

                    if (signaturesJSON is not JArray signaturesArray)
                    {
                        ErrorResponse = "The signatures must be a JSON array!";
                        return false;
                    }

                    foreach (var signatureJSON in signaturesArray)
                    {

                        if (signatureJSON is not JObject signatureObject ||
                            !RoamingNetworkChangeSetSignature.TryParse(signatureObject, out var signature, out var signatureError))
                        {
                            ErrorResponse = $"Invalid signature {signatures.Length}!";
                            return false;
                        }

                        signatures = signatures.Add(signature);

                    }

                }

                #endregion


                ChangeSet = new RoamingNetworkChangeSet(
                                id,
                                roamingNetworkId,
                                createdAt,
                                changes.MoveToImmutable(),
                                beforeETags,
                                afterETags,
                                description,
                                (JObject?) metadata,
                                signatures
                            );

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChangeSet      = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON   (IncludeSignatures = true)

        /// <summary>
        /// Return a JSON representation of this change set.
        /// </summary>
        /// <param name="IncludeSignatures">Whether to include the signatures; the signed content is without them.</param>
        public JObject ToJSON(Boolean IncludeSignatures = true)
        {

            var json = JSONObject.Create(

                                 new JProperty("@id",                Id.              ToString()),
                                 new JProperty("roamingNetworkId",   RoamingNetworkId.ToString()),
                                 new JProperty("createdAt",          CreatedAt.       ToISO8601()),

                           Description.IsNotNullOrEmpty()
                               ? new JProperty("description",        Description.ToJSON())
                               : null,

                           metadata.HasValues
                               ? new JProperty("metadata",           metadata.DeepClone())
                               : null,

                           IncludeSignatures && Signatures.Length > 0
                               ? new JProperty("signatures",         new JArray(Signatures.Select(signature => signature.ToJSON())))
                               : null

                       );

            // Added directly: JSONObject.Create leaves out empty arrays, but they are mandatory here.
            json.Add("changes",      new JArray(Changes.    Select(change => change.ToJSON())));
            json.Add("beforeETags",  new JArray(BeforeETags.Select(eTag   => eTag.  ToJSON())));
            json.Add("afterETags",   new JArray(AfterETags. Select(eTag   => eTag.  ToJSON())));

            return json;

        }

        #endregion


        #region (private static) TryParseETags(JSON, PropertyName, out ETags, out ErrorResponse)

        private static Boolean TryParseETags(JObject                           JSON,
                                             String                            PropertyName,
                                             out ImmutableArray<ETag>          ETags,
                                             [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ETags = [];

            if (JSON[PropertyName] is not JArray eTagsJSON)
            {
                ErrorResponse = $"The {PropertyName} must be a JSON array!";
                return false;
            }

            var eTags = ImmutableArray.CreateBuilder<ETag>(eTagsJSON.Count);

            foreach (var eTagJSON in eTagsJSON)
            {
                try
                {
                    eTags.Add(ETag.Parse(eTagJSON));
                }
                catch (Exception e)
                {
                    ErrorResponse = $"Invalid {PropertyName}: {e.Message}";
                    return false;
                }
            }

            ETags          = eTags.MoveToImmutable();
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }

}
