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

using System.Diagnostics.CodeAnalysis;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The identification of a commit: the SHA-256 digest of its canonical JSON content,
    /// as 64 lowercase hexadecimal digits.
    /// </summary>
    public readonly record struct RoamingNetworkCommit_Id
    {

        #region Properties

        /// <summary>
        /// The 64 lowercase hexadecimal digits of the digest.
        /// </summary>
        public String Hex { get; }

        #endregion

        #region Constructor(s)

        private RoamingNetworkCommit_Id(String Hex)
        {
            this.Hex = Hex;
        }

        #endregion


        #region (static) FromDigest(Digest)

        /// <summary>
        /// The commit identification of the given SHA-256 digest.
        /// </summary>
        /// <param name="Digest">A SHA-256 digest.</param>
        public static RoamingNetworkCommit_Id FromDigest(ReadOnlySpan<Byte> Digest)

            => Digest.Length == 32
                   ? new (Convert.ToHexStringLower(Digest))
                   : throw new ArgumentException("A commit identification is a SHA-256 digest of 32 bytes!", nameof(Digest));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a commit identification.
        /// </summary>
        /// <param name="Text">A text representation of a commit identification.</param>
        public static RoamingNetworkCommit_Id Parse(String Text)

            => TryParse(Text, out var commitId)
                   ? commitId
                   : throw new ArgumentException($"Invalid commit identification '{Text}'!", nameof(Text));

        #endregion

        #region (static) TryParse(Text, out CommitId)

        /// <summary>
        /// Try to parse the given text as a commit identification: 64 lowercase hexadecimal digits.
        /// </summary>
        /// <param name="Text">A text representation of a commit identification.</param>
        /// <param name="CommitId">The parsed commit identification.</param>
        public static Boolean TryParse([NotNullWhen(true)] String?  Text,
                                       out RoamingNetworkCommit_Id  CommitId)
        {

            if (Text is not null &&
                Text.Length == 64 &&
                Text.AsSpan().IndexOfAnyExcept("0123456789abcdef") < 0)
            {
                CommitId = new (Text);
                return true;
            }

            CommitId = default;
            return false;

        }

        #endregion

        #region ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => Hex ?? "";

        #endregion

    }

}
