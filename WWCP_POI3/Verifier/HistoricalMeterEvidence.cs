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
    /// A checkpoint of a historical evidence, with the roaming network operator of its time that signed it.
    /// </summary>
    /// <param name="Checkpoint">The checkpoint, as JSON.</param>
    /// <param name="Operator">The roaming network operator of its time, as JSON.</param>
    public sealed record HistoricalCheckpointEntry(JObject  Checkpoint,
                                                   JObject  Operator);


    /// <summary>
    /// A version of a charging station in a historical evidence: its proof, and where given the proof of its commit in a
    /// checkpoint and that checkpoint.
    /// </summary>
    /// <param name="ChargingStationProof">The proof of the charging station in that version.</param>
    /// <param name="CommitProof">The proof of its commit in a checkpoint, if given.</param>
    /// <param name="Checkpoints">The checkpoints, with their operators.</param>
    public sealed record HistoricalStationVersion(JObject                                    ChargingStationProof,
                                                  JObject?                                   CommitProof,
                                                  ImmutableArray<HistoricalCheckpointEntry>  Checkpoints);


    /// <summary>
    /// Where a version a historical verification used is published.
    /// </summary>
    /// <param name="CommitId">The commit of the version.</param>
    /// <param name="Publication">"missing", "rno-checkpoint" or "witnessed-checkpoint".</param>
    /// <param name="CheckpointId">The checkpoint including it, if any.</param>
    /// <param name="WitnessedBy">The trusted witnesses that signed that checkpoint.</param>
    public sealed record HistoricalVersionPublication(RoamingNetworkCommit_Id?      CommitId,
                                                      String                        Publication,
                                                      RoamingNetworkCheckpoint_Id?  CheckpointId,
                                                      ImmutableArray<String>        WitnessedBy);


    /// <summary>
    /// What a historical verification established (WWCP_POI.TS HistoricalMeterVerification): per time of a measurement, the
    /// key of the meter of the EVSE the roaming network operator attested then, and where the versions used are published.
    /// The time is the signed time of the measurements: an independently established time, that the version was the only
    /// or the latest one, and that a checkpoint was public before charging are not proven.
    /// </summary>
    /// <param name="Bindings">One binding per time of a measurement.</param>
    /// <param name="Versions">Where the versions used are published.</param>
    /// <param name="CheckedAt">When it was checked, the time of the current authorization.</param>
    public sealed record HistoricalMeterVerification(ImmutableArray<VerifiedMeterKeyBinding>       Bindings,
                                                     ImmutableArray<HistoricalVersionPublication>  Versions,
                                                     DateTimeOffset                                CheckedAt)
    {

        /// <summary>
        /// The basis of the times: the signed times of the measurements.
        /// </summary>
        public String TimeBasis        { get; } = "signed-measurement-timestamps";

        /// <summary>
        /// An independently established time: not proven.
        /// </summary>
        public String IndependentTime  { get; } = "not-proven";

        /// <summary>
        /// That the version was the latest one: not proven.
        /// </summary>
        public String LatestVersion    { get; } = "not-proven";

    }


    /// <summary>
    /// The evidence of the meters and keys of an EVSE in former versions of the roaming network (WWCP_POI.TS
    /// historicalMeterEvidence/v1), as an app keeps it with a charging session to check its measurements later: an
    /// extension of WWCP_POI.TS, not of the Evidence of SPECIFICATION.md. A missing inclusion stays visible, a malformed one
    /// fails; prices are not needed to verify the assignment of a meter.
    /// </summary>
    public sealed class HistoricalMeterEvidence
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/historicalMeterEvidence/v1";

        #endregion

        #region Properties

        /// <summary>
        /// The EVSE.
        /// </summary>
        public String                                     EVSEId      { get; }

        /// <summary>
        /// The versions of its charging station.
        /// </summary>
        public ImmutableArray<HistoricalStationVersion>   Versions    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a historical evidence of the given EVSE, e.g. of the proofs a history gives of it in former versions.
        /// </summary>
        /// <param name="EVSEId">The EVSE.</param>
        /// <param name="Versions">The versions of its charging station, at least one.</param>
        public HistoricalMeterEvidence(String                                 EVSEId,
                                       IEnumerable<HistoricalStationVersion>  Versions)
        {

            this.EVSEId    = EVSEId;
            this.Versions  = [.. Versions];

            if (this.Versions.IsEmpty)
                throw new ArgumentException("Historical station evidence is empty!", nameof(Versions));

        }

        #endregion


        #region (static) TryParse(JSON, out Evidence, out ErrorResponse)

        /// <summary>
        /// Try to read the given historical meter evidence, naming nothing else.
        /// </summary>
        /// <param name="JSON">The JSON.</param>
        /// <param name="Evidence">The evidence.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JToken?                                            JSON,
                                       [NotNullWhen(true)]  out HistoricalMeterEvidence?  Evidence,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)
        {

            Evidence = null;

            if (JSON is not JObject json)
            {
                ErrorResponse = "Historical meter evidence must be a JSON object!";
                return false;
            }

            if (!Only(json, "Historical meter evidence", out ErrorResponse, "@context", "EVSEId", "versions"))
                return false;

            if (json["@context"]?.Type != JTokenType.String || json["@context"]!.Value<String>() != JSONLDContext)
            {
                ErrorResponse = "Unsupported historical meter evidence!";
                return false;
            }

            if (json["EVSEId"]?.Type != JTokenType.String || json["versions"] is not JArray versionsJSON)
            {
                ErrorResponse = "Historical meter evidence names its EVSEId and its versions!";
                return false;
            }

            var versions = new List<HistoricalStationVersion>();

            foreach (var item in versionsJSON)
            {

                if (item is not JObject version)
                {
                    ErrorResponse = "A historical station version must be a JSON object!";
                    return false;
                }

                if (!Only(version, "A historical station version", out ErrorResponse, "chargingStationProof", "commitProof", "checkpoints"))
                    return false;

                if (version["chargingStationProof"] is not JObject proof)
                {
                    ErrorResponse = "A historical station version gives the proof of its station!";
                    return false;
                }

                if (version["commitProof"] is not null and not JObject)
                {
                    ErrorResponse = "The commit proof of a historical station version must be a JSON object!";
                    return false;
                }

                if (!TryParseCheckpoints(version["checkpoints"], "Historical checkpoints", out var checkpoints, out ErrorResponse))
                    return false;

                versions.Add(new HistoricalStationVersion(proof, version["commitProof"] as JObject, checkpoints));

            }

            if (versions.Count == 0)
            {
                ErrorResponse = "Historical station evidence is empty!";
                return false;
            }

            Evidence = new HistoricalMeterEvidence(json["EVSEId"]!.Value<String>()!, versions);
            return true;

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// The JSON of this evidence.
        /// </summary>
        public JObject ToJSON()

            => new (new JProperty("@context",  JSONLDContext),
                    new JProperty("EVSEId",    EVSEId),
                    new JProperty("versions",  new JArray(Versions.Select(version => JSONOf(version)))));

        #endregion


        #region TryVerify(Current, EVSEId, MeterSerialNumber, MeasurementTimes, Format, out Verification, out ErrorResponse, Policy = null)

        /// <summary>
        /// Verify this evidence for the given measurements of the given EVSE and meter, under the given current local
        /// authorization, apart from the historical validity: per time of a measurement, a version whose proof the roaming
        /// network operator attested then, with the key statement and certificates valid then though they ended since, and
        /// the meter of that serial number and its key, not compromised then; every version covering a time binding the same
        /// meter and key. Each version used is published where its commit proof and checkpoint are given and verify, each
        /// checkpoint with its own operator and signature times, as the policy requires.
        /// </summary>
        /// <param name="Current">The current local authorization: a trust context, or a historical one.</param>
        /// <param name="EVSEId">The EVSE.</param>
        /// <param name="MeterSerialNumber">The serial number of its meter, as the measurements name it.</param>
        /// <param name="MeasurementTimes">The signed times of the measurements.</param>
        /// <param name="Format">The format of the keys of the meter.</param>
        /// <param name="Verification">What was established.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Policy">What the verifier requires.</param>
        public Boolean TryVerify(IRoamingNetworkTrustAuthority                          Current,
                                 String                                                 EVSEId,
                                 String                                                 MeterSerialNumber,
                                 IEnumerable<DateTimeOffset>                            MeasurementTimes,
                                 MeasurementKeyFormat                                   Format,
                                 [NotNullWhen(true)]  out HistoricalMeterVerification?  Verification,
                                 [NotNullWhen(false)] out String?                       ErrorResponse,
                                 HistoricalPolicy?                                      Policy   = null)
        {

            (Verification, ErrorResponse) = Verify(Current, EVSEId, MeterSerialNumber, MeasurementTimes, Format, Policy);

            return Verification is not null;

        }

        #endregion

        #region (private) Verify(Current, EVSEId, MeterSerialNumber, MeasurementTimes, Format, Policy)

        private (HistoricalMeterVerification?, String?) Verify(IRoamingNetworkTrustAuthority  Current,
                                                               String                         EVSEId,
                                                               String                         MeterSerialNumber,
                                                               IEnumerable<DateTimeOffset>    MeasurementTimes,
                                                               MeasurementKeyFormat           Format,
                                                               HistoricalPolicy?              Policy)
        {

            static (HistoricalMeterVerification?, String?) Failed(String? Error)
                => (null, Error ?? "Unknown error!");

            var policy = Policy ?? new HistoricalPolicy();

            if (this.EVSEId != EVSEId)
                return Failed("Historical evidence belongs to another EVSE!");

            var times = MeasurementTimes.Distinct().ToArray();

            if (times.Length == 0 || times.Any(time => time > Current.EvaluatedAt))
                return Failed("Historical verification needs non-future original measurement times!");

            var versions = new List<(HistoricalStationVersion Version, RoamingNetworkProof Proof)>();

            foreach (var version in Versions)
            {
                if (!RoamingNetworkProof.TryParse(version.ChargingStationProof, out var proof, out var proofError))
                    return Failed(proofError);
                versions.Add((version, proof));
            }

            var used      = new Dictionary<String, (HistoricalStationVersion Version, RoamingNetworkProof Proof)>(StringComparer.Ordinal);
            var bindings  = new List<VerifiedMeterKeyBinding>();

            foreach (var time in times)
            {

                var matches  = new List<(VerifiedMeterKeyBinding Binding, HistoricalStationVersion Version, RoamingNetworkProof Proof)>();
                var errors   = new List<String>();

                foreach (var (version, proof) in versions)
                {

                    var operatorJSON = proof.RoamingNetwork[RoamingNetwork.PropertyKeys.Operator] as JObject;

                    if (!HistoricalTrustContext.TryGetOperatorContext(Current, operatorJSON, time, out var context, out var error) ||
                        !VerifiedMeterKeyBinding.TryVerify(context, proof, EVSEId, MeterSerialNumber, Format, out var binding, out _, out error) ||
                        !proof.TryVerify(context, out var verification, out error) ||
                        !RoamingNetworkOperatorTrust.TryCreate(context, operatorJSON!, out var trust, out error))
                    {
                        errors.Add(error);
                        continue;
                    }

                    // Compromise is applied before attestations are counted, not to signatures ignored anyway.
                    var holders = new HashSet<String>(StringComparer.Ordinal);

                    foreach (var attestation in verification.Attestations)
                        if (trust.CertificateOf(attestation.Role, attestation.KeyId, attestation.SignedAt) is RoamingNetworkSigningKeyCertificate certificate &&
                            !policy.IsRevoked(attestation.KeyId,  attestation.SignedAt) &&
                            !policy.IsRevoked(certificate.KeyId,  attestation.SignedAt) &&
                            trust.HolderOf(certificate) is String holder)
                        {
                            holders.Add(holder);
                        }

                    if (holders.Count < context.RequiredConsensus || policy.IsRevoked(binding.Key.Id, time) || policy.IsRevoked(binding.Key.CanonicalId, time))
                    {
                        errors.Add("Historical keys are affected by a caller-authorized compromise!");
                        continue;
                    }

                    matches.Add((binding, version, proof));

                }

                if (matches.Count == 0)
                    return Failed($"No historical EVSE/meter/key coverage at {time.ToISO8601()}: {String.Join("; ", errors)}");

                if (matches.Select(match => $"{match.Binding.MeterId}:{match.Binding.Key.CanonicalId}").Distinct().Count() != 1)
                    return Failed("Conflicting historical key assignments cover the same measurement time!");

                // The same material in several attested versions is no search for a key that happens to verify.
                var selected = matches.OrderBy(match => match.Binding.Evidence.CommitId?.ToString(), StringComparer.Ordinal).First();

                used[selected.Binding.Evidence.CommitId?.ToString() ?? ""] = (selected.Version, selected.Proof);

                bindings.Add(selected.Binding with {
                                 Evidence = selected.Binding.Evidence with {
                                                VerifiedAt                 = Current.EvaluatedAt,
                                                ValidUntil                 = Current.ValidUntil,
                                                KeyStatementDigest         = Current.KeyStatementDigest,
                                                HistoricalStatementDigest  = selected.Binding.Evidence.KeyStatementDigest,
                                                AttestationValidUntil      = selected.Binding.Evidence.ValidUntil,
                                                MeasurementTimes           = [ time ],
                                                Association                = VerifiedMeterKeyBinding.HistoricalAttestation
                                            }
                             });

            }

            var heads        = new Dictionary<String, RoamingNetworkCheckpoint_Id>(StringComparer.Ordinal);
            var publication  = new List<HistoricalVersionPublication>();

            foreach (var (commitId, (version, proof)) in used)
            {

                if (version.CommitProof is null)
                {

                    if (!version.Checkpoints.IsEmpty || policy.RequirePublication || policy.RequiredWitnesses > 0)
                        return Failed("Required historical commit inclusion evidence is missing!");

                    publication.Add(new HistoricalVersionPublication(proof.CommitId, "missing", null, []));
                    continue;

                }

                if (!RoamingNetworkCheckpointProof.TryParse(version.CommitProof, out var commitProof, out var error))
                    return Failed(error);

                RoamingNetworkCheckpoint? checkpoint  = null;
                JObject?                  operatorOf  = null;

                foreach (var entry in version.Checkpoints)
                {
                    if (!RoamingNetworkCheckpoint.TryParse(entry.Checkpoint, out var parsed, out error))
                        return Failed(error);
                    if (parsed.Id == commitProof.CheckpointId)
                    {
                        checkpoint  = parsed;
                        operatorOf  = entry.Operator;
                    }
                }

                if (checkpoint is null || operatorOf is null)
                    return Failed("The checkpoint of the historical commit proof is missing!");

                if (checkpoint.RoamingNetworkId != Current.RoamingNetworkId || checkpoint.CreatedAt > Current.EvaluatedAt)
                    return Failed("Foreign or future historical checkpoint!");

                var coordinate = $"{checkpoint.Epoch}:{checkpoint.Sequence}";

                if (heads.TryGetValue(coordinate, out var head) && head != checkpoint.Id)
                    return Failed("Conflicting historical checkpoint heads!");

                heads[coordinate] = checkpoint.Id;

                if (!AuthorizedCheckpoint.TryVerify(Current, checkpoint, operatorOf, policy, out var authorized, out error) ||
                    !commitProof.Verify(checkpoint, out error)                                                            ||
                    !TryVerifyAgainstCommitProof(proof, commitProof, out error))
                {
                    return Failed(error);
                }

                publication.Add(new HistoricalVersionPublication(proof.CommitId,
                                                                 authorized.WitnessedBy.IsEmpty ? "rno-checkpoint" : "witnessed-checkpoint",
                                                                 checkpoint.Id,
                                                                 authorized.WitnessedBy));

            }

            return (new HistoricalMeterVerification([.. bindings], [.. publication], Current.EvaluatedAt), null);

        }

        #endregion


        #region (internal static) TryVerifyAgainstCommitProof(Proof, CommitProof, out ErrorResponse)

        /// <summary>
        /// Whether the given proof is one of the version the given proof of a commit proves (WWCP_POI.TS
        /// verifyAgainstCommitProof): of its commit and revision, leading to the effectiveTree ETag the commit names.
        /// </summary>
        internal static Boolean TryVerifyAgainstCommitProof(RoamingNetworkProof               Proof,
                                                            RoamingNetworkCheckpointProof     CommitProof,
                                                            [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (!Proof.TryCalculateETag(out var eTag, out ErrorResponse))
                return false;

            if (Proof.CommitId != CommitProof.CommitId || Proof.Revision != CommitProof.Revision)
            {
                ErrorResponse = $"The proof of the commit is one of '{CommitProof.CommitId}', not of '{Proof.CommitId}'!";
                return false;
            }

            var leaf = CommitProof.ETags.FirstOrDefault(tag => tag.Format == ETagFormat.EffectiveTree && tag.HashAlgorithm == Proof.HashAlgorithm);

            if (!leaf.IsValid || !leaf.Equals(eTag))
            {
                ErrorResponse = leaf.IsValid
                                    ? $"The proof leads to {eTag}, not to {leaf}!"
                                    : $"The commit names no effectiveTree ETag of {Proof.HashAlgorithm.ToText()}, the hash algorithm of the proof!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (internal static) TryParseCheckpoints(JSON, What, out Checkpoints, out ErrorResponse)

        internal static Boolean TryParseCheckpoints(JToken?                                     JSON,
                                                    String                                      What,
                                                    out ImmutableArray<HistoricalCheckpointEntry>  Checkpoints,
                                                    [NotNullWhen(false)] out String?            ErrorResponse)
        {

            Checkpoints    = [];
            ErrorResponse  = null;

            if (JSON is null)
                return true;

            if (JSON is not JArray array)
            {
                ErrorResponse = $"{What} must be a JSON array!";
                return false;
            }

            var entries = new List<HistoricalCheckpointEntry>();

            foreach (var item in array)
            {

                if (item is not JObject entry)
                {
                    ErrorResponse = "A checkpoint entry must be a JSON object!";
                    return false;
                }

                if (!Only(entry, "A checkpoint entry", out ErrorResponse, "checkpoint", "operator"))
                    return false;

                if (entry["checkpoint"] is not JObject checkpoint || entry["operator"] is not JObject @operator)
                {
                    ErrorResponse = "A checkpoint entry gives the checkpoint and the operator that signed it!";
                    return false;
                }

                entries.Add(new HistoricalCheckpointEntry(checkpoint, @operator));

            }

            Checkpoints = [.. entries];
            return true;

        }

        #endregion

        #region (internal static) Only(JSON, What, out ErrorResponse, params Properties)

        /// <summary>
        /// Whether the given JSON object names only the given properties.
        /// </summary>
        internal static Boolean Only(JObject                           JSON,
                                     String                            What,
                                     [NotNullWhen(false)] out String?  ErrorResponse,
                                     params String[]                   Properties)
        {

            if (JSON.Properties().FirstOrDefault(property => !Properties.Contains(property.Name)) is JProperty unknown)
            {
                ErrorResponse = $"{What} must not name the property '{unknown.Name}'!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private static) JSONOf(Version)

        private static JObject JSONOf(HistoricalStationVersion Version)
        {

            var json = new JObject(new JProperty("chargingStationProof", Version.ChargingStationProof));

            if (Version.CommitProof is not null)
                json.Add("commitProof", Version.CommitProof);

            if (!Version.Checkpoints.IsEmpty)
                json.Add("checkpoints", new JArray(Version.Checkpoints.Select(entry => new JObject(new JProperty("checkpoint", entry.Checkpoint),
                                                                                                  new JProperty("operator",   entry.Operator)))));

            return json;

        }

        #endregion

    }

}
