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

using EntityMap          = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, cloud.charging.open.protocols.WWCP.POI.InfrastructureEntitySnapshot>;
using TariffReferenceMap = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, System.Collections.Immutable.ImmutableHashSet<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey>>;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public sealed partial class RoamingNetworkDataSnapshot
    {

        #region Read tariff assignments

        private static IEnumerable<InfrastructureEntityKey> ReferenceKeys(InfrastructureEntitySnapshot entity)
        {

            if (entity.Key.Type is not (InfrastructureEntityType.EVSE or InfrastructureEntityType.ChargingConnector) ||
                !entity.Properties.TryGetValue("tariffIds", out var values))
            {
                yield break;
            }

            if (values.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("tariffIds: expected an array.");

            var seen = new HashSet<InfrastructureEntityKey>();

            foreach (var value in values.EnumerateArray())
            {
                if (value.ValueKind != JsonValueKind.String)
                    throw new ArgumentException("tariffIds: expected a tariff identifier.");

                var key = Key(InfrastructureEntityType.ChargingTariff, value.GetString()!);

                if (!seen.Add(key))
                    throw new ArgumentException($"tariffIds: duplicate reference '{key.Id}'.");

                yield return key;
            }

        }

        #endregion

        #region Maintain the reverse tariff index

        private static TariffReferenceMap AddReferences(InfrastructureEntitySnapshot  entity,
                                                        EntityMap                     map,
                                                        TariffReferenceMap            references)
        {

            foreach (var tariff in ReferenceKeys(entity))
            {
                if (!map.TryGetValue(tariff, out var target))
                    throw new ArgumentException($"tariffIds: tariff '{tariff.Id}' does not exist.");

                var ancestor = entity;

                while (ancestor.Key.Type != InfrastructureEntityType.ChargingStationOperator)
                    ancestor = map[ancestor.Parent ?? throw new ArgumentException("Tariff assignments require an operator.")];

                if (target.Parent != ancestor.Key)
                    throw new ArgumentException($"tariffIds: tariff '{tariff.Id}' belongs to a different operator.");

                var consumers = references.TryGetValue(tariff, out var existing)
                                    ? existing
                                    : ImmutableHashSet<InfrastructureEntityKey>.Empty;

                references = references.SetItem(tariff, consumers.Add(entity.Key));
            }

            return references;

        }

        private static TariffReferenceMap RemoveReferences(InfrastructureEntitySnapshot  entity,
                                                           TariffReferenceMap            references)
        {

            foreach (var tariff in ReferenceKeys(entity))
            {
                if (references.TryGetValue(tariff, out var consumers))
                {
                    consumers = consumers.Remove(entity.Key);

                    references = consumers.IsEmpty
                                     ? references.Remove(tariff)
                                     : references.SetItem(tariff, consumers);
                }
            }

            return references;

        }

        #endregion

    }

}
