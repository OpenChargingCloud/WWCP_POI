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

using System.Security.Cryptography;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The Merkle tree hash of a list of byte strings and the audit paths of its leaves,
    /// as RFC 9162, sections 2.1.1 and 2.1.3 define them, with SHA-256 unless another hash algorithm is given.
    /// </summary>
    public static class POIMerkleTree
    {

        #region Hash(HashAlgorithm, Data)

        /// <summary>
        /// The digest of the given data with the given hash algorithm.
        /// </summary>
        /// <param name="HashAlgorithm">The hash algorithm.</param>
        /// <param name="Data">The data.</param>
        public static Byte[] Hash(ETagHashAlgorithm   HashAlgorithm,
                                  ReadOnlySpan<Byte>  Data)

            => HashAlgorithm switch {
                   ETagHashAlgorithm.SHA256  => SHA256.HashData(Data),
                   ETagHashAlgorithm.SHA384  => SHA384.HashData(Data),
                   ETagHashAlgorithm.SHA512  => SHA512.HashData(Data),
                   _                         => throw new ArgumentOutOfRangeException(nameof(HashAlgorithm), "Unsupported hash algorithm!")
               };

        #endregion

        #region LeafHash(Data) / LeafHash(HashAlgorithm, Data)

        /// <summary>
        /// The hash of one leaf: SHA-256(0x00 || data).
        /// </summary>
        /// <param name="Data">The data of the leaf.</param>
        public static Byte[] LeafHash(ReadOnlySpan<Byte> Data)

            => LeafHash(ETagHashAlgorithm.SHA256, Data);

        /// <summary>
        /// The hash of one leaf: H(0x00 || data).
        /// </summary>
        /// <param name="HashAlgorithm">The hash algorithm.</param>
        /// <param name="Data">The data of the leaf.</param>
        public static Byte[] LeafHash(ETagHashAlgorithm   HashAlgorithm,
                                      ReadOnlySpan<Byte>  Data)
        {

            var input = new Byte[Data.Length + 1];
            input[0]  = 0x00;
            Data.CopyTo(input.AsSpan(1));

            return Hash(HashAlgorithm, input);

        }

        #endregion

        #region NodeHash(Left, Right) / NodeHash(HashAlgorithm, Left, Right)

        /// <summary>
        /// The hash of an inner node: SHA-256(0x01 || left || right).
        /// </summary>
        public static Byte[] NodeHash(ReadOnlySpan<Byte> Left,
                                      ReadOnlySpan<Byte> Right)

            => NodeHash(ETagHashAlgorithm.SHA256, Left, Right);

        /// <summary>
        /// The hash of an inner node: H(0x01 || left || right).
        /// </summary>
        public static Byte[] NodeHash(ETagHashAlgorithm   HashAlgorithm,
                                      ReadOnlySpan<Byte>  Left,
                                      ReadOnlySpan<Byte>  Right)
        {

            var input = new Byte[1 + Left.Length + Right.Length];
            input[0]  = 0x01;
            Left. CopyTo(input.AsSpan(1));
            Right.CopyTo(input.AsSpan(1 + Left.Length));

            return Hash(HashAlgorithm, input);

        }

        #endregion

        #region RootHash(Leaves)

        /// <summary>
        /// The Merkle tree hash of the given leaves, in their order: SHA-256("") for none.
        /// </summary>
        /// <param name="Leaves">The data of the leaves.</param>
        public static Byte[] RootHash(IReadOnlyList<Byte[]> Leaves)

            => RootOfLeafHashes(ETagHashAlgorithm.SHA256,
                                [ .. Leaves.Select(leaf => LeafHash(leaf)) ]);

        #endregion

        #region RootOfLeafHashes(HashAlgorithm, LeafHashes)

        /// <summary>
        /// The Merkle tree hash of the leaves of the given leaf hashes, in their order: H("") for none.
        /// </summary>
        /// <param name="HashAlgorithm">The hash algorithm.</param>
        /// <param name="LeafHashes">The hashes of the leaves, H(0x00 || data) each.</param>
        public static Byte[] RootOfLeafHashes(ETagHashAlgorithm      HashAlgorithm,
                                              IReadOnlyList<Byte[]>  LeafHashes)

            => LeafHashes.Count == 0
                   ? Hash(HashAlgorithm, [])
                   : Root(HashAlgorithm, LeafHashes, 0, LeafHashes.Count);

        #endregion

        #region AuditPath(Leaves, Index)

        /// <summary>
        /// The audit path of the leaf at the given index: the hashes needed to calculate the root from it,
        /// from the bottom up (RFC 9162, section 2.1.3.1).
        /// </summary>
        /// <param name="Leaves">The data of the leaves.</param>
        /// <param name="Index">The index of the leaf.</param>
        public static IReadOnlyList<Byte[]> AuditPath(IReadOnlyList<Byte[]>  Leaves,
                                                      Int32                  Index)

            => AuditPathOfLeafHashes(ETagHashAlgorithm.SHA256,
                                     [ .. Leaves.Select(leaf => LeafHash(leaf)) ],
                                     Index);

        #endregion

        #region AuditPathOfLeafHashes(HashAlgorithm, LeafHashes, Index)

        /// <summary>
        /// The audit path of the leaf at the given index of the leaves of the given leaf hashes, from the bottom up
        /// (RFC 9162, section 2.1.3.1).
        /// </summary>
        /// <param name="HashAlgorithm">The hash algorithm.</param>
        /// <param name="LeafHashes">The hashes of the leaves, H(0x00 || data) each.</param>
        /// <param name="Index">The index of the leaf.</param>
        public static IReadOnlyList<Byte[]> AuditPathOfLeafHashes(ETagHashAlgorithm      HashAlgorithm,
                                                                  IReadOnlyList<Byte[]>  LeafHashes,
                                                                  Int32                  Index)
        {

            if (Index < 0 || Index >= LeafHashes.Count)
                throw new ArgumentOutOfRangeException(nameof(Index), "The index must be one of a leaf!");

            var path = new List<Byte[]>();

            Path(HashAlgorithm, LeafHashes, Index, 0, LeafHashes.Count, path);

            return path;

        }

        #endregion

        #region TryCalculateRoot(LeafData, Index, Size, AuditPath, out Root)

        /// <summary>
        /// Calculate the root from one leaf, its index, the size of the tree and the audit path of the leaf
        /// (RFC 9162, section 2.1.3.2). False when the audit path does not fit the index and size.
        /// </summary>
        /// <param name="LeafData">The data of the leaf.</param>
        /// <param name="Index">The index of the leaf.</param>
        /// <param name="Size">The number of leaves.</param>
        /// <param name="AuditPath">The audit path of the leaf.</param>
        /// <param name="Root">The root, when it could be calculated.</param>
        public static Boolean TryCalculateRoot(ReadOnlySpan<Byte>     LeafData,
                                               UInt64                 Index,
                                               UInt64                 Size,
                                               IReadOnlyList<Byte[]>  AuditPath,
                                               out Byte[]             Root)

            => TryCalculateRootOfLeafHash(ETagHashAlgorithm.SHA256,
                                          LeafHash(LeafData),
                                          Index,
                                          Size,
                                          AuditPath,
                                          out Root);

        #endregion

        #region TryCalculateRootOfLeafHash(HashAlgorithm, LeafHash, Index, Size, AuditPath, out Root)

        /// <summary>
        /// Calculate the root from the hash of one leaf, its index, the size of the tree and the audit path of the leaf
        /// (RFC 9162, section 2.1.3.2). False when the audit path does not fit the index and size, or its hashes are not
        /// of the length of the hash algorithm.
        /// </summary>
        /// <param name="HashAlgorithm">The hash algorithm.</param>
        /// <param name="LeafHash">The hash of the leaf, H(0x00 || data).</param>
        /// <param name="Index">The index of the leaf.</param>
        /// <param name="Size">The number of leaves.</param>
        /// <param name="AuditPath">The audit path of the leaf.</param>
        /// <param name="Root">The root, when it could be calculated.</param>
        public static Boolean TryCalculateRootOfLeafHash(ETagHashAlgorithm      HashAlgorithm,
                                                         Byte[]                 LeafHash,
                                                         UInt64                 Index,
                                                         UInt64                 Size,
                                                         IReadOnlyList<Byte[]>  AuditPath,
                                                         out Byte[]             Root)
        {

            Root = [];

            var length = ETag.DigestLength(HashAlgorithm);

            if (Index >= Size || LeafHash.Length != length || AuditPath.Any(hash => hash.Length != length))
                return false;

            var fn  = Index;
            var sn  = Size - 1;
            var r   = LeafHash;

            foreach (var p in AuditPath)
            {

                if (sn == 0)
                    return false;

                if ((fn & 1) == 1 || fn == sn)
                {

                    r = NodeHash(HashAlgorithm, p, r);

                    if ((fn & 1) == 0)
                        while ((fn & 1) == 0 && fn != 0)
                        {
                            fn >>= 1;
                            sn >>= 1;
                        }

                }
                else
                    r = NodeHash(HashAlgorithm, r, p);

                fn >>= 1;
                sn >>= 1;

            }

            if (sn != 0)
                return false;

            Root = r;
            return true;

        }

        #endregion


        #region (private static) Root(HashAlgorithm, Hashes, Start, End)

        private static Byte[] Root(ETagHashAlgorithm HashAlgorithm, IReadOnlyList<Byte[]> Hashes, Int32 Start, Int32 End)
        {

            var n = End - Start;

            if (n == 1)
                return Hashes[Start];

            var k = LargestPowerOfTwoBelow(n);

            return NodeHash(HashAlgorithm,
                            Root(HashAlgorithm, Hashes, Start,     Start + k),
                            Root(HashAlgorithm, Hashes, Start + k, End));

        }

        #endregion

        #region (private static) Path(HashAlgorithm, Hashes, Index, Start, End, Collected)

        private static void Path(ETagHashAlgorithm HashAlgorithm, IReadOnlyList<Byte[]> Hashes, Int32 Index, Int32 Start, Int32 End, List<Byte[]> Collected)
        {

            var n = End - Start;

            if (n == 1)
                return;

            var k = LargestPowerOfTwoBelow(n);

            if (Index - Start < k)
            {
                Path(HashAlgorithm, Hashes, Index, Start, Start + k, Collected);
                Collected.Add(Root(HashAlgorithm, Hashes, Start + k, End));
            }
            else
            {
                Path(HashAlgorithm, Hashes, Index, Start + k, End, Collected);
                Collected.Add(Root(HashAlgorithm, Hashes, Start, Start + k));
            }

        }

        #endregion

        #region (private static) LargestPowerOfTwoBelow(N)

        /// <summary>
        /// The largest power of two smaller than the given number, which is at least 2.
        /// </summary>
        private static Int32 LargestPowerOfTwoBelow(Int32 N)
        {

            var k = 1;

            while (k << 1 < N)
                k <<= 1;

            return k;

        }

        #endregion

    }

}
