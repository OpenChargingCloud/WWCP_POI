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

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Locally chosen bounds for reading change sets received from others,
    /// checked before and while parsing, independent of anything the change set declares.
    /// </summary>
    public sealed record RoamingNetworkChangeSetLimits
    {

        #region Properties

        /// <summary>
        /// The maximum size of the JSON text (UTF-8) or of the CBOR bytes.
        /// </summary>
        public Int32  MaxBytes         { get; init; } = 16 * 1024 * 1024;

        /// <summary>
        /// The maximum nesting depth of the complete change set.
        /// </summary>
        public Int32  MaxDepth         { get; init; } = 32;

        /// <summary>
        /// The maximum number of changes.
        /// </summary>
        public Int32  MaxChanges       { get; init; } = 10_000;

        /// <summary>
        /// The maximum number of signatures.
        /// </summary>
        public Int32  MaxSignatures    { get; init; } = 16;

        /// <summary>
        /// The maximum number of preconditions.
        /// </summary>
        public Int32  MaxPreconditions { get; init; } = 1_000;

        #endregion

        #region Default

        /// <summary>
        /// The default limits: 16 MiB, a depth of 32, 10,000 changes, 16 signatures and 1,000 preconditions.
        /// </summary>
        public static RoamingNetworkChangeSetLimits Default { get; } = new ();

        #endregion

    }

}
