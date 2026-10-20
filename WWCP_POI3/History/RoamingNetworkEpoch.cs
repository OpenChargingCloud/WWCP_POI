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
    /// commits and checkpoints and of the trees of checkpoints, the profiles of commits, checkpoints and price statements,
    /// and the signature algorithms accepted. Only what this implementation can calculate is accepted.
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

        #endregion

        #region Properties

        /// <summary>
        /// The canonicalization of everything hashed and signed.
        /// </summary>
        public String                  Canonicalization        { get; }

        /// <summary>
        /// The hash of the identifications of commits and checkpoints, and of the trees of checkpoints.
        /// </summary>
        public ETagHashAlgorithm       IdentificationHash      { get; }

        /// <summary>
        /// The profile of the content of commits.
        /// </summary>
        public String                  Commit                  { get; }

        /// <summary>
        /// The profile of the content of checkpoints.
        /// </summary>
        public String                  Checkpoint              { get; }

        /// <summary>
        /// The profile of price statements.
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
                   RoamingNetworkCommit.IdentityProfile,
                   RoamingNetworkCheckpoint.IdentityProfile,
                   RoamingNetworkPriceStatement.SigningProfile,
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
                if (property.Name is not ("canonicalization" or "identificationHash" or "commit" or "checkpoint" or "priceStatement" or "signatureAlgorithms"))
                {
                    ErrorResponse = $"Unknown profile '{property.Name}' of an epoch!";
                    return false;
                }

            if (Text(json, "canonicalization") != StyxCanonicalJSON)
            {
                ErrorResponse = $"The canonicalization of an epoch must be '{StyxCanonicalJSON}', the one this implementation calculates!";
                return false;
            }

            if (!ETagHashAlgorithmExtensions.TryParse(Text(json, "identificationHash"), out var identificationHash))
            {
                ErrorResponse = "The identification hash of an epoch must be sha256, sha384 or sha512!";
                return false;
            }

            if (Text(json, "commit")         != RoamingNetworkCommit.        IdentityProfile ||
                Text(json, "checkpoint")     != RoamingNetworkCheckpoint.    IdentityProfile ||
                Text(json, "priceStatement") != RoamingNetworkPriceStatement.SigningProfile)
            {
                ErrorResponse = $"The profiles of commits, checkpoints and price statements of an epoch must be '{RoamingNetworkCommit.IdentityProfile}', " +
                                $"'{RoamingNetworkCheckpoint.IdentityProfile}' and '{RoamingNetworkPriceStatement.SigningProfile}', the ones this implementation calculates!";
                return false;
            }

            var signatureAlgorithms = new List<String>();

            if (json["signatureAlgorithms"] is JToken signatureAlgorithmsJSON)
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
                                                             RoamingNetworkCommit.IdentityProfile,
                                                             RoamingNetworkCheckpoint.IdentityProfile,
                                                             RoamingNetworkPriceStatement.SigningProfile,
                                                             signatureAlgorithms);
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of these profiles.
        /// </summary>
        public JObject ToJSON()

            => new (
                   new JProperty("canonicalization",     Canonicalization),
                   new JProperty("identificationHash",   IdentificationHash.ToText()),
                   new JProperty("commit",               Commit),
                   new JProperty("checkpoint",           Checkpoint),
                   new JProperty("priceStatement",       PriceStatement),
                   new JProperty("signatureAlgorithms",  new JArray(SignatureAlgorithms))
               );

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
               ToJSON().ToString(Formatting.None) == Profiles.ToJSON().ToString(Formatting.None);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is RoamingNetworkEpochProfiles profiles &&
               Equals(profiles);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => ToJSON().ToString(Formatting.None).GetHashCode();

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
        public JObject ToJSON()

            => new (
                   new JProperty("epoch",                                 Number),
                   new JProperty(RoamingNetworkEpochProfiles.JSONProperty,  Profiles.ToJSON())
               );

        /// <summary>
        /// Try to parse the given JSON representation of an epoch: { epoch, profiles }.
        /// </summary>
        public static Boolean TryParse(JToken?                                        JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkEpoch?  Epoch,
                                       [NotNullWhen(false)] out String?               ErrorResponse)
        {

            Epoch = null;

            if (JSON is not JObject json ||
                json["epoch"] is not JValue { Type: JTokenType.Integer } number ||
                number.Value<Decimal>() < 1 || number.Value<Decimal>() > UInt32.MaxValue)
            {
                ErrorResponse = "An epoch must be { epoch, profiles }, its number from 1 on!";
                return false;
            }

            if (!RoamingNetworkEpochProfiles.TryParse(json[RoamingNetworkEpochProfiles.JSONProperty], out var profiles, out ErrorResponse))
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
        public const String JSONProperty = "previous";

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
        public JObject ToJSON()

            => new (
                   new JProperty("epoch",         Epoch),
                   new JProperty("checkpointId",  CheckpointId.ToString()),
                   new JProperty("eTags",         new JArray(ETags.Select(eTag => eTag.ToJSON())))
               );

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
                    json["epoch"] is not JValue { Type: JTokenType.Integer } epoch ||
                    epoch.Value<Decimal>() < 1 || epoch.Value<Decimal>() > UInt32.MaxValue ||
                    !RoamingNetworkCheckpoint_Id.TryParse(json["checkpointId"]?.Type == JTokenType.String ? json["checkpointId"]!.Value<String>() : null, out var checkpointId) ||
                    json["eTags"] is not JArray { Count: > 0 } eTagsJSON)
                {
                    ErrorResponse = "The epoch before must be { epoch, checkpointId, eTags }!";
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
