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
    /// Connector identifiers are local to their EVSE; their key includes its identifier.
    /// </summary>
    public readonly record struct InfrastructureEntityKey
    {
        private readonly String identity;
        private readonly String? scopeIdentity;

        public InfrastructureEntityKey(InfrastructureEntityType  type,
                                       String                    id,
                                       String?                   scope = null)
        {

            Type = type;
            Id = InfrastructureChangeSchema.Id(type, id);
            Scope = scope;
            identity = InfrastructureChangeSchema.Identity(type, Id);
            scopeIdentity = scope is null ? null : InfrastructureChangeSchema.Identity(InfrastructureEntityType.EVSE, scope);

        }

        /// <summary>
        /// The type of infrastructure entity.
        /// </summary>
        public InfrastructureEntityType Type { get; }

        /// <summary>
        /// The identifier with its domain wire spelling.
        /// </summary>
        public String Id { get; }

        /// <summary>
        /// The EVSE identifier for locally scoped connector IDs.
        /// </summary>
        public String? Scope { get; }

        public Boolean Equals(InfrastructureEntityKey other)
            => Type == other.Type && identity == other.identity && scopeIdentity == other.scopeIdentity;

        public override Int32 GetHashCode()
            => HashCode.Combine(Type, identity, scopeIdentity);
    }
}
