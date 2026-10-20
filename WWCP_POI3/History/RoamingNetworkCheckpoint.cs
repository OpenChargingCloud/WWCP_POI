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
using System.Globalization;
using System.Security.Cryptography;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A delegate verifying one signature of a checkpoint with a trusted key,
    /// e.g. by resolving the key of its key identification and calling VerifySignature.
    /// </summary>
    /// <param name="Checkpoint">The signed checkpoint.</param>
    /// <param name="Signature">One of its signatures.</param>
    public delegate Boolean VerifyCheckpointSignatureDelegate(RoamingNetworkCheckpoint       Checkpoint,
                                                              RoamingNetworkCommitSignature  Signature);


    /// <summary>
    /// A signed statement that a commit, and every commit it follows, is part of the history of a roaming network
    /// (SPECIFICATION.md, section 10). Checkpoints form one line per roaming network and epoch, each naming the one
    /// before it, while the commits form a graph. Witnesses sign them after they checked them against their own history,
    /// so that a proof of a commit (RoamingNetworkCheckpointProof) stays evidence for months.
    /// Its identification is the SHA-256 digest of its canonical JSON content without signatures.
    /// </summary>
    public sealed class RoamingNetworkCheckpoint
    {

        #region Data

        /// <summary>
        /// The versioned profile of the content a checkpoint identification is calculated over.
        /// </summary>
        public const String  IdentityProfile  = "wwcp-poi-checkpoint-v1";

        /// <summary>
        /// The versioned profile of the signature input of a checkpoint signature.
        /// </summary>
        public const String  SigningProfile   = "wwcp-poi-checkpoint-signature-v1";

        /// <summary>
        /// The epoch of every history, until epochs are implemented (SPECIFICATION.md, section 11).
        /// </summary>
        public const UInt32  DefaultEpoch     = 1;

        #endregion

        #region Properties

        /// <summary>
        /// The identification of this checkpoint, calculated from its content.
        /// </summary>
        public RoamingNetworkCheckpoint_Id                    Id                  { get; }

        /// <summary>
        /// The roaming network of this checkpoint.
        /// </summary>
        public RoamingNetwork_Id                              RoamingNetworkId    { get; }

        /// <summary>
        /// The epoch of this checkpoint.
        /// </summary>
        public UInt32                                         Epoch               { get; }

        /// <summary>
        /// The position of this checkpoint in the line of its epoch: 0 for the first one.
        /// </summary>
        public UInt64                                         Sequence            { get; }

        /// <summary>
        /// The identification of the checkpoint before, absent for sequence 0.
        /// </summary>
        public RoamingNetworkCheckpoint_Id?                   PreviousId          { get; }

        /// <summary>
        /// The commit of this checkpoint.
        /// </summary>
        public RoamingNetworkCommit_Id                        CommitId            { get; }

        /// <summary>
        /// The revision of the commit.
        /// </summary>
        public UInt64                                         Revision            { get; }

        /// <summary>
        /// The ETags of the version of the commit.
        /// </summary>
        public ImmutableArray<ETag>                           ETags               { get; }

        /// <summary>
        /// The Merkle tree hash of the commits of this checkpoint, as 64 lowercase hexadecimal digits.
        /// </summary>
        public String                                         CommitsRoot         { get; }

        /// <summary>
        /// The number of commits of this checkpoint.
        /// </summary>
        public UInt64                                         CommitsSize         { get; }

        /// <summary>
        /// The Merkle tree hash of the price statements this checkpoint names, those its server took since the checkpoint
        /// before (SPECIFICATION.md, section 12), as 64 lowercase hexadecimal digits; null when it names none.
        /// </summary>
        public String?                                        PriceStatementsRoot { get; }

        /// <summary>
        /// The number of price statements this checkpoint names: 0 when it names none.
        /// </summary>
        public UInt64                                         PriceStatementsSize { get; }

        /// <summary>
        /// When this checkpoint was made, to the millisecond.
        /// </summary>
        public DateTimeOffset                                 CreatedAt           { get; }

        /// <summary>
        /// The signatures of this checkpoint, e.g. of the roaming network operator and of witnesses.
        /// They are not part of the content the identification is calculated over.
        /// </summary>
        public ImmutableArray<RoamingNetworkCommitSignature>  Signatures          { get; }

        #endregion

        #region Constructor(s)

        private RoamingNetworkCheckpoint(RoamingNetwork_Id                              RoamingNetworkId,
                                         UInt32                                         Epoch,
                                         UInt64                                         Sequence,
                                         RoamingNetworkCheckpoint_Id?                   PreviousId,
                                         RoamingNetworkCommit_Id                        CommitId,
                                         UInt64                                         Revision,
                                         ImmutableArray<ETag>                           ETags,
                                         String                                         CommitsRoot,
                                         UInt64                                         CommitsSize,
                                         DateTimeOffset                                 CreatedAt,
                                         String?                                        PriceStatementsRoot,
                                         UInt64                                         PriceStatementsSize,
                                         ImmutableArray<RoamingNetworkCommitSignature>  Signatures   = default)
        {

            if (Epoch == 0)
                throw new ArgumentException("The first epoch is 1.", nameof(Epoch));

            if ((Sequence == 0) != (PreviousId is null))
                throw new ArgumentException("A checkpoint names the one before it, unless it is the first one of its epoch.", nameof(PreviousId));

            if (ETags.IsDefaultOrEmpty || ETags.Any(eTag => !eTag.IsValid))
                throw new ArgumentException("A checkpoint requires valid ETags of the version of its commit.", nameof(ETags));

            if (CommitsRoot is null || CommitsRoot.Length != 64 || CommitsRoot.AsSpan().IndexOfAnyExcept("0123456789abcdef") >= 0)
                throw new ArgumentException("The root of the commits must be 64 lowercase hexadecimal digits.", nameof(CommitsRoot));

            if (CommitsSize == 0)
                throw new ArgumentException("A checkpoint has at least its own commit.", nameof(CommitsSize));

            if ((PriceStatementsRoot is null) != (PriceStatementsSize == 0))
                throw new ArgumentException("A checkpoint names price statements by their root and their number, or none.", nameof(PriceStatementsRoot));

            if (PriceStatementsRoot is not null && (PriceStatementsRoot.Length != 64 || PriceStatementsRoot.AsSpan().IndexOfAnyExcept("0123456789abcdef") >= 0))
                throw new ArgumentException("The root of the price statements must be 64 lowercase hexadecimal digits.", nameof(PriceStatementsRoot));

            this.RoamingNetworkId     = RoamingNetworkId;
            this.Epoch                = Epoch;
            this.Sequence             = Sequence;
            this.PreviousId           = PreviousId;
            this.CommitId             = CommitId;
            this.Revision             = Revision;
            this.ETags                = ETags;
            this.CommitsRoot          = CommitsRoot;
            this.CommitsSize          = CommitsSize;
            this.PriceStatementsRoot  = PriceStatementsRoot;
            this.PriceStatementsSize  = PriceStatementsSize;
            this.CreatedAt            = DateTimeOffset.FromUnixTimeMilliseconds(CreatedAt.ToUnixTimeMilliseconds());
            this.Signatures           = Signatures.IsDefault ? [] : Signatures;
            this.Id                   = CalcId();

        }

        #endregion


        #region (static) Create  (RoamingNetworkId, Epoch, Sequence, PreviousId, Commit, Commits, CreatedAt, PriceStatements = null)

        /// <summary>
        /// Make a checkpoint of the given commit, unsigned.
        /// </summary>
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="Epoch">The epoch.</param>
        /// <param name="Sequence">The position in the line of the epoch.</param>
        /// <param name="PreviousId">The checkpoint before, absent for sequence 0.</param>
        /// <param name="Commit">The commit.</param>
        /// <param name="Commits">The commits of the checkpoint, the commit among them, in any order.</param>
        /// <param name="CreatedAt">When it is made.</param>
        /// <param name="PriceStatements">The price statements it names, in any order: those taken since the checkpoint before.</param>
        public static RoamingNetworkCheckpoint Create(RoamingNetwork_Id                           RoamingNetworkId,
                                                      UInt32                                      Epoch,
                                                      UInt64                                      Sequence,
                                                      RoamingNetworkCheckpoint_Id?                PreviousId,
                                                      RoamingNetworkCommit                        Commit,
                                                      IEnumerable<RoamingNetworkCommit>           Commits,
                                                      DateTimeOffset                              CreatedAt,
                                                      IEnumerable<RoamingNetworkPriceStatement>?  PriceStatements   = null)
        {

            var ordered = InOrder(Commits);

            if (!ordered.Any(commit => commit.Id == Commit.Id))
                throw new ArgumentException("The commits of a checkpoint include its own commit.", nameof(Commits));

            var priceStatements = RoamingNetworkPriceStatement.InOrder(PriceStatements ?? []);

            return new (RoamingNetworkId,
                        Epoch,
                        Sequence,
                        PreviousId,
                        Commit.Id,
                        Commit.Revision,
                        Commit.ETags,
                        CommitsRootOf(ordered),
                        (UInt64) ordered.Count,
                        CreatedAt,
                        priceStatements.Count > 0 ? RoamingNetworkPriceStatement.RootOf(priceStatements) : null,
                        (UInt64) priceStatements.Count);

        }

        #endregion

        #region (static) InOrder (Commits)

        /// <summary>
        /// The given commits in the order of the leaves of a checkpoint: ascending revisions,
        /// among equal revisions the ordinal order of their identifications.
        /// </summary>
        /// <param name="Commits">Commits.</param>
        public static IReadOnlyList<RoamingNetworkCommit> InOrder(IEnumerable<RoamingNetworkCommit> Commits)

            => [.. Commits.DistinctBy(commit => commit.Id).
                           OrderBy (commit => commit.Revision).
                           ThenBy  (commit => commit.Id.ToString(), StringComparer.Ordinal)];

        #endregion

        #region (static) Leaf    (Commit)

        /// <summary>
        /// The leaf of the given commit in the tree of a checkpoint: the UTF-8 encoded canonical JSON
        /// of { commitId, eTags, revision }, so that a proof of the commit binds its ETags as well.
        /// </summary>
        /// <param name="Commit">A commit.</param>
        public static Byte[] Leaf(RoamingNetworkCommit Commit)

            => Leaf(Commit.Id, Commit.ETags, Commit.Revision);


        /// <summary>
        /// The leaf of a commit in the tree of a checkpoint.
        /// </summary>
        /// <param name="CommitId">The identification of the commit.</param>
        /// <param name="ETags">The ETags of its version.</param>
        /// <param name="Revision">Its revision.</param>
        public static Byte[] Leaf(RoamingNetworkCommit_Id  CommitId,
                                  ImmutableArray<ETag>     ETags,
                                  UInt64                   Revision)

            => CanonicalJSON.ToUTF8Bytes(LeafJSON(CommitId, ETags, Revision));


        /// <summary>
        /// The JSON of the leaf of a commit: { commitId, eTags, revision }.
        /// </summary>
        public static JObject LeafJSON(RoamingNetworkCommit_Id  CommitId,
                                       ImmutableArray<ETag>     ETags,
                                       UInt64                   Revision)

            => new (
                   new JProperty("commitId",  CommitId.ToString()),
                   new JProperty("eTags",     new JArray(ETags.Select(eTag => eTag.ToJSON()))),
                   new JProperty("revision",  Revision)
               );

        #endregion

        #region (static) CommitsRootOf(OrderedCommits)

        /// <summary>
        /// The Merkle tree hash of the given commits, already in the order of a checkpoint.
        /// </summary>
        public static String CommitsRootOf(IReadOnlyList<RoamingNetworkCommit> OrderedCommits)

            => Convert.ToHexStringLower(POIMerkleTree.RootHash([.. OrderedCommits.Select(Leaf)]));

        #endregion


        #region WithSignatures(Signatures)

        /// <summary>
        /// Return this checkpoint with the given signatures added, unless it has them already.
        /// The identification stays the same.
        /// </summary>
        /// <param name="Signatures">Signatures of this checkpoint.</param>
        public RoamingNetworkCheckpoint WithSignatures(IEnumerable<RoamingNetworkCommitSignature> Signatures)
        {

            var newSignatures = Signatures.Where(signature => !this.Signatures.Contains(signature)).Distinct().ToArray();

            return newSignatures.Length == 0
                       ? this
                       : new RoamingNetworkCheckpoint(RoamingNetworkId,
                                                      Epoch,
                                                      Sequence,
                                                      PreviousId,
                                                      CommitId,
                                                      Revision,
                                                      ETags,
                                                      CommitsRoot,
                                                      CommitsSize,
                                                      CreatedAt,
                                                      PriceStatementsRoot,
                                                      PriceStatementsSize,
                                                      this.Signatures.AddRange(newSignatures));

        }

        #endregion

        #region CreateSignature(PrivateKey, KeyId, Algorithm, Role, Signer = null, SignedAt = null, Deterministic = true)

        /// <summary>
        /// Create a signature of this checkpoint, e.g. of a witness; the checkpoint stays unchanged.
        /// </summary>
        /// <param name="PrivateKey">The private key.</param>
        /// <param name="KeyId">The application identifier of the key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="Role">The role of the signer, e.g. roamingNetworkOperator or witness.</param>
        /// <param name="Signer">The optional name of the signer.</param>
        /// <param name="SignedAt">When the checkpoint is signed, now otherwise.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public RoamingNetworkCommitSignature CreateSignature(AsymmetricKeyParameter  PrivateKey,
                                                             String                  KeyId,
                                                             COSEAlgorithm           Algorithm,
                                                             String                  Role,
                                                             String?                 Signer          = null,
                                                             DateTimeOffset?         SignedAt        = null,
                                                             Boolean                 Deterministic   = true)
        {

            ArgumentNullException.ThrowIfNull(PrivateKey);

            if (!PrivateKey.IsPrivate)
                throw new ArgumentException("Signing requires a private asymmetric key.", nameof(PrivateKey));

            // Without a value yet, only to get the signed fields exactly as they will be written.
            var unsigned  = new RoamingNetworkCommitSignature(Role, Signer, SignedAt ?? Timestamp.Now, Algorithm.Name, KeyId, "-", SigningProfile);
            var value     = Algorithm.Sign(GetSigningBytes(unsigned), PrivateKey, Deterministic: Deterministic);

            return new RoamingNetworkCommitSignature(unsigned.Role,
                                                     unsigned.Signer,
                                                     unsigned.SignedAt,
                                                     unsigned.Algorithm,
                                                     unsigned.KeyId,
                                                     Convert.ToBase64String(value),
                                                     SigningProfile);

        }

        #endregion

        #region Sign           (PrivateKey, KeyId, Algorithm, Role, Signer = null, SignedAt = null, Deterministic = true)

        /// <summary>
        /// Return this checkpoint with a new signature added. The identification stays the same.
        /// </summary>
        public RoamingNetworkCheckpoint Sign(AsymmetricKeyParameter  PrivateKey,
                                             String                  KeyId,
                                             COSEAlgorithm           Algorithm,
                                             String                  Role,
                                             String?                 Signer          = null,
                                             DateTimeOffset?         SignedAt        = null,
                                             Boolean                 Deterministic   = true)

            => WithSignatures([ CreateSignature(PrivateKey, KeyId, Algorithm, Role, Signer, SignedAt, Deterministic) ]);

        #endregion

        #region VerifySignature(Signature, PublicKey, ExpectedKeyId, out ErrorResponse)

        /// <summary>
        /// Verify the given signature of this checkpoint with a trusted public key and its expected key identification.
        /// Unknown profiles, noncanonical Base64, mismatched keys and malformed inputs return false.
        /// </summary>
        /// <param name="Signature">A signature of this checkpoint.</param>
        /// <param name="PublicKey">The trusted public key.</param>
        /// <param name="ExpectedKeyId">The key identification of the trusted public key.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean VerifySignature(RoamingNetworkCommitSignature     Signature,
                                       AsymmetricKeyParameter            PublicKey,
                                       String                            ExpectedKeyId,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ErrorResponse = null;

            try
            {

                ArgumentNullException.ThrowIfNull(Signature);
                ArgumentNullException.ThrowIfNull(PublicKey);

                if (PublicKey.IsPrivate)
                    throw new ArgumentException("Verification requires a public asymmetric key.", nameof(PublicKey));

                if (Signature.Profile != SigningProfile || Signature.Encoding != "base64")
                    throw new ArgumentException("Unsupported checkpoint signature profile or encoding.");

                if (Signature.KeyId != ExpectedKeyId)
                    throw new ArgumentException("The signature key ID does not match the trusted key ID.");

                if (!COSEAlgorithm.TryParse(Signature.Algorithm, out var algorithm) || algorithm.Name != Signature.Algorithm)
                    throw new ArgumentException("Unknown or noncanonical signature algorithm.");

                var value = Convert.FromBase64String(Signature.Value);

                if (value.Length == 0 || Convert.ToBase64String(value) != Signature.Value)
                    throw new ArgumentException("Signature Value must contain nonempty canonical standard Base64.");

                return algorithm.Verify(GetSigningBytes(Signature), value, PublicKey, out ErrorResponse);

            }
            catch (Exception exception)
            {
                ErrorResponse = exception.Message;
                return false;
            }

        }

        #endregion

        #region GetSigningBytes(Signature)

        /// <summary>
        /// The signature input of the given signature of this checkpoint, as Styx canonical JSON:
        /// the signed fields of the signature and the JSON representation of this checkpoint without its signatures.
        /// </summary>
        /// <param name="Signature">A signature of this checkpoint; its value is not part of the input.</param>
        public Byte[] GetSigningBytes(RoamingNetworkCommitSignature Signature)
        {

            if (!COSEAlgorithm.TryParse(Signature.Algorithm, out var algorithm) || !algorithm.IsSupportedForSigning)
                throw new ArgumentException("The algorithm is not supported for asymmetric signing.", nameof(Signature));

            var json = new JObject(
                           new JProperty("profile",     SigningProfile),
                           new JProperty("algorithm",   Signature.Algorithm),
                           new JProperty("keyId",       Signature.KeyId),
                           new JProperty("encoding",    "base64"),
                           new JProperty("role",        Signature.Role),
                           new JProperty("signedAt",    Signature.SignedAt.ToISO8601())
                       );

            if (Signature.Signer is not null)
                json.Add("signer", Signature.Signer);

            json.Add("checkpoint", ToJSON(IncludeSignatures: false));

            return CanonicalJSON.ToUTF8Bytes(json);

        }

        #endregion


        #region (static) TryParse(Text, out Checkpoint, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON text of a checkpoint. Dates stay text, so signed values keep their exact representation.
        /// </summary>
        public static Boolean TryParse(String                                              Text,
                                       [NotNullWhen(true)]  out RoamingNetworkCheckpoint?  Checkpoint,
                                       [NotNullWhen(false)] out String?                    ErrorResponse)
        {

            Checkpoint = null;

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

                return TryParse(json, out Checkpoint, out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(JSON, out Checkpoint, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a checkpoint.
        /// Its identification must be the one calculated from its content.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Checkpoint">The parsed checkpoint.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkCheckpoint?  Checkpoint,
                                       [NotNullWhen(false)] out String?                    ErrorResponse)
        {

            Checkpoint = null;

            try
            {

                if (JSON is null)
                {
                    ErrorResponse = "The given JSON object must not be null!";
                    return false;
                }

                if (!RoamingNetworkCheckpoint_Id.TryParse(Text(JSON, "@id"), out var id))
                {
                    ErrorResponse = "Missing or invalid checkpoint identification!";
                    return false;
                }

                if (!JSON.ParseMandatory("roamingNetworkId",
                                         "roaming network identification",
                                         RoamingNetwork_Id.TryParse,
                                         out RoamingNetwork_Id roamingNetworkId,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!TryParseUInt(JSON, "epoch",     out var epoch,     out ErrorResponse) ||
                    !TryParseUInt(JSON, "sequence",  out var sequence,  out ErrorResponse) ||
                    !TryParseUInt(JSON, "revision",  out var revision,  out ErrorResponse))
                {
                    return false;
                }

                if (epoch == 0 || epoch > UInt32.MaxValue)
                {
                    ErrorResponse = "Invalid epoch!";
                    return false;
                }

                RoamingNetworkCheckpoint_Id? previousId = null;

                if (JSON["previousId"] is JToken)
                {

                    if (!RoamingNetworkCheckpoint_Id.TryParse(Text(JSON, "previousId"), out var parsedPreviousId))
                    {
                        ErrorResponse = "Invalid identification of the checkpoint before!";
                        return false;
                    }

                    previousId = parsedPreviousId;

                }

                if (!RoamingNetworkCommit_Id.TryParse(Text(JSON, "commitId"), out var commitId))
                {
                    ErrorResponse = "Missing or invalid commit identification!";
                    return false;
                }

                if (JSON["eTags"] is not JArray eTagsJSON)
                {
                    ErrorResponse = "The ETags must be a JSON array!";
                    return false;
                }

                var eTags = ImmutableArray.CreateBuilder<ETag>(eTagsJSON.Count);

                foreach (var eTagJSON in eTagsJSON)
                    eTags.Add(ETag.ParseHex(eTagJSON));

                if (JSON["commits"] is not JObject commitsJSON ||
                    Text(commitsJSON, "@merkleRoot") is not String commitsRoot ||
                    !TryParseUInt(commitsJSON, "@size", out var commitsSize, out _))
                {
                    ErrorResponse = "The commits must be { @merkleRoot, @size }!";
                    return false;
                }

                String? priceStatementsRoot = null;
                UInt64  priceStatementsSize = 0;

                if (JSON["priceStatements"] is JToken priceStatementsJSON)
                {

                    if (priceStatementsJSON is not JObject priceStatementsObject ||
                        Text(priceStatementsObject, "@merkleRoot") is not String root ||
                        !TryParseUInt(priceStatementsObject, "@size", out priceStatementsSize, out _) ||
                        priceStatementsSize == 0)
                    {
                        ErrorResponse = "The price statements must be { @merkleRoot, @size }, at least one!";
                        return false;
                    }

                    priceStatementsRoot = root;

                }

                if (Text(JSON, "createdAt") is not String createdAtText ||
                    !DateTimeOffset.TryParse(createdAtText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt) ||
                    createdAt.ToISO8601() != createdAtText)
                {
                    ErrorResponse = "The creation timestamp must be written as yyyy-MM-ddTHH:mm:ss.fffZ!";
                    return false;
                }

                var signatures = ImmutableArray.CreateBuilder<RoamingNetworkCommitSignature>();

                if (JSON["signatures"] is JToken signaturesJSON)
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

                var checkpoint = new RoamingNetworkCheckpoint(roamingNetworkId,
                                                              (UInt32) epoch,
                                                              sequence,
                                                              previousId,
                                                              commitId,
                                                              revision,
                                                              eTags.ToImmutable(),
                                                              commitsRoot,
                                                              commitsSize,
                                                              createdAt,
                                                              priceStatementsRoot,
                                                              priceStatementsSize,
                                                              [ .. signatures.Distinct() ]);

                if (checkpoint.Id != id)
                {
                    ErrorResponse = $"The checkpoint identification '{id}' is not the one of its content, '{checkpoint.Id}'!";
                    return false;
                }

                Checkpoint     = checkpoint;
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                Checkpoint     = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(CBOR, out Checkpoint, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of a checkpoint: the keys and values of its JSON representation.
        /// </summary>
        public static Boolean TryParse(CBORValue                                           CBOR,
                                       [NotNullWhen(true)]  out RoamingNetworkCheckpoint?  Checkpoint,
                                       [NotNullWhen(false)] out String?                    ErrorResponse)
        {

            Checkpoint = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map || CBORJSON.ToJSON(CBOR) is not JObject json)
                {
                    ErrorResponse = "The CBOR representation of a checkpoint must be a map with text keys!";
                    return false;
                }

                return TryParse(json, out Checkpoint, out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(IncludeSignatures = true)

        /// <summary>
        /// Return the JSON representation of this checkpoint, and, unless excluded, its signatures.
        /// </summary>
        /// <param name="IncludeSignatures">Whether to include the signatures.</param>
        public JObject ToJSON(Boolean IncludeSignatures = true)
        {

            var json = Content();

            json.Remove("profile");
            json.AddFirst(new JProperty("@id", Id.ToString()));

            if (IncludeSignatures && !Signatures.IsEmpty)
                json.Add("signatures", new JArray(Signatures.Select(signature => signature.ToJSON())));

            return json;

        }

        #endregion

        #region ToCBOR()

        /// <summary>
        /// Return the CBOR representation of this checkpoint: the keys and values of its JSON representation.
        /// </summary>
        public CBORValue ToCBOR()

            => CBORJSON.ToCBOR(ToJSON());

        #endregion


        #region (private) Content()

        /// <summary>
        /// The content of this checkpoint, what its identification is calculated over.
        /// </summary>
        private JObject Content()
        {

            var json = new JObject(
                           new JProperty("profile",           IdentityProfile),
                           new JProperty("roamingNetworkId",  RoamingNetworkId.ToString()),
                           new JProperty("epoch",             Epoch),
                           new JProperty("sequence",          Sequence)
                       );

            if (PreviousId.HasValue)
                json.Add("previousId",  PreviousId.Value.ToString());

            json.Add("commitId",        CommitId.ToString());
            json.Add("revision",        Revision);
            json.Add("eTags",           new JArray(ETags.Select(eTag => eTag.ToJSON())));
            json.Add("commits",         new JObject(
                                            new JProperty("@merkleRoot",  CommitsRoot),
                                            new JProperty("@size",        CommitsSize)
                                        ));

            if (PriceStatementsRoot is not null)
                json.Add("priceStatements", new JObject(
                                                new JProperty("@merkleRoot",  PriceStatementsRoot),
                                                new JProperty("@size",        PriceStatementsSize)
                                            ));

            json.Add("createdAt",       CreatedAt.ToISO8601());

            return json;

        }

        #endregion

        #region (private) CalcId()

        private RoamingNetworkCheckpoint_Id CalcId()

            => RoamingNetworkCheckpoint_Id.FromDigest(
                   SHA256.HashData(
                       CanonicalJSON.ToUTF8Bytes(
                           Content()
                       )
                   )
               );

        #endregion

        #region (private static) Text(JSON, PropertyName)

        private static String? Text(JObject JSON, String PropertyName)

            => JSON[PropertyName]?.Type == JTokenType.String
                   ? JSON[PropertyName]!.Value<String>()
                   : null;

        #endregion

        #region (private static) TryParseUInt(JSON, PropertyName, out Value, out ErrorResponse)

        private static Boolean TryParseUInt(JObject                           JSON,
                                            String                            PropertyName,
                                            out UInt64                        Value,
                                            [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value = 0;

            if (JSON[PropertyName] is not JValue { Type: JTokenType.Integer } value ||
                value.Value<Decimal>() < 0 ||
                value.Value<Decimal>() > UInt64.MaxValue)
            {
                ErrorResponse = $"Missing or invalid '{PropertyName}'!";
                return false;
            }

            Value          = value.Value<UInt64>();
            ErrorResponse  = null;
            return true;

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"Checkpoint {Sequence} of epoch {Epoch} ({Id}) of commit {CommitId}, {CommitsSize} commits, {PriceStatementsSize} price statements, {Signatures.Length} signatures";

        #endregion

    }

}
