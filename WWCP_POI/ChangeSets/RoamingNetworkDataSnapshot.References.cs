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

using System.Collections.Immutable;
using System.Text.Json;
using EntityMap = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, cloud.charging.open.protocols.WWCP.POI.InfrastructureEntitySnapshot>;
using ReferenceMap = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, System.Collections.Immutable.ImmutableHashSet<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey>>;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkDataSnapshot
{
    private static IEnumerable<(String Field, InfrastructureEntityType Type, Boolean Array)> ReferenceFields(InfrastructureEntityType type)
    {
        switch (type)
        {
            case InfrastructureEntityType.EVSE:
            case InfrastructureEntityType.ChargingConnector:
                yield return ("tariffIds", InfrastructureEntityType.ChargingTariff, true); break;
            case InfrastructureEntityType.EVSEGroup:
                yield return ("EVSEIds", InfrastructureEntityType.EVSE, true);
                yield return ("tariffId", InfrastructureEntityType.ChargingTariff, false); break;
            case InfrastructureEntityType.ChargingStationGroup:
                yield return ("chargingStationIds", InfrastructureEntityType.ChargingStation, true);
                yield return ("tariffId", InfrastructureEntityType.ChargingTariff, false); break;
            case InfrastructureEntityType.ChargingPoolGroup:
                yield return ("chargingPoolIds", InfrastructureEntityType.ChargingPool, true);
                yield return ("tariffId", InfrastructureEntityType.ChargingTariff, false); break;
            case InfrastructureEntityType.ChargingTariffGroup:
                yield return ("chargingTariffIds", InfrastructureEntityType.ChargingTariff, true); break;
            case InfrastructureEntityType.ParkingOperator:
                yield return ("localParkingSpaceIds", InfrastructureEntityType.ParkingSpace, true);
                yield return ("invalidParkingSpaceIds", InfrastructureEntityType.ParkingSpace, true); break;
            case InfrastructureEntityType.ParkingSpace:
            case InfrastructureEntityType.ParkingSpaceGroup:
                yield return ("sensors", InfrastructureEntityType.ParkingSensor, true);
                yield return ("chargingStationIds", InfrastructureEntityType.ChargingStation, true); break;
            case InfrastructureEntityType.ParkingGarage:
            case InfrastructureEntityType.ParkingSensor:
                yield return ("chargingStationIds", InfrastructureEntityType.ChargingStation, true); break;
        }
    }

    private static IEnumerable<InfrastructureEntityKey> ReferenceKeys(InfrastructureEntitySnapshot entity)
        => ReferenceTargets(entity).Select(reference => reference.Key);

    private static IEnumerable<(String Field, InfrastructureEntityKey Key)> ReferenceTargets(InfrastructureEntitySnapshot entity)
    {
        foreach (var relation in ReferenceFields(entity.Key.Type))
        {
            if (!entity.Properties.TryGetValue(relation.Field, out var values) || values.ValueKind == JsonValueKind.Null)
                continue;
            if (relation.Array && values.ValueKind != JsonValueKind.Array)
                throw new ArgumentException($"{relation.Field}: expected an array.");
            var seen = new HashSet<InfrastructureEntityKey>();
            foreach (var value in relation.Array ? values.EnumerateArray().ToArray() : new[] { values })
            {
                if (value.ValueKind != JsonValueKind.String)
                    throw new ArgumentException($"{relation.Field}: expected an identifier string.");
                var key = Key(relation.Type, value.GetString()!);
                if (!seen.Add(key))
                    throw new ArgumentException($"{relation.Field}: duplicate reference '{key.Id}'.");
                yield return (relation.Field, key);
            }
        }
    }

    private static InfrastructureEntityKey Ancestor(InfrastructureEntitySnapshot entity, InfrastructureEntityType type, EntityMap map)
    {
        while (entity.Key.Type != type)
            entity = map[entity.Parent ?? throw new ArgumentException($"A {type} ownership scope is required.")];
        return entity.Key;
    }

    private static ReferenceMap AddReferences(InfrastructureEntitySnapshot entity, EntityMap map, ReferenceMap references)
    {
        foreach (var reference in ReferenceKeys(entity))
        {
            if (ReferenceError(entity, reference, map) is { } error) throw new ArgumentException(error);
            var consumers = references.TryGetValue(reference, out var existing)
                                ? existing : ImmutableHashSet<InfrastructureEntityKey>.Empty;
            references = references.SetItem(reference, consumers.Add(entity.Key));
        }
        return references;
    }

    // Normal operation validation and merge diagnostics use the same reference/scope rules.
    private static String? ReferenceError(InfrastructureEntitySnapshot entity, InfrastructureEntityKey reference, EntityMap map)
    {
        if (!map.TryGetValue(reference, out var target))
            return $"Reference from '{entity.Key}' to '{reference}' cannot be resolved.";
        if (entity.Key.Type is InfrastructureEntityType.EVSE or InfrastructureEntityType.ChargingConnector or
            InfrastructureEntityType.EVSEGroup or InfrastructureEntityType.ChargingStationGroup or
            InfrastructureEntityType.ChargingPoolGroup or InfrastructureEntityType.ChargingTariffGroup)
        {
            if (Ancestor(entity, InfrastructureEntityType.ChargingStationOperator, map) !=
                Ancestor(target, InfrastructureEntityType.ChargingStationOperator, map))
                return $"Reference '{reference}' belongs to a different charging station operator.";
        }
        else if (reference.Type is InfrastructureEntityType.ParkingSpace or InfrastructureEntityType.ParkingSensor)
        {
            if (Ancestor(entity, InfrastructureEntityType.ParkingOperator, map) != target.Parent)
                return $"Reference '{reference}' belongs to a different parking operator.";
        }
        return null;
    }

    private static ReferenceMap RefreshReferences(InfrastructureEntitySnapshot before, InfrastructureEntitySnapshot after,
                                                  EntityMap map, ReferenceMap references)
    {
        if (ReferenceKeys(before).ToHashSet().SetEquals(ReferenceKeys(after))) return references;
        return AddReferences(after, map, RemoveReferences(before, references));
    }

    private static ReferenceMap RemoveReferences(InfrastructureEntitySnapshot entity, ReferenceMap references)
    {
        foreach (var reference in ReferenceKeys(entity))
            if (references.TryGetValue(reference, out var consumers))
            {
                consumers = consumers.Remove(entity.Key);
                references = consumers.IsEmpty ? references.Remove(reference) : references.SetItem(reference, consumers);
            }
        return references;
    }
}
