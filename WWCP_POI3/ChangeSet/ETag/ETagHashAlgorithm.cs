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

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Styx;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Styx.Arrows;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The hash algorithm of an ETag.
    /// </summary>
    public enum ETagHashAlgorithm
    {

        /// <summary>
        /// SHA-256 with a 32-byte digest.
        /// </summary>
        SHA256,

        /// <summary>
        /// SHA-384 with a 48-byte digest.
        /// </summary>
        SHA384,

        /// <summary>
        /// SHA-512 with a 64-byte digest.
        /// </summary>
        SHA512

    }


    /// <summary>
    /// The names of the hash algorithms of ETags and trees, as they are written: sha256, sha384 and sha512.
    /// </summary>
    public static class ETagHashAlgorithmExtensions
    {

        /// <summary>
        /// The name of the given hash algorithm, e.g. sha256.
        /// </summary>
        /// <param name="HashAlgorithm">A hash algorithm.</param>
        public static String ToText(this ETagHashAlgorithm HashAlgorithm)

            => HashAlgorithm switch {
                   ETagHashAlgorithm.SHA256  => "sha256",
                   ETagHashAlgorithm.SHA384  => "sha384",
                   ETagHashAlgorithm.SHA512  => "sha512",
                   _                         => throw new ArgumentOutOfRangeException(nameof(HashAlgorithm), "Unsupported hash algorithm!")
               };

        /// <summary>
        /// Try to parse the given name of a hash algorithm, exactly as it is written.
        /// </summary>
        /// <param name="Text">The name of a hash algorithm, e.g. sha256.</param>
        /// <param name="HashAlgorithm">The hash algorithm.</param>
        public static Boolean TryParse(String?                Text,
                                       out ETagHashAlgorithm  HashAlgorithm)
        {

            switch (Text)
            {
                case "sha256": HashAlgorithm = ETagHashAlgorithm.SHA256; return true;
                case "sha384": HashAlgorithm = ETagHashAlgorithm.SHA384; return true;
                case "sha512": HashAlgorithm = ETagHashAlgorithm.SHA512; return true;
                default:       HashAlgorithm = default;                  return false;
            }

        }

    }

}
