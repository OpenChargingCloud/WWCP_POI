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
    /// The identification of a checkpoint: the SHA-256 digest of its canonical JSON content without signatures,
    /// as 64 lowercase hexadecimal digits.
    /// </summary>
    public readonly record struct RoamingNetworkCheckpoint_Id
    {

        #region Properties

        /// <summary>
        /// The 64 lowercase hexadecimal digits of the digest.
        /// </summary>
        public String Hex { get; }

        #endregion

        #region Constructor(s)

        private RoamingNetworkCheckpoint_Id(String Hex)
        {
            this.Hex = Hex;
        }

        #endregion


        #region (static) FromDigest(Digest)

        /// <summary>
        /// The checkpoint identification of the given SHA-256 digest.
        /// </summary>
        /// <param name="Digest">A SHA-256 digest.</param>
        public static RoamingNetworkCheckpoint_Id FromDigest(ReadOnlySpan<Byte> Digest)

            => Digest.Length == 32
                   ? new (Convert.ToHexStringLower(Digest))
                   : throw new ArgumentException("A checkpoint identification is a SHA-256 digest of 32 bytes!", nameof(Digest));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a checkpoint identification.
        /// </summary>
        /// <param name="Text">A text representation of a checkpoint identification.</param>
        public static RoamingNetworkCheckpoint_Id Parse(String Text)

            => TryParse(Text, out var checkpointId)
                   ? checkpointId
                   : throw new ArgumentException($"Invalid checkpoint identification '{Text}'!", nameof(Text));

        #endregion

        #region (static) TryParse(Text, out CheckpointId)

        /// <summary>
        /// Try to parse the given text as a checkpoint identification: 64 lowercase hexadecimal digits.
        /// </summary>
        /// <param name="Text">A text representation of a checkpoint identification.</param>
        /// <param name="CheckpointId">The parsed checkpoint identification.</param>
        public static Boolean TryParse([NotNullWhen(true)] String?  Text,
                                       out RoamingNetworkCheckpoint_Id  CheckpointId)
        {

            if (Text is not null &&
                Text.Length == 64 &&
                Text.AsSpan().IndexOfAnyExcept("0123456789abcdef") < 0)
            {
                CheckpointId = new (Text);
                return true;
            }

            CheckpointId = default;
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
