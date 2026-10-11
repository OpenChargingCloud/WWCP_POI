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
    /// A validation or optimistic concurrency failure. No changes have been committed.
    /// </summary>
    public sealed class RoamingNetworkChangeSetException : InvalidOperationException
    {
        internal RoamingNetworkChangeSetException(String      changeSetId,
                                                  Int32?      operationIndex,
                                                  String      message,
                                                  Exception?  inner          = null)
            : base(operationIndex is null ? message : $"Changes[{operationIndex}]: {message}", inner)
        {

            ChangeSetId = changeSetId;
            OperationIndex = operationIndex;

        }

        /// <summary>
        /// The rejected change set identifier.
        /// </summary>
        public String ChangeSetId { get; }

        /// <summary>
        /// The zero-based failed operation index, or null for a batch validation failure.
        /// </summary>
        public Int32? OperationIndex { get; }
    }
}
