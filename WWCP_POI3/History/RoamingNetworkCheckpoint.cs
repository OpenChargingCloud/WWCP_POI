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
    /// Its identification is the digest of its canonical JSON content without signatures, with the identification hash of
    /// its epoch (SPECIFICATION.md, section 11), as are its trees. The final checkpoint of an epoch names the next one, the
    /// first checkpoint of an epoch after the first one the final checkpoint of the one before.
    /// </summary>
    public sealed class RoamingNetworkCheckpoint
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the signature input of a checkpoint signature, which names what it signs and in which
        /// version: a checkpoint signature is written as a commit signature is, its context separates the two.
        /// </summary>
        public const String  SignatureContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/checkpointSignature/v1";

        /// <summary>
        /// The epoch every history begins in (SPECIFICATION.md, section 11).
        /// </summary>
        public const UInt32  DefaultEpoch     = 1;

        /// <summary>
        /// The JSON-LD context of the object, which names the version of the content a checkpoint identification is
        /// calculated over, and is part of it also where it is embedded.
        /// </summary>
        public const String  JSONLDContext    = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/checkpoint/v1";

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
            /// The property of its epoch.
            /// </summary>
            public const String  Epoch             = "epoch";

            /// <summary>
            /// The property of its position in the line of its epoch.
            /// </summary>
            public const String  Sequence          = "sequence";

            /// <summary>
            /// The property of the identification of the checkpoint before.
            /// </summary>
            public const String  PreviousId        = "previousId";

            /// <summary>
            /// The property of the identification of its commit, as of the leaf of a commit as well.
            /// </summary>
            public const String  CommitId          = "commitId";

            /// <summary>
            /// The property of the revision of its commit, as of the leaf of a commit as well.
            /// </summary>
            public const String  Revision          = "revision";

            /// <summary>
            /// The property of the ETags of the version of its commit, as of the leaf of a commit as well.
            /// </summary>
            public const String  ETags             = "eTags";

            /// <summary>
            /// The property of the tree of its commits: { @merkleRoot, @size }.
            /// </summary>
            public const String  Commits           = "commits";

            /// <summary>
            /// The property of the tree of the price statements it names: { @merkleRoot, @size }.
            /// </summary>
            public const String  PriceStatements   = "priceStatements";

            /// <summary>
            /// The property of the Merkle tree hash of a tree.
            /// </summary>
            public const String  MerkleRoot        = "@merkleRoot";

            /// <summary>
            /// The property of the number of leaves of a tree.
            /// </summary>
            public const String  Size              = "@size";

            /// <summary>
            /// The property of its creation timestamp.
            /// </summary>
            public const String  CreatedAt         = "createdAt";

            /// <summary>
            /// The property telling that it is the final checkpoint of its epoch.
            /// </summary>
            public const String  Final             = "final";

            /// <summary>
            /// The property of the next epoch, named by a final checkpoint.
            /// </summary>
            public const String  Next              = "next";

            /// <summary>
            /// The property of the final checkpoint of the epoch before.
            /// </summary>
            public const String  PreviousEpoch     = RoamingNetworkEpochLink.JSONProperty;

            /// <summary>
            /// The property of its signatures.
            /// </summary>
            public const String  Signatures        = "signatures";

            /// <summary>
            /// The property of the checkpoint in the signature input of a checkpoint signature.
            /// </summary>
            public const String  Checkpoint        = "checkpoint";

        }

        #endregion


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
        /// The Merkle tree hash of the commits of this checkpoint, in lowercase hexadecimal digits.
        /// </summary>
        public String                                         CommitsRoot         { get; }

        /// <summary>
        /// The number of commits of this checkpoint.
        /// </summary>
        public UInt64                                         CommitsSize         { get; }

        /// <summary>
        /// The Merkle tree hash of the price statements this checkpoint names, those its server took since the checkpoint
        /// before (SPECIFICATION.md, section 12), in lowercase hexadecimal digits; null when it names none.
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

        /// <summary>
        /// The hash of the identification and of the trees of this checkpoint, the identification hash of its epoch.
        /// </summary>
        public ETagHashAlgorithm                              IdentificationHash  { get; }

        /// <summary>
        /// The next epoch, when this checkpoint is the final one of its epoch: the epoch takes no commit after it.
        /// </summary>
        public RoamingNetworkEpoch?                           Next                { get; }

        /// <summary>
        /// Whether this checkpoint is the final one of its epoch.
        /// </summary>
        public Boolean                                        IsFinal
            => Next is not null;

        /// <summary>
        /// The final checkpoint of the epoch before, when this is the first checkpoint of an epoch after the first one.
        /// </summary>
        public RoamingNetworkEpochLink?                       PreviousEpoch       { get; }

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
                                         ETagHashAlgorithm                              IdentificationHash,
                                         RoamingNetworkEpoch?                           Next,
                                         RoamingNetworkEpochLink?                       PreviousEpoch,
                                         ImmutableArray<RoamingNetworkCommitSignature>  Signatures   = default)
        {

            if (Epoch == 0)
                throw new ArgumentException("The first epoch is 1.", nameof(Epoch));

            if ((Sequence == 0) != (PreviousId is null))
                throw new ArgumentException("A checkpoint names the one before it, unless it is the first one of its epoch.", nameof(PreviousId));

            if (ETags.IsDefaultOrEmpty || ETags.Any(eTag => !eTag.IsValid))
                throw new ArgumentException("A checkpoint requires valid ETags of the version of its commit.", nameof(ETags));

            var digestLength = 2 * ETag.DigestLength(IdentificationHash);

            if (CommitsRoot is null || CommitsRoot.Length != digestLength || CommitsRoot.AsSpan().IndexOfAnyExcept("0123456789abcdef") >= 0)
                throw new ArgumentException($"The root of the commits must be {digestLength} lowercase hexadecimal digits, a digest of the identification hash.", nameof(CommitsRoot));

            if (CommitsSize == 0)
                throw new ArgumentException("A checkpoint has at least its own commit.", nameof(CommitsSize));

            if ((PriceStatementsRoot is null) != (PriceStatementsSize == 0))
                throw new ArgumentException("A checkpoint names price statements by their root and their number, or none.", nameof(PriceStatementsRoot));

            if (PriceStatementsRoot is not null && (PriceStatementsRoot.Length != digestLength || PriceStatementsRoot.AsSpan().IndexOfAnyExcept("0123456789abcdef") >= 0))
                throw new ArgumentException($"The root of the price statements must be {digestLength} lowercase hexadecimal digits, a digest of the identification hash.", nameof(PriceStatementsRoot));

            if (Epoch == 1 && IdentificationHash != ETagHashAlgorithm.SHA256)
                throw new ArgumentException("The checkpoints of the first epoch are identified with SHA-256.", nameof(IdentificationHash));

            if (CommitId.HashAlgorithm != IdentificationHash || (PreviousId.HasValue && PreviousId.Value.HashAlgorithm != IdentificationHash))
                throw new ArgumentException("A checkpoint, its commit and the checkpoint before are identified with the same hash: the one of their epoch.", nameof(IdentificationHash));

            if (Next is not null && Next.Number != Epoch + 1)
                throw new ArgumentException("The final checkpoint of an epoch names the next one.", nameof(Next));

            if ((PreviousEpoch is not null) != (Sequence == 0 && Epoch > 1))
                throw new ArgumentException("The first checkpoint of an epoch after the first one names the final checkpoint of the one before, no other checkpoint does.", nameof(PreviousEpoch));

            if (PreviousEpoch is not null && PreviousEpoch.Epoch != Epoch - 1)
                throw new ArgumentException("The first checkpoint of an epoch names the final checkpoint of the epoch just before.", nameof(PreviousEpoch));

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
            this.IdentificationHash   = IdentificationHash;
            this.Next                 = Next;
            this.PreviousEpoch        = PreviousEpoch;
            this.CreatedAt            = DateTimeOffset.FromUnixTimeMilliseconds(CreatedAt.ToUnixTimeMilliseconds());
            this.Signatures           = Signatures.IsDefault ? [] : Signatures;
            this.Id                   = CalcId();

        }

        #endregion


        #region (static) Create  (RoamingNetworkId, Epoch, Sequence, PreviousId, Commit, Commits, CreatedAt, PriceStatements = null, Next = null, PreviousEpoch = null)

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
        /// <param name="Next">The next epoch, when it is the final checkpoint of its epoch.</param>
        /// <param name="PreviousEpoch">The final checkpoint of the epoch before, when it is the first checkpoint of an epoch after the first one.</param>
        public static RoamingNetworkCheckpoint Create(RoamingNetwork_Id                           RoamingNetworkId,
                                                      UInt32                                      Epoch,
                                                      UInt64                                      Sequence,
                                                      RoamingNetworkCheckpoint_Id?                PreviousId,
                                                      RoamingNetworkCommit                        Commit,
                                                      IEnumerable<RoamingNetworkCommit>           Commits,
                                                      DateTimeOffset                              CreatedAt,
                                                      IEnumerable<RoamingNetworkPriceStatement>?  PriceStatements   = null,
                                                      RoamingNetworkEpoch?                        Next              = null,
                                                      RoamingNetworkEpochLink?                    PreviousEpoch     = null)
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
                        CommitsRootOf(ordered, Commit.IdentificationHash),
                        (UInt64) ordered.Count,
                        CreatedAt,
                        priceStatements.Count > 0 ? RoamingNetworkPriceStatement.RootOf(priceStatements, Commit.IdentificationHash) : null,
                        (UInt64) priceStatements.Count,
                        Commit.IdentificationHash,
                        Next,
                        PreviousEpoch);

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
                   new JProperty(PropertyKeys.CommitId,  CommitId.ToString()),
                   new JProperty(PropertyKeys.ETags,     new JArray(ETags.Select(eTag => eTag.ToJSON()))),
                   new JProperty(PropertyKeys.Revision,  Revision)
               );

        #endregion

        #region (static) CommitsRootOf(OrderedCommits, IdentificationHash = SHA256)

        /// <summary>
        /// The Merkle tree hash of the given commits, already in the order of a checkpoint, with the given hash.
        /// </summary>
        public static String CommitsRootOf(IReadOnlyList<RoamingNetworkCommit>  OrderedCommits,
                                           ETagHashAlgorithm                    IdentificationHash   = ETagHashAlgorithm.SHA256)

            => Convert.ToHexStringLower(POIMerkleTree.RootOfLeafHashes(IdentificationHash,
                                                                       [ .. OrderedCommits.Select(commit => POIMerkleTree.LeafHash(IdentificationHash, Leaf(commit))) ]));

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
                                                      IdentificationHash,
                                                      Next,
                                                      PreviousEpoch,
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
            var unsigned  = new RoamingNetworkCommitSignature(Role, Signer, SignedAt ?? Timestamp.Now, Algorithm.Name, KeyId, "-");
            var value     = Algorithm.Sign(GetSigningBytes(unsigned), PrivateKey, Deterministic: Deterministic);

            return new RoamingNetworkCommitSignature(unsigned.Role,
                                                     unsigned.Signer,
                                                     unsigned.SignedAt,
                                                     unsigned.Algorithm,
                                                     unsigned.KeyId,
                                                     Convert.ToBase64String(value));

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

        #region VerifySignature(Signature, PublicKey (COSE), ExpectedKeyId, out ErrorResponse)

        /// <summary>
        /// Verify the given signature of this checkpoint with a trusted Styx COSE public key, honoring its algorithm restriction,
        /// e.g. the key of a POIPublicKey.
        /// </summary>
        /// <param name="Signature">A signature of this checkpoint.</param>
        /// <param name="PublicKey">The trusted public COSE key.</param>
        /// <param name="ExpectedKeyId">The key identification of the trusted public key.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean VerifySignature(RoamingNetworkCommitSignature     Signature,
                                       COSEKey                           PublicKey,
                                       String                            ExpectedKeyId,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ErrorResponse = null;

            try
            {

                ArgumentNullException.ThrowIfNull(Signature);
                ArgumentNullException.ThrowIfNull(PublicKey);

                if (PublicKey.IsPrivate)
                    throw new ArgumentException("Supply the public COSE key for verification.", nameof(PublicKey));

                if (PublicKey.Algorithm is { } restricted && Signature.Algorithm != restricted.Name)
                    throw new ArgumentException("The signature algorithm differs from the key's algorithm restriction.");

                return VerifySignature(Signature, PublicKey.ToPublicKey(), ExpectedKeyId, out ErrorResponse);

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
        /// The signature input of the given signature of this checkpoint, as Styx canonical JSON: the JSON-LD context of a
        /// checkpoint signature, the signed fields of the signature and the JSON representation of this checkpoint with its
        /// own context, without its signatures.
        /// </summary>
        /// <param name="Signature">A signature of this checkpoint; its value is not part of the input.</param>
        public Byte[] GetSigningBytes(RoamingNetworkCommitSignature Signature)
        {

            if (!COSEAlgorithm.TryParse(Signature.Algorithm, out var algorithm) || !algorithm.IsSupportedForSigning)
                throw new ArgumentException("The algorithm is not supported for asymmetric signing.", nameof(Signature));

            var json = new JObject(
                           new JProperty(RoamingNetworkCommitSignature.PropertyKeys.Context,     SignatureContext),
                           new JProperty(RoamingNetworkCommitSignature.PropertyKeys.Algorithm,   Signature.Algorithm),
                           new JProperty(RoamingNetworkCommitSignature.PropertyKeys.KeyId,       Signature.KeyId),
                           new JProperty(RoamingNetworkCommitSignature.PropertyKeys.Role,        Signature.Role),
                           new JProperty(RoamingNetworkCommitSignature.PropertyKeys.SignedAt,    Signature.SignedAt.ToISO8601())
                       );

            if (Signature.Signer is not null)
                json.Add(RoamingNetworkCommitSignature.PropertyKeys.Signer, Signature.Signer);

            json.Add(PropertyKeys.Checkpoint, ToJSON(Embedded: false, IncludeSignatures: false));

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

                if (POIJSON.Read(Text) is not JObject json)
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

                if (!RoamingNetworkCheckpoint_Id.TryParse(Text(JSON, PropertyKeys.Id), out var id))
                {
                    ErrorResponse = "Missing or invalid checkpoint identification!";
                    return false;
                }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the checkpoint '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!JSON.ParseMandatory(PropertyKeys.RoamingNetworkId,
                                         "roaming network identification",
                                         RoamingNetwork_Id.TryParse,
                                         out RoamingNetwork_Id roamingNetworkId,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!TryParseUInt(JSON, PropertyKeys.Epoch,     out var epoch,     out ErrorResponse) ||
                    !TryParseUInt(JSON, PropertyKeys.Sequence,  out var sequence,  out ErrorResponse) ||
                    !TryParseUInt(JSON, PropertyKeys.Revision,  out var revision,  out ErrorResponse))
                {
                    return false;
                }

                if (epoch == 0 || epoch > UInt32.MaxValue)
                {
                    ErrorResponse = "Invalid epoch!";
                    return false;
                }

                RoamingNetworkCheckpoint_Id? previousId = null;

                if (JSON[PropertyKeys.PreviousId] is JToken)
                {

                    if (!RoamingNetworkCheckpoint_Id.TryParse(Text(JSON, PropertyKeys.PreviousId), out var parsedPreviousId))
                    {
                        ErrorResponse = "Invalid identification of the checkpoint before!";
                        return false;
                    }

                    previousId = parsedPreviousId;

                }

                if (!RoamingNetworkCommit_Id.TryParse(Text(JSON, PropertyKeys.CommitId), out var commitId))
                {
                    ErrorResponse = "Missing or invalid commit identification!";
                    return false;
                }

                if (JSON[PropertyKeys.ETags] is not JArray eTagsJSON)
                {
                    ErrorResponse = "The ETags must be a JSON array!";
                    return false;
                }

                var eTags = ImmutableArray.CreateBuilder<ETag>(eTagsJSON.Count);

                foreach (var eTagJSON in eTagsJSON)
                    eTags.Add(ETag.ParseHex(eTagJSON));

                if (JSON[PropertyKeys.Commits] is not JObject commitsJSON ||
                    Text(commitsJSON, PropertyKeys.MerkleRoot) is not String commitsRoot ||
                    !TryParseUInt(commitsJSON, PropertyKeys.Size, out var commitsSize, out _))
                {
                    ErrorResponse = "The commits must be { @merkleRoot, @size }!";
                    return false;
                }

                String? priceStatementsRoot = null;
                UInt64  priceStatementsSize = 0;

                if (JSON[PropertyKeys.PriceStatements] is JToken priceStatementsJSON)
                {

                    if (priceStatementsJSON is not JObject priceStatementsObject ||
                        Text(priceStatementsObject, PropertyKeys.MerkleRoot) is not String root ||
                        !TryParseUInt(priceStatementsObject, PropertyKeys.Size, out priceStatementsSize, out _) ||
                        priceStatementsSize == 0)
                    {
                        ErrorResponse = "The price statements must be { @merkleRoot, @size }, at least one!";
                        return false;
                    }

                    priceStatementsRoot = root;

                }

                RoamingNetworkEpoch? next = null;

                if (JSON[PropertyKeys.Final] is JToken finalJSON)
                {

                    if (finalJSON.Type != JTokenType.Boolean || !finalJSON.Value<Boolean>())
                    {
                        ErrorResponse = $"A final checkpoint is \"{PropertyKeys.Final}\": true, with the next epoch as \"{PropertyKeys.Next}\"!";
                        return false;
                    }

                    if (!RoamingNetworkEpoch.TryParse(JSON[PropertyKeys.Next], out next, out var nextError))
                    {
                        ErrorResponse = "Invalid next epoch of the final checkpoint: " + nextError;
                        return false;
                    }

                }

                else if (JSON[PropertyKeys.Next] is not null)
                {
                    ErrorResponse = "Only a final checkpoint names the next epoch!";
                    return false;
                }

                RoamingNetworkEpochLink? previousEpoch = null;

                if (JSON[PropertyKeys.PreviousEpoch] is JToken previousEpochJSON &&
                    !RoamingNetworkEpochLink.TryParse(previousEpochJSON, out previousEpoch, out var previousEpochError))
                {
                    ErrorResponse = previousEpochError;
                    return false;
                }

                if (Text(JSON, PropertyKeys.CreatedAt) is not String createdAtText ||
                    !DateTimeOffset.TryParse(createdAtText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt) ||
                    createdAt.ToISO8601() != createdAtText)
                {
                    ErrorResponse = "The creation timestamp must be written as yyyy-MM-ddTHH:mm:ss.fffZ!";
                    return false;
                }

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
                                                              id.HashAlgorithm,
                                                              next,
                                                              previousEpoch,
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

        #region ToJSON(Embedded = false, IncludeSignatures = true, CustomCheckpointSerializer = null)

        /// <summary>
        /// Return the JSON representation of this checkpoint, and, unless excluded, its signatures.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeSignatures">Whether to include the signatures.</param>
        /// <param name="CustomCheckpointSerializer">An optional custom serializer of checkpoints.</param>
        public JObject ToJSON(Boolean                                                     Embedded                     = false,
                              Boolean                                                     IncludeSignatures            = true,
                              CustomJObjectSerializerDelegate<RoamingNetworkCheckpoint>?  CustomCheckpointSerializer   = null)
        {

            var json = Content();

            if (Embedded)
                json.Remove(PropertyKeys.Context);

            json.AddFirst(new JProperty(PropertyKeys.Id, Id.ToString()));

            if (IncludeSignatures && !Signatures.IsEmpty)
                json.Add(PropertyKeys.Signatures, new JArray(Signatures.Select(signature => signature.ToJSON(Embedded: true))));

            return CustomCheckpointSerializer is not null
                       ? CustomCheckpointSerializer(this, json)
                       : json;

        }

        #endregion

        #region ToCBOR(Embedded = false, CustomCheckpointSerializer = null)

        /// <summary>
        /// Return the CBOR representation of this checkpoint: the keys and values of its JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomCheckpointSerializer">An optional custom serializer of checkpoints.</param>
        public CBORValue ToCBOR(Boolean                                                  Embedded                     = false,
                                CustomCBORSerializerDelegate<RoamingNetworkCheckpoint>?  CustomCheckpointSerializer   = null)
        {

            var cbor = new CBORMap {
                           { CBORValue.FromText(PropertyKeys.Id),                CBORValue.FromText(Id.ToString()) }
                       };

            if (!Embedded)
                cbor.Add(CBORValue.FromText(PropertyKeys.Context),           CBORValue.FromText(JSONLDContext));

            cbor.Add(CBORValue.FromText(PropertyKeys.RoamingNetworkId),      CBORValue.FromText(RoamingNetworkId.ToString()));
            cbor.Add(CBORValue.FromText(PropertyKeys.Epoch),                 CBORValue.FromUInt64(Epoch));
            cbor.Add(CBORValue.FromText(PropertyKeys.Sequence),              CBORValue.FromUInt64(Sequence));

            if (PreviousId.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.PreviousId),        CBORValue.FromText(PreviousId.Value.ToString()));

            cbor.Add(CBORValue.FromText(PropertyKeys.CommitId),              CBORValue.FromText(CommitId.ToString()));
            cbor.Add(CBORValue.FromText(PropertyKeys.Revision),              CBORValue.FromUInt64(Revision));

            // ETags as in the JSON representation: arrays of texts, their digests in hex.
            cbor.Add(CBORValue.FromText(PropertyKeys.ETags),                 CBORValue.FromArray(ETags.Select(eTag => CBORJSON.ToCBOR(eTag.ToJSON()))));

            cbor.Add(CBORValue.FromText(PropertyKeys.Commits),               new CBORMap {
                                                                                 { CBORValue.FromText(PropertyKeys.MerkleRoot),  CBORValue.FromText(CommitsRoot) },
                                                                                 { CBORValue.FromText(PropertyKeys.Size),        CBORValue.FromUInt64(CommitsSize) }
                                                                             });

            if (PriceStatementsRoot is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.PriceStatements),   new CBORMap {
                                                                                 { CBORValue.FromText(PropertyKeys.MerkleRoot),  CBORValue.FromText(PriceStatementsRoot) },
                                                                                 { CBORValue.FromText(PropertyKeys.Size),        CBORValue.FromUInt64(PriceStatementsSize) }
                                                                             });

            cbor.Add(CBORValue.FromText(PropertyKeys.CreatedAt),             CBORValue.FromText(CreatedAt.ToISO8601()));

            // The next epoch, the epoch before and the signatures have no CBOR representation of their own: theirs are the
            // keys and values of their JSON representation.
            if (Next is not null)
            {
                cbor.Add(CBORValue.FromText(PropertyKeys.Final),             CBORValue.True);
                cbor.Add(CBORValue.FromText(PropertyKeys.Next),              CBORJSON.ToCBOR(Next.ToJSON(Embedded: true)));
            }

            if (PreviousEpoch is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.PreviousEpoch),     CBORJSON.ToCBOR(PreviousEpoch.ToJSON(Embedded: true)));

            if (!Signatures.IsEmpty)
                cbor.Add(CBORValue.FromText(PropertyKeys.Signatures),        CBORValue.FromArray(Signatures.Select(signature => CBORJSON.ToCBOR(signature.ToJSON(Embedded: true)))));

            return CustomCheckpointSerializer is not null
                       ? CustomCheckpointSerializer(this, cbor)
                       : cbor;

        }

        #endregion


        #region (private) Content()

        /// <summary>
        /// The content of this checkpoint, what its identification is calculated over.
        /// </summary>
        private JObject Content()
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Context,           JSONLDContext),
                           new JProperty(PropertyKeys.RoamingNetworkId,  RoamingNetworkId.ToString()),
                           new JProperty(PropertyKeys.Epoch,             Epoch),
                           new JProperty(PropertyKeys.Sequence,          Sequence)
                       );

            if (PreviousId.HasValue)
                json.Add(PropertyKeys.PreviousId,  PreviousId.Value.ToString());

            json.Add(PropertyKeys.CommitId,        CommitId.ToString());
            json.Add(PropertyKeys.Revision,        Revision);
            json.Add(PropertyKeys.ETags,           new JArray(ETags.Select(eTag => eTag.ToJSON())));
            json.Add(PropertyKeys.Commits,         new JObject(
                                                       new JProperty(PropertyKeys.MerkleRoot,  CommitsRoot),
                                                       new JProperty(PropertyKeys.Size,        CommitsSize)
                                                   ));

            if (PriceStatementsRoot is not null)
                json.Add(PropertyKeys.PriceStatements, new JObject(
                                                           new JProperty(PropertyKeys.MerkleRoot,  PriceStatementsRoot),
                                                           new JProperty(PropertyKeys.Size,        PriceStatementsSize)
                                                       ));

            json.Add(PropertyKeys.CreatedAt,       CreatedAt.ToISO8601());

            if (Next is not null)
            {
                json.Add(PropertyKeys.Final,       true);
                json.Add(PropertyKeys.Next,        Next.ToJSON(Embedded: true));
            }

            if (PreviousEpoch is not null)
                json.Add(PropertyKeys.PreviousEpoch, PreviousEpoch.ToJSON(Embedded: true));

            return json;

        }

        #endregion

        #region (private) CalcId()

        private RoamingNetworkCheckpoint_Id CalcId()

            => RoamingNetworkCheckpoint_Id.FromDigest(
                   POIMerkleTree.Hash(
                       IdentificationHash,
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

            => $"{(IsFinal ? "Final checkpoint" : "Checkpoint")} {Sequence} of epoch {Epoch} ({Id}) of commit {CommitId}, {CommitsSize} commits, {PriceStatementsSize} price statements, {Signatures.Length} signatures";

        #endregion

    }

}
