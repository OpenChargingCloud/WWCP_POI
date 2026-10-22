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
using System.Text;

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

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/changeSet/v3";

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
            public const String  Id                 = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context            = "@context";

            /// <summary>
            /// The property of its roaming network identification.
            /// </summary>
            public const String  RoamingNetworkId   = "roamingNetworkId";

            /// <summary>
            /// The property of its creation timestamp.
            /// </summary>
            public const String  CreatedAt          = "createdAt";

            /// <summary>
            /// The property of its multilingual description.
            /// </summary>
            public const String  Description        = "description";

            /// <summary>
            /// The property of its application metadata.
            /// </summary>
            public const String  Metadata           = "metadata";

            /// <summary>
            /// The property of its preconditions.
            /// </summary>
            public const String  Preconditions      = "preconditions";

            /// <summary>
            /// The property of its signatures.
            /// </summary>
            public const String  Signatures         = "signatures";

            /// <summary>
            /// The property of its changes.
            /// </summary>
            public const String  Changes            = "changes";

            /// <summary>
            /// The property of its before ETags.
            /// </summary>
            public const String  BeforeETags        = "beforeETags";

            /// <summary>
            /// The property of its after ETags.
            /// </summary>
            public const String  AfterETags         = "afterETags";

            /// <summary>
            /// The property of the change set in its signature input.
            /// </summary>
            public const String  ChangeSet          = "changeSet";

        }

        #endregion


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

        /// <summary>
        /// The ETags entities must still have for this change set to apply, or to be rebased; included in the signature.
        /// </summary>
        public ImmutableArray<RoamingNetworkPrecondition>        Preconditions       { get; }

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
        /// <param name="Preconditions">Optional ETags entities must still have.</param>
        public RoamingNetworkChangeSet(ChangeSet_Id                                       Id,
                                       RoamingNetwork_Id                                  RoamingNetworkId,
                                       DateTimeOffset                                     CreatedAt,
                                       ImmutableArray<RoamingNetworkChange>               Changes,
                                       ImmutableArray<ETag>                               BeforeETags,
                                       ImmutableArray<ETag>                               AfterETags,
                                       ImmutableI18NString?                               Description     = null,
                                       JObject?                                           Metadata        = null,
                                       ImmutableArray<RoamingNetworkChangeSetSignature>?  Signatures      = null,
                                       ImmutableArray<RoamingNetworkPrecondition>?        Preconditions   = null)
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

            var preconditions = Preconditions is { IsDefault: false } givenPreconditions
                                    ? givenPreconditions
                                    : [];

            if (preconditions.Any(precondition => precondition is null || !precondition.ETag.IsValid))
                throw new ArgumentException("Preconditions must not contain null entries or invalid ETags.", nameof(Preconditions));

            this.Id                = Id;
            this.RoamingNetworkId  = RoamingNetworkId;
            this.CreatedAt         = DateTimeOffset.FromUnixTimeMilliseconds(CreatedAt.ToUnixTimeMilliseconds());
            this.Changes           = Changes;
            this.BeforeETags       = BeforeETags;
            this.AfterETags        = AfterETags;
            this.Description       = Description ?? ImmutableI18NString.Empty;
            this.metadata          = (JObject?) Metadata?.DeepClone() ?? [];
            this.Signatures        = signatures;
            this.Preconditions     = preconditions;

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
                    Signatures,
                    Preconditions);

        #endregion



        #region Parse    (Text, Limits = null)

        /// <summary>
        /// Parse the given JSON text of a change set.
        /// Dates stay text, so signed values keep their exact representation.
        /// </summary>
        /// <param name="Text">The JSON text to parse.</param>
        /// <param name="Limits">Optional limits, the default ones otherwise.</param>
        public static RoamingNetworkChangeSet Parse(String                          Text,
                                                    RoamingNetworkChangeSetLimits?  Limits   = null)
        {

            if (TryParse(Text,
                         out var changeSet,
                         out var errorResponse,
                         Limits))
            {
                return changeSet;
            }

            throw new ArgumentException("The given JSON representation of a change set is invalid: " + errorResponse,
                                        nameof(Text));

        }

        #endregion

        #region TryParse (Text, out ChangeSet, out ErrorResponse, Limits = null)

        /// <summary>
        /// Try to parse the given JSON text of a change set.
        /// Dates stay text, so signed values keep their exact representation.
        /// </summary>
        /// <param name="Text">The JSON text to parse.</param>
        /// <param name="ChangeSet">The parsed change set.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Limits">Optional limits, the default ones otherwise.</param>
        public static Boolean TryParse(String                                             Text,
                                       [NotNullWhen(true)]  out RoamingNetworkChangeSet?  ChangeSet,
                                       [NotNullWhen(false)] out String?                   ErrorResponse,
                                       RoamingNetworkChangeSetLimits?                     Limits   = null)
        {

            ChangeSet  = null;
            Limits   ??= RoamingNetworkChangeSetLimits.Default;

            try
            {

                if (Text is null)
                {
                    ErrorResponse = "The given JSON text must not be null!";
                    return false;
                }

                // Checked before parsing: a character takes at least one UTF-8 byte.
                if (Text.Length > Limits.MaxBytes || Encoding.UTF8.GetByteCount(Text) > Limits.MaxBytes)
                {
                    ErrorResponse = $"The change set exceeds the limit of {Limits.MaxBytes} bytes!";
                    return false;
                }

                if (POIJSON.Read(Text, Limits.MaxDepth) is not JObject json)
                {
                    ErrorResponse = "The given JSON text must be a JSON object!";
                    return false;
                }

                return TryParse(json,
                                out ChangeSet,
                                out ErrorResponse,
                                Limits);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region ParseCBOR    (CBOR, Limits = null)

        /// <summary>
        /// Parse the given CBOR bytes of a change set.
        /// </summary>
        /// <param name="CBOR">The CBOR bytes to parse.</param>
        /// <param name="Limits">Optional limits, the default ones otherwise.</param>
        public static RoamingNetworkChangeSet ParseCBOR(ReadOnlySpan<Byte>              CBOR,
                                                        RoamingNetworkChangeSetLimits?  Limits   = null)
        {

            if (TryParseCBOR(CBOR,
                             out var changeSet,
                             out var errorResponse,
                             Limits))
            {
                return changeSet;
            }

            throw new ArgumentException("The given CBOR representation of a change set is invalid: " + errorResponse,
                                        nameof(CBOR));

        }

        #endregion

        #region TryParseCBOR (CBOR, out ChangeSet, out ErrorResponse, Limits = null)

        /// <summary>
        /// Try to parse the given CBOR bytes of a change set.
        /// It has the keys and values of the JSON representation, and the same signatures.
        /// </summary>
        /// <param name="CBOR">The CBOR bytes to parse.</param>
        /// <param name="ChangeSet">The parsed change set.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Limits">Optional limits, the default ones otherwise.</param>
        public static Boolean TryParseCBOR(ReadOnlySpan<Byte>                                 CBOR,
                                           [NotNullWhen(true)]  out RoamingNetworkChangeSet?  ChangeSet,
                                           [NotNullWhen(false)] out String?                   ErrorResponse,
                                           RoamingNetworkChangeSetLimits?                     Limits   = null)
        {

            ChangeSet  = null;
            Limits   ??= RoamingNetworkChangeSetLimits.Default;

            if (CBOR.Length > Limits.MaxBytes)
            {
                ErrorResponse = $"The change set exceeds the limit of {Limits.MaxBytes} bytes!";
                return false;
            }

            if (!CBORValue.TryParse(CBOR,
                                    out var cbor,
                                    out var cborError,
                                    new CBORReaderOptions { MaxDepth = Limits.MaxDepth }))
            {
                ErrorResponse = "Invalid CBOR: " + cborError;
                return false;
            }

            return TryParse(cbor,
                            out ChangeSet,
                            out ErrorResponse,
                            Limits);

        }

        #endregion

        #region TryParse     (CBOR, out ChangeSet, out ErrorResponse, Limits = null)

        /// <summary>
        /// Try to parse the given CBOR representation of a change set.
        /// It has the keys and values of the JSON representation, and the same signatures.
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="ChangeSet">The parsed change set.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Limits">Optional limits, the default ones otherwise.</param>
        public static Boolean TryParse(CBORValue                                          CBOR,
                                       [NotNullWhen(true)]  out RoamingNetworkChangeSet?  ChangeSet,
                                       [NotNullWhen(false)] out String?                   ErrorResponse,
                                       RoamingNetworkChangeSetLimits?                     Limits   = null)
        {

            ChangeSet  = null;
            Limits   ??= RoamingNetworkChangeSetLimits.Default;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = "The CBOR representation of a change set must be a map!";
                    return false;
                }

                if (CBORJSON.ToJSON(CBOR, new CBORJSONOptions { MaxDepth = Limits.MaxDepth }) is not JObject json)
                {
                    ErrorResponse = "The CBOR representation of a change set must be a map with text keys!";
                    return false;
                }

                return TryParse(json,
                                out ChangeSet,
                                out ErrorResponse,
                                Limits);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region Parse    (JSON, Limits = null)

        /// <summary>
        /// Parse the given JSON representation of a change set.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Limits">Optional limits, the default ones otherwise.</param>
        public static RoamingNetworkChangeSet Parse(JObject                         JSON,
                                                    RoamingNetworkChangeSetLimits?  Limits   = null)
        {

            if (TryParse(JSON,
                         out var changeSet,
                         out var errorResponse,
                         Limits))
            {
                return changeSet;
            }

            throw new ArgumentException("The given JSON representation of a change set is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region TryParse (JSON, out ChangeSet, out ErrorResponse, Limits = null)

        /// <summary>
        /// Try to parse the given JSON representation of a change set.
        /// Parse JSON text with Parse(Text), as dates read as dates
        /// would change the signed representation of their values.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChangeSet">The parsed change set.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Limits">Optional limits, the default ones otherwise.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkChangeSet?  ChangeSet,
                                       [NotNullWhen(false)] out String?                   ErrorResponse,
                                       RoamingNetworkChangeSetLimits?                     Limits   = null)
        {

            ChangeSet  = null;
            Limits   ??= RoamingNetworkChangeSetLimits.Default;

            try
            {

                if (JSON is null)
                {
                    ErrorResponse = "The given JSON object must not be null!";
                    return false;
                }

                #region Limits

                if (Depth(JSON) > Limits.MaxDepth)
                {
                    ErrorResponse = $"The change set exceeds the limit of a nesting depth of {Limits.MaxDepth}!";
                    return false;
                }

                if (JSON[PropertyKeys.Changes] is JArray changesToCount && changesToCount.Count > Limits.MaxChanges)
                {
                    ErrorResponse = $"The change set exceeds the limit of {Limits.MaxChanges} changes!";
                    return false;
                }

                if (JSON[PropertyKeys.Signatures] is JArray signaturesToCount && signaturesToCount.Count > Limits.MaxSignatures)
                {
                    ErrorResponse = $"The change set exceeds the limit of {Limits.MaxSignatures} signatures!";
                    return false;
                }

                if (JSON[PropertyKeys.Preconditions] is JArray preconditionsToCount && preconditionsToCount.Count > Limits.MaxPreconditions)
                {
                    ErrorResponse = $"The change set exceeds the limit of {Limits.MaxPreconditions} preconditions!";
                    return false;
                }

                #endregion

                #region Parse Id                  [mandatory]

                if (!JSON.ParseMandatory(PropertyKeys.Id,
                                         "change set identification",
                                         ChangeSet_Id.TryParse,
                                         out ChangeSet_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse Context             [optional]

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the change set '{id}' must be '{JSONLDContext}'!";
                    return false;
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

                #region Parse CreatedAt           [mandatory]

                if (!JSON.ParseMandatory(PropertyKeys.CreatedAt,
                                         "creation timestamp",
                                         out DateTimeOffset createdAt,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse Changes             [mandatory]

                if (JSON[PropertyKeys.Changes] is not JArray changesJSON)
                {
                    ErrorResponse = $"The {PropertyKeys.Changes} must be a JSON array!";
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

                if (!TryParseETags(JSON, PropertyKeys.BeforeETags, out var beforeETags, out ErrorResponse))
                    return false;

                #endregion

                #region Parse AfterETags          [mandatory]

                if (!TryParseETags(JSON, PropertyKeys.AfterETags,  out var afterETags,  out ErrorResponse))
                    return false;

                #endregion

                #region Parse Description         [optional]

                if (JSON.ParseOptionalJSON(PropertyKeys.Description,
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

                var metadata = JSON[PropertyKeys.Metadata];

                if (metadata is not null && metadata is not JObject)
                {
                    ErrorResponse = $"The {PropertyKeys.Metadata} must be a JSON object!";
                    return false;
                }

                #endregion

                #region Parse Preconditions       [optional]

                var preconditions = ImmutableArray.CreateBuilder<RoamingNetworkPrecondition>();

                if (JSON[PropertyKeys.Preconditions] is JToken preconditionsJSON)
                {

                    if (preconditionsJSON is not JArray preconditionsArray)
                    {
                        ErrorResponse = $"The {PropertyKeys.Preconditions} must be a JSON array!";
                        return false;
                    }

                    foreach (var preconditionJSON in preconditionsArray)
                    {

                        if (preconditionJSON is not JObject preconditionObject ||
                            !RoamingNetworkPrecondition.TryParse(preconditionObject, out var precondition, out var preconditionError))
                        {
                            ErrorResponse = $"Invalid precondition {preconditions.Count}!";
                            return false;
                        }

                        preconditions.Add(precondition);

                    }

                }

                #endregion

                #region Parse Signatures          [optional]

                var signatures = ImmutableArray<RoamingNetworkChangeSetSignature>.Empty;

                if (JSON[PropertyKeys.Signatures] is JToken signaturesJSON)
                {

                    if (signaturesJSON is not JArray signaturesArray)
                    {
                        ErrorResponse = $"The {PropertyKeys.Signatures} must be a JSON array!";
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
                                signatures,
                                preconditions.ToImmutable()
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

        #region ToCBOR   (Embedded = false, IncludeSignatures = true, CustomRoamingNetworkChangeSetSerializer = null)

        /// <summary>
        /// Return the CBOR representation of this change set: the keys and values of its JSON representation.
        /// Its signatures stay valid, as they sign the JSON representation, which the CBOR one reads back exactly.
        /// Write it with CBORWriterOptions.Canonical for deterministic bytes.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeSignatures">Whether to include the signatures.</param>
        /// <param name="CustomRoamingNetworkChangeSetSerializer">An optional custom serializer.</param>
        public CBORValue ToCBOR(Boolean                                                 Embedded                                  = false,
                                Boolean                                                 IncludeSignatures                         = true,
                                CustomCBORSerializerDelegate<RoamingNetworkChangeSet>?  CustomRoamingNetworkChangeSetSerializer   = null)
        {

            var cbor = new CBORMap {
                           { CBORValue.FromText(PropertyKeys.Id),                CBORValue.FromText(Id.              ToString()) }
                       };

            if (!Embedded)
                cbor.Add(CBORValue.FromText(PropertyKeys.Context),           CBORValue.FromText(JSONLDContext));

            cbor.Add(CBORValue.FromText(PropertyKeys.RoamingNetworkId),      CBORValue.FromText(RoamingNetworkId.ToString()));
            cbor.Add(CBORValue.FromText(PropertyKeys.CreatedAt),             CBORValue.FromText(CreatedAt.       ToISO8601()));

            if (Description.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(PropertyKeys.Description),       Description.ToCBOR());

            // The metadata, the preconditions, the signatures and the changes have no CBOR representation of their own:
            // theirs are the keys and values of their JSON representation, with whole numbers as integers as in ToJSON().
            if (metadata.HasValues)
                cbor.Add(CBORValue.FromText(PropertyKeys.Metadata),          CBORJSON.ToCBOR(POIJSON.WholeNumbersAsIntegers(metadata.DeepClone())));

            if (Preconditions.Length > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.Preconditions),     CBORValue.FromArray(Preconditions.Select(precondition => CBORJSON.ToCBOR(precondition.ToJSON(Embedded: true)))));

            if (IncludeSignatures && Signatures.Length > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.Signatures),        CBORValue.FromArray(Signatures.   Select(signature    => CBORJSON.ToCBOR(signature.   ToJSON(Embedded: true)))));

            cbor.Add(CBORValue.FromText(PropertyKeys.Changes),               CBORValue.FromArray(Changes.      Select(change       => CBORJSON.ToCBOR(POIJSON.WholeNumbersAsIntegers(change.ToJSON(Embedded: true))))));
            cbor.Add(CBORValue.FromText(PropertyKeys.BeforeETags),           CBORValue.FromArray(BeforeETags.  Select(eTag         => eTag.ToTextCBOR())));
            cbor.Add(CBORValue.FromText(PropertyKeys.AfterETags),            CBORValue.FromArray(AfterETags.   Select(eTag         => eTag.ToTextCBOR())));

            return CustomRoamingNetworkChangeSetSerializer is not null
                       ? CustomRoamingNetworkChangeSetSerializer(this, cbor)
                       : cbor;

        }

        #endregion

        #region (private static) Depth(JSON)

        /// <summary>
        /// The nesting depth of the given JSON value: 0 for a primitive value, 1 for an object or array of them.
        /// </summary>
        private static Int32 Depth(JToken JSON)

            => JSON switch {
                   JObject json  => 1 + json.Properties().Select(property => Depth(property.Value)).DefaultIfEmpty(0).Max(),
                   JArray  json  => 1 + json.             Select(Depth).                            DefaultIfEmpty(0).Max(),
                   _             => 0
               };

        #endregion

        #region ToJSON   (Embedded = false, IncludeSignatures = true, CustomRoamingNetworkChangeSetSerializer = null)

        /// <summary>
        /// Return a JSON representation of this change set.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeSignatures">Whether to include the signatures; the signed content is without them.</param>
        /// <param name="CustomRoamingNetworkChangeSetSerializer">An optional custom serializer.</param>
        public JObject ToJSON(Boolean                                                    Embedded                                  = false,
                              Boolean                                                    IncludeSignatures                         = true,
                              CustomJObjectSerializerDelegate<RoamingNetworkChangeSet>?  CustomRoamingNetworkChangeSetSerializer   = null)
        {

            var json = JSONObject.Create(

                                 new JProperty(PropertyKeys.Id,                 Id.              ToString()),

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,            JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.RoamingNetworkId,   RoamingNetworkId.ToString()),
                                 new JProperty(PropertyKeys.CreatedAt,          CreatedAt.       ToISO8601()),

                           Description.IsNotNullOrEmpty()
                               ? new JProperty(PropertyKeys.Description,        Description.ToJSON())
                               : null,

                           metadata.HasValues
                               ? new JProperty(PropertyKeys.Metadata,           metadata.DeepClone())
                               : null,

                           Preconditions.Length > 0
                               ? new JProperty(PropertyKeys.Preconditions,      new JArray(Preconditions.Select(precondition => precondition.ToJSON(Embedded: true))))
                               : null,

                           IncludeSignatures && Signatures.Length > 0
                               ? new JProperty(PropertyKeys.Signatures,         new JArray(Signatures.Select(signature => signature.ToJSON(Embedded: true))))
                               : null

                       );

            // Added directly: JSONObject.Create leaves out empty arrays, but they are mandatory here.
            json.Add(PropertyKeys.Changes,      new JArray(Changes.    Select(change => change.ToJSON(Embedded: true))));
            json.Add(PropertyKeys.BeforeETags,  new JArray(BeforeETags.Select(eTag   => eTag.  ToJSON())));
            json.Add(PropertyKeys.AfterETags,   new JArray(AfterETags. Select(eTag   => eTag.  ToJSON())));

            // Whole numbers as integers, so that ToString() of it is read back with its signatures, and a commit of it
            // with its identification; see POIJSON.
            POIJSON.WholeNumbersAsIntegers(json);

            return CustomRoamingNetworkChangeSetSerializer is not null
                       ? CustomRoamingNetworkChangeSetSerializer(this, json)
                       : json;

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
                    eTags.Add(ETag.ParseHex(eTagJSON));
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
