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
using System.Text.Json;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// An immutable entity document and immutable links to its parent and children.
    /// </summary>
    public sealed class InfrastructureEntitySnapshot
    {
        internal InfrastructureEntitySnapshot(InfrastructureEntityKey                    key,
                                              InfrastructureEntityKey?                   parent,
                                              ImmutableDictionary<String, JsonElement>   properties,
                                              ImmutableHashSet<InfrastructureEntityKey>  children)
        {

            Key = key;
            Parent = parent;
            Properties = properties;
            Children = children;

        }

        /// <summary>
        /// The domain identity of this entity.
        /// </summary>
        public InfrastructureEntityKey Key { get; }

        /// <summary>
        /// The owning entity, or null for the root network.
        /// </summary>
        public InfrastructureEntityKey? Parent { get; }

        /// <summary>
        /// Independent immutable JSON values describing this entity.
        /// </summary>
        public ImmutableDictionary<String, JsonElement> Properties { get; }

        /// <summary>
        /// The immutable set of immediate child identities.
        /// </summary>
        public ImmutableHashSet<InfrastructureEntityKey> Children { get; }

        internal InfrastructureEntitySnapshot With(ImmutableDictionary<String, JsonElement>?   properties = null,
                                                   ImmutableHashSet<InfrastructureEntityKey>?  children   = null)
            => new(Key, Parent, properties ?? Properties, children ?? Children);
    }
}
