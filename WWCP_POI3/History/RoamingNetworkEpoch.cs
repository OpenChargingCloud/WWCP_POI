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
    /// Where an epoch of a history is: taking commits, closed by its final checkpoint, or followed by the next epoch.
    /// </summary>
    public enum RoamingNetworkEpochStatus
    {

        /// <summary>
        /// It takes commits.
        /// </summary>
        Active,

        /// <summary>
        /// Its final checkpoint is there: it takes no commits any more, the next epoch is not started here yet,
        /// and witnesses still sign the final checkpoint.
        /// </summary>
        Closing,

        /// <summary>
        /// The next epoch is started here from its final checkpoint; it stays readable.
        /// </summary>
        Closed

    }


    /// <summary>
    /// The profiles of an epoch (SPECIFICATION.md, section 11): the canonicalization, the hash of the identifications of
    /// commits and checkpoints and of the trees of checkpoints, the JSON-LD contexts of commits, checkpoints and price
    /// statements, which name their versions, and the signature algorithms accepted. Only what this implementation can calculate is accepted.
    /// </summary>
    public sealed class RoamingNetworkEpochProfiles : IEquatable<RoamingNetworkEpochProfiles>
    {

        #region Data

        /// <summary>
        /// The JSON key of the profiles.
        /// </summary>
        public const String  JSONProperty                     = "profiles";

        /// <summary>
        /// The canonicalization of this implementation: Styx canonical JSON.
        /// </summary>
        public const String  StyxCanonicalJSON                = "styx-canonical-json-v1";

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String  JSONLDContext                    = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/epochProfiles/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context              = "@context";

            /// <summary>
            /// The property of its canonicalization.
            /// </summary>
            public const String  Canonicalization     = "canonicalization";

            /// <summary>
            /// The property of its identification hash.
            /// </summary>
            public const String  IdentificationHash   = "identificationHash";

            /// <summary>
            /// The property of the JSON-LD context of its commits.
            /// </summary>
            public const String  Commit               = "commit";

            /// <summary>
            /// The property of the JSON-LD context of its checkpoints.
            /// </summary>
            public const String  Checkpoint           = "checkpoint";

            /// <summary>
            /// The property of the JSON-LD context of its price statements.
            /// </summary>
            public const String  PriceStatement       = "priceStatement";

            /// <summary>
            /// The property of its signature algorithms.
            /// </summary>
            public const String  SignatureAlgorithms  = "signatureAlgorithms";

        }

        #endregion


        /// <summary>
        /// The canonicalization of everything hashed and signed.
        /// </summary>
        public String                  Canonicalization        { get; }

        /// <summary>
        /// The hash of the identifications of commits and checkpoints, and of the trees of checkpoints.
        /// </summary>
        public ETagHashAlgorithm       IdentificationHash      { get; }

        /// <summary>
        /// The JSON-LD context of commits, which names the version of their content.
        /// </summary>
        public String                  Commit                  { get; }

        /// <summary>
        /// The JSON-LD context of checkpoints, which names the version of their content.
        /// </summary>
        public String                  Checkpoint              { get; }

        /// <summary>
        /// The JSON-LD context of price statements, which names their version.
        /// </summary>
        public String                  PriceStatement          { get; }

        /// <summary>
        /// The COSE names of the signature algorithms accepted, in their ordinal order: every one this implementation signs
        /// with, when there are none.
        /// </summary>
        public ImmutableArray<String>  SignatureAlgorithms     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the profiles of an epoch, with the canonicalization and the profiles of this implementation.
        /// </summary>
        /// <param name="IdentificationHash">The hash of identifications and of the trees of checkpoints.</param>
        /// <param name="SignatureAlgorithms">The COSE names of the signature algorithms accepted, every one when none.</param>
        public RoamingNetworkEpochProfiles(ETagHashAlgorithm     IdentificationHash    = ETagHashAlgorithm.SHA256,
                                           IEnumerable<String>?  SignatureAlgorithms   = null)

            : this(StyxCanonicalJSON,
                   IdentificationHash,
                   RoamingNetworkCommit.JSONLDContext,
                   RoamingNetworkCheckpoint.JSONLDContext,
                   RoamingNetworkPriceStatement.JSONLDContext,
                   SignatureAlgorithms)

        { }

        private RoamingNetworkEpochProfiles(String                Canonicalization,
                                            ETagHashAlgorithm     IdentificationHash,
                                            String                Commit,
                                            String                Checkpoint,
                                            String                PriceStatement,
                                            IEnumerable<String>?  SignatureAlgorithms)
        {

            var signatureAlgorithms = (SignatureAlgorithms ?? []).Distinct().Order(StringComparer.Ordinal).ToArray();

            foreach (var signatureAlgorithm in signatureAlgorithms)
                if (!COSEAlgorithm.TryParse(signatureAlgorithm, out var algorithm) || algorithm.Name != signatureAlgorithm || !algorithm.IsSupportedForSigning)
                    throw new ArgumentException($"'{signatureAlgorithm}' is no signature algorithm this implementation signs with.", nameof(SignatureAlgorithms));

            this.Canonicalization     = Canonicalization;
            this.IdentificationHash   = IdentificationHash;
            this.Commit               = Commit;
            this.Checkpoint           = Checkpoint;
            this.PriceStatement       = PriceStatement;
            this.SignatureAlgorithms  = [ .. signatureAlgorithms ];

        }

        #endregion


        #region (static) Default

        /// <summary>
        /// The profiles of the first epoch: SHA-256 identifications, every signature algorithm.
        /// </summary>
        public static RoamingNetworkEpochProfiles Default { get; } = new ();

        #endregion

        #region AllowsSignatureAlgorithm(Algorithm)

        /// <summary>
        /// Whether the given signature algorithm, by its COSE name, is accepted in the epoch.
        /// </summary>
        public Boolean AllowsSignatureAlgorithm(String Algorithm)

            => SignatureAlgorithms.IsEmpty
                   ? COSEAlgorithm.TryParse(Algorithm, out var algorithm) && algorithm.IsSupportedForSigning
                   : SignatureAlgorithms.Contains(Algorithm);

        #endregion


        #region (static) TryParse(JSON, out Profiles, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of the profiles of an epoch: only those this implementation can
        /// calculate.
        /// </summary>
        public static Boolean TryParse(JToken?                                                JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkEpochProfiles?  Profiles,
                                       [NotNullWhen(false)] out String?                       ErrorResponse)
        {

            Profiles = null;

            if (JSON is not JObject json)
            {
                ErrorResponse = "The profiles of an epoch must be a JSON object!";
                return false;
            }

            foreach (var property in json.Properties())
                if (property.Name is not (PropertyKeys.Context or PropertyKeys.Canonicalization or PropertyKeys.IdentificationHash or PropertyKeys.Commit or
                                          PropertyKeys.Checkpoint or PropertyKeys.PriceStatement or PropertyKeys.SignatureAlgorithms))
                {
                    ErrorResponse = $"Unknown profile '{property.Name}' of an epoch!";
                    return false;
                }

            if (json[PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of the profiles of an epoch must be '{JSONLDContext}'!";
                return false;
            }

            if (Text(json, PropertyKeys.Canonicalization) != StyxCanonicalJSON)
            {
                ErrorResponse = $"The canonicalization of an epoch must be '{StyxCanonicalJSON}', the one this implementation calculates!";
                return false;
            }

            if (!ETagHashAlgorithmExtensions.TryParse(Text(json, PropertyKeys.IdentificationHash), out var identificationHash))
            {
                ErrorResponse = "The identification hash of an epoch must be sha256, sha384 or sha512!";
                return false;
            }

            if (Text(json, PropertyKeys.Commit)         != RoamingNetworkCommit.        JSONLDContext ||
                Text(json, PropertyKeys.Checkpoint)     != RoamingNetworkCheckpoint.    JSONLDContext ||
                Text(json, PropertyKeys.PriceStatement) != RoamingNetworkPriceStatement.JSONLDContext)
            {
                ErrorResponse = $"The JSON-LD contexts of commits, checkpoints and price statements of an epoch must be '{RoamingNetworkCommit.JSONLDContext}', " +
                                $"'{RoamingNetworkCheckpoint.JSONLDContext}' and '{RoamingNetworkPriceStatement.JSONLDContext}', the ones this implementation calculates!";
                return false;
            }

            var signatureAlgorithms = new List<String>();

            if (json[PropertyKeys.SignatureAlgorithms] is JToken signatureAlgorithmsJSON)
            {

                if (signatureAlgorithmsJSON is not JArray signatureAlgorithmsArray ||
                    signatureAlgorithmsArray.Any(algorithm => algorithm.Type != JTokenType.String))
                {
                    ErrorResponse = "The signature algorithms of an epoch must be a JSON array of COSE names!";
                    return false;
                }

                foreach (var name in signatureAlgorithmsArray.Values<String>())
                {

                    if (name is null || !COSEAlgorithm.TryParse(name, out var algorithm) || algorithm.Name != name || !algorithm.IsSupportedForSigning)
                    {
                        ErrorResponse = $"'{name}' is no signature algorithm this implementation signs with!";
                        return false;
                    }

                    signatureAlgorithms.Add(name);

                }

            }

            Profiles       = new RoamingNetworkEpochProfiles(StyxCanonicalJSON,
                                                             identificationHash,
                                                             RoamingNetworkCommit.JSONLDContext,
                                                             RoamingNetworkCheckpoint.JSONLDContext,
                                                             RoamingNetworkPriceStatement.JSONLDContext,
                                                             signatureAlgorithms);
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region ToJSON(Embedded = false, CustomEpochProfilesSerializer = null)

        /// <summary>
        /// Return the JSON representation of these profiles.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomEpochProfilesSerializer">An optional custom serializer of the profiles of an epoch.</param>
        public JObject ToJSON(Boolean                                                        Embedded                        = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkEpochProfiles>?  CustomEpochProfilesSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,              JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.Canonicalization,     Canonicalization),
                                 new JProperty(PropertyKeys.IdentificationHash,   IdentificationHash.ToText()),
                                 new JProperty(PropertyKeys.Commit,               Commit),
                                 new JProperty(PropertyKeys.Checkpoint,           Checkpoint),
                                 new JProperty(PropertyKeys.PriceStatement,       PriceStatement),
                                 new JProperty(PropertyKeys.SignatureAlgorithms,  new JArray(SignatureAlgorithms))

                       );

            return CustomEpochProfilesSerializer is not null
                       ? CustomEpochProfilesSerializer(this, json)
                       : json;

        }

        #endregion


        #region (private static) Text(JSON, PropertyName)

        private static String? Text(JObject JSON, String PropertyName)

            => JSON[PropertyName]?.Type == JTokenType.String
                   ? JSON[PropertyName]!.Value<String>()
                   : null;

        #endregion


        #region IEquatable<RoamingNetworkEpochProfiles> Members

        /// <inheritdoc/>
        public Boolean Equals(RoamingNetworkEpochProfiles? Profiles)

            => Profiles is not null &&
               ToJSON(Embedded: true).ToString(Formatting.None) == Profiles.ToJSON(Embedded: true).ToString(Formatting.None);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is RoamingNetworkEpochProfiles profiles &&
               Equals(profiles);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => ToJSON(Embedded: true).ToString(Formatting.None).GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => $"{IdentificationHash.ToText()} identifications, {(SignatureAlgorithms.IsEmpty ? "every signature algorithm" : String.Join(", ", SignatureAlgorithms))}";

        #endregion

    }


    /// <summary>
    /// An epoch of a history: its number, from 1 on, and its profiles (SPECIFICATION.md, section 11).
    /// </summary>
    /// <param name="Number">The number of the epoch, from 1 on.</param>
    /// <param name="Profiles">Its profiles.</param>
    public sealed record RoamingNetworkEpoch(UInt32                       Number,
                                             RoamingNetworkEpochProfiles  Profiles)
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/epoch/v1";

        #endregion

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context   = "@context";

            /// <summary>
            /// The property of its number.
            /// </summary>
            public const String  Epoch     = "epoch";

            /// <summary>
            /// The property of its profiles.
            /// </summary>
            public const String  Profiles  = RoamingNetworkEpochProfiles.JSONProperty;

        }

        #endregion


        /// <summary>
        /// The first epoch of every history: SHA-256 identifications, every signature algorithm.
        /// </summary>
        public static RoamingNetworkEpoch First { get; } = new (1, RoamingNetworkEpochProfiles.Default);

        /// <summary>
        /// The next epoch, with the given profiles.
        /// </summary>
        /// <param name="Profiles">The profiles of the next epoch.</param>
        public RoamingNetworkEpoch Next(RoamingNetworkEpochProfiles Profiles)

            => new (Number + 1, Profiles);

        /// <summary>
        /// Return the JSON representation of this epoch: { epoch, profiles }.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomEpochSerializer">An optional custom serializer of epochs.</param>
        public JObject ToJSON(Boolean                                                Embedded                = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkEpoch>?  CustomEpochSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,   JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.Epoch,     Number),
                                 new JProperty(PropertyKeys.Profiles,  Profiles.ToJSON(Embedded: true))

                       );

            return CustomEpochSerializer is not null
                       ? CustomEpochSerializer(this, json)
                       : json;

        }

        /// <summary>
        /// Try to parse the given JSON representation of an epoch: { epoch, profiles }.
        /// </summary>
        public static Boolean TryParse(JToken?                                        JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkEpoch?  Epoch,
                                       [NotNullWhen(false)] out String?               ErrorResponse)
        {

            Epoch = null;

            if (JSON is not JObject json ||
                json[PropertyKeys.Epoch] is not JValue { Type: JTokenType.Integer } number ||
                number.Value<Decimal>() < 1 || number.Value<Decimal>() > UInt32.MaxValue)
            {
                ErrorResponse = "An epoch must be { epoch, profiles }, its number from 1 on!";
                return false;
            }

            if (json[PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of the epoch must be '{JSONLDContext}'!";
                return false;
            }

            if (!RoamingNetworkEpochProfiles.TryParse(json[PropertyKeys.Profiles], out var profiles, out ErrorResponse))
                return false;

            Epoch = new RoamingNetworkEpoch(number.Value<UInt32>(), profiles);
            return true;

        }

    }


    /// <summary>
    /// What the first checkpoint of an epoch names of the epoch before (SPECIFICATION.md, section 11): its number, its
    /// final checkpoint and the ETags of the version of the commit of that checkpoint, the version the epoch starts from.
    /// </summary>
    /// <param name="Epoch">The number of the epoch before.</param>
    /// <param name="CheckpointId">Its final checkpoint.</param>
    /// <param name="ETags">The ETags of the version of the commit of the final checkpoint.</param>
    public sealed record RoamingNetworkEpochLink(UInt32                       Epoch,
                                                 RoamingNetworkCheckpoint_Id  CheckpointId,
                                                 ImmutableArray<ETag>         ETags)
    {

        /// <summary>
        /// The JSON key of the link in a checkpoint.
        /// </summary>
        public const String JSONProperty   = "previous";

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext  = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/epochLink/v1";

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context       = "@context";

            /// <summary>
            /// The property of the number of the epoch before.
            /// </summary>
            public const String  Epoch         = "epoch";

            /// <summary>
            /// The property of the final checkpoint of the epoch before.
            /// </summary>
            public const String  CheckpointId  = "checkpointId";

            /// <summary>
            /// The property of the ETags of the version of the commit of the final checkpoint.
            /// </summary>
            public const String  ETags         = "eTags";

        }

        #endregion

        /// <summary>
        /// The link to the given final checkpoint.
        /// </summary>
        public static RoamingNetworkEpochLink To(RoamingNetworkCheckpoint FinalCheckpoint)

            => new (FinalCheckpoint.Epoch, FinalCheckpoint.Id, FinalCheckpoint.ETags);

        /// <summary>
        /// Whether it names the same as the given one.
        /// </summary>
        public Boolean Equals(RoamingNetworkEpochLink? Link)

            => Link is not null &&
               Epoch        == Link.Epoch        &&
               CheckpointId == Link.CheckpointId &&
               ETags.SequenceEqual(Link.ETags);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => HashCode.Combine(Epoch, CheckpointId);

        /// <summary>
        /// Return the JSON representation: { epoch, checkpointId, eTags }.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomEpochLinkSerializer">An optional custom serializer of links to the epoch before.</param>
        public JObject ToJSON(Boolean                                                    Embedded                    = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkEpochLink>?  CustomEpochLinkSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,       JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.Epoch,         Epoch),
                                 new JProperty(PropertyKeys.CheckpointId,  CheckpointId.ToString()),
                                 new JProperty(PropertyKeys.ETags,         new JArray(ETags.Select(eTag => eTag.ToJSON())))

                       );

            return CustomEpochLinkSerializer is not null
                       ? CustomEpochLinkSerializer(this, json)
                       : json;

        }

        /// <summary>
        /// Try to parse the given JSON representation: { epoch, checkpointId, eTags }.
        /// </summary>
        public static Boolean TryParse(JToken?                                            JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkEpochLink?  Link,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)
        {

            Link = null;

            try
            {

                if (JSON is not JObject json ||
                    json[PropertyKeys.Epoch] is not JValue { Type: JTokenType.Integer } epoch ||
                    epoch.Value<Decimal>() < 1 || epoch.Value<Decimal>() > UInt32.MaxValue ||
                    !RoamingNetworkCheckpoint_Id.TryParse(json[PropertyKeys.CheckpointId]?.Type == JTokenType.String ? json[PropertyKeys.CheckpointId]!.Value<String>() : null, out var checkpointId) ||
                    json[PropertyKeys.ETags] is not JArray { Count: > 0 } eTagsJSON)
                {
                    ErrorResponse = "The epoch before must be { epoch, checkpointId, eTags }!";
                    return false;
                }

                if (json[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the epoch before must be '{JSONLDContext}'!";
                    return false;
                }

                Link           = new RoamingNetworkEpochLink(epoch.Value<UInt32>(), checkpointId, [ .. eTagsJSON.Select(ETag.ParseHex) ]);
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                ErrorResponse = "Invalid epoch before: " + e.Message;
                return false;
            }

        }

    }

}
