using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.WWCP.POI;

namespace WWCP_POI_Tests.Interoperability;

/// <summary>
/// Previous recursive-copy algorithm, frozen before the model preparation optimization.
/// </summary>
internal static class SnapshotPreparationOracle
{
    private sealed record Document(String JSON);
    private static readonly ConditionalWeakTable<IImmutablePOI, Document> documents = new();

    internal static JObject? Get(IImmutablePOI value)
        => documents.TryGetValue(value, out var document) ? InfrastructureJson.ReadObject(document.JSON) : null;

    internal static JObject CompleteImport(JObject source, JObject projection, String kind, Boolean ownedGraph = false)
    {
        var copy = (JObject) source.DeepClone();
        if (ownedGraph)
            foreach (var field in new[] { "roamingNetworkId", "chargingStationOperatorId", "chargingPoolId", "chargingStationId", "EVSEId", "parkingOperatorId" })
                copy.Remove(field);
        if (ownedGraph && kind == nameof(EVSE))
            foreach (var field in new[] { "address", "authenticationModes", "openingTimes" })
                copy.Remove(field); // Validated inherited station views are not independent EVSE properties.
        if (HasMetadata(kind))
            foreach (var field in new[] { "created", "lastChange" })
                if ((copy[field] is null || copy[field]!.Type == JTokenType.Null) && projection[field] is { } timestamp)
                    copy[field] = timestamp.DeepClone();
        if (Enum.TryParse<InfrastructureEntityType>(kind, out var type))
            foreach (var relation in InfrastructureChangeSchema.Relations.Where(relation => relation.Value.Parent == type))
            {
                var field = relation.Value.Field;
                // Resolver views become complete owned documents before immutable capture.
                copy.Remove(field[..^1] + "Ids");
                if (projection[field] is JArray projectedChildren)
                {
                    var supplied = copy[field] as JArray ?? new JArray();
                    var suppliedIndex = Index(supplied, relation.Key.ToString());
                    copy[field] = new JArray(projectedChildren.OfType<JObject>().Select((child, index) =>
                        Find(supplied, child, relation.Key.ToString(), index, suppliedIndex)?.DeepClone() ?? child.DeepClone()));
                }
                else if (copy[field] is null) copy[field] = new JArray();
            }
        IEnumerable<String> views = kind switch {
            nameof(RoamingNetwork) or nameof(ChargingStationOperator) => ["chargingPoolIds", "chargingStationIds", "EVSEIds"],
            nameof(ChargingPool) => ["EVSEIds"],
            _ => []
        };
        foreach (var field in views) copy.Remove(field);
        var brandIds = kind == nameof(EVSE) ? "brandId" : "brandIds";
        var brandField = kind == nameof(EVSE) ? "brand" : "brands";
        if (copy[brandIds] is JArray { Count: > 0 } && projection[brandField] is JArray resolvedBrands)
        {
            var supplied = copy[brandField] as JArray ?? new JArray();
            var suppliedIndex = Index(supplied, nameof(Brand));
            copy[brandField] = new JArray(resolvedBrands.OfType<JObject>().Select((child, index) =>
                Find(supplied, child, nameof(Brand), index, suppliedIndex)?.DeepClone() ?? child.DeepClone()));
            copy.Remove(brandIds);
        }
        foreach (var property in copy.Properties().ToArray())
        {
            if (POIRepresentation.ChildKind(kind, property.Name) is not { } childKind) continue;
            var owned = Enum.TryParse<InfrastructureEntityType>(childKind, out var childType) &&
                        InfrastructureChangeSchema.Relations.TryGetValue(childType, out var childRelation) &&
                        childRelation.Parent.ToString() == kind && childRelation.Field == property.Name;
            if (property.Value is JObject child && projection[property.Name] is JObject projected)
                property.Value = CompleteImport(child, projected, childKind, owned);
            else if (property.Value is JArray children && projection[property.Name] is JArray projectedChildren)
            {
                var lookup = Index(projectedChildren, childKind);
                for (var index = 0; index < children.Count; index++)
                    if (children[index] is JObject item && Find(projectedChildren, item, childKind, index, lookup) is { } projectedItem)
                        children[index] = CompleteImport(item, projectedItem, childKind, owned);
            }
        }
        return copy;
    }

    internal static void Bind(IImmutablePOI value, JObject json)
    {
        var copy = (JObject) json.DeepClone();
        var kind = value.GetType().Name;
        POIRepresentation.RemoveETags(copy, kind);
        POIRepresentation.RemoveRuntime(copy, kind);
        if (value is not (RoamingNetwork or RoamingNetworkDataSnapshot))
            documents.TryAdd(value, new Document(copy.ToString(Newtonsoft.Json.Formatting.None)));
        var indexes = new Dictionary<String, Int32>(StringComparer.Ordinal);
        var lookups = new Dictionary<String, Dictionary<String, JObject>>(StringComparer.Ordinal);
        foreach (var (field, child) in Children(value))
        {
            if (copy[field] is JObject singleton) Bind(child, singleton);
            else if (copy[field] is JArray array)
            {
                var index = indexes.GetValueOrDefault(field);
                indexes[field] = index + 1;
                if (!lookups.TryGetValue(field, out var lookup)) lookups.Add(field, lookup = Index(array, child.GetType().Name));
                var identity = POIRepresentation.WithoutETags(() => POIJSON.Document(child));
                if (Find(array, identity, child.GetType().Name, index, lookup) is { } document) Bind(child, document);
            }
        }
    }

    internal static void OverlayRuntime(JObject target, JObject projection, String kind)
    {
        foreach (var property in projection.Properties())
            if (POIRepresentation.IsRuntimeProperty(kind, property.Name))
                target[property.Name] = property.Value.DeepClone();
        foreach (var property in target.Properties().ToArray())
        {
            if (POIRepresentation.ChildKind(kind, property.Name) is not { } childKind) continue;
            if (property.Value is JObject child && projection[property.Name] is JObject projected)
                OverlayRuntime(child, projected, childKind);
            else if (property.Value is JArray children && projection[property.Name] is JArray projectedChildren)
            {
                var lookup = Index(projectedChildren, childKind);
                for (var index = 0; index < children.Count; index++)
                    if (children[index] is JObject item && Find(projectedChildren, item, childKind, index, lookup) is { } projectedItem)
                        OverlayRuntime(item, projectedItem, childKind);
            }
        }
    }

    private static Boolean HasMetadata(String kind)
        => kind is nameof(EnergyMeter) or nameof(GridOperator) ||
           Enum.TryParse<InfrastructureEntityType>(kind, out var type) && InfrastructureChangeSchema.HasMetadata(type);

    private static String? Identity(JObject document, String kind)
    {
        var id = (document["@id"] ?? document["id"])?.Value<String>();
        return id is null ? null : Enum.TryParse<InfrastructureEntityType>(kind, out var type)
                                      ? InfrastructureChangeSchema.Identity(type, id)
                                      : kind == nameof(EnergyMeter) ? EnergyMeter_Id.Parse(id).ToString().ToUpperInvariant() : id;
    }

    private static Dictionary<String, JObject> Index(JArray array, String kind)
    {
        var lookup = new Dictionary<String, JObject>(StringComparer.Ordinal);
        foreach (var item in array.OfType<JObject>())
            if (Identity(item, kind) is { } id) lookup.TryAdd(id, item);
        return lookup;
    }

    private static JObject? Find(JArray array, JObject identity, String kind, Int32 index, Dictionary<String, JObject> lookup)
        => Identity(identity, kind) is { } id ? lookup.GetValueOrDefault(id) : index < array.Count ? array[index] as JObject : null;

    private static IEnumerable<(String Field, IImmutablePOI Value)> Children(IImmutablePOI value)
    {
        switch (value)
        {
            case RoamingNetwork network:
                foreach (var child in network.ChargingStationOperators) yield return ("chargingStationOperators", child);
                foreach (var child in network.EMobilityProviders) yield return ("eMobilityProviders", child);
                foreach (var child in network.GridOperators) yield return ("gridOperators", child);
                foreach (var child in network.ParkingOperators) yield return ("parkingOperators", child);
                foreach (var child in network.ChargingStationManufacturers) yield return ("chargingStationManufacturers", child);
                break;
            case ChargingStationOperator op:
                foreach (var child in op.ChargingPools) yield return ("chargingPools", child);
                foreach (var child in op.ChargingTariffs) yield return ("chargingTariffs", child);
                foreach (var child in op.EVSEGroups) yield return ("EVSEGroups", child);
                foreach (var child in op.ChargingStationGroups) yield return ("chargingStationGroups", child);
                foreach (var child in op.ChargingPoolGroups) yield return ("chargingPoolGroups", child);
                foreach (var child in op.ChargingTariffGroups) yield return ("chargingTariffGroups", child);
                break;
            case ChargingPool pool:
                foreach (var child in pool.ChargingStations) yield return ("chargingStations", child);
                foreach (var child in pool.EnergyMeters) yield return ("energyMeters", child);
                if (pool.GridConnectionPoint is { } poolPoint) yield return ("gridConnectionPoint", poolPoint);
                break;
            case ChargingStation station:
                foreach (var child in station.EVSEs) yield return ("EVSEs", child);
                foreach (var child in station.EnergyMeters) yield return ("energyMeters", child);
                break;
            case EVSE evse:
                foreach (var child in evse.ChargingConnectors) yield return ("socketOutlets", child);
                if (evse.EnergyMeter is { } evseMeter) yield return ("energyMeter", evseMeter);
                break;
            case ChargingConnector connector:
                if (connector.ChargingCable is { } cable) yield return ("cable", cable);
                break;
            case GridConnectionPoint point:
                if (point.EnergyMeter is { } pointMeter) yield return ("energyMeter", pointMeter);
                break;
            case ChargingTariff tariff:
                foreach (var child in tariff.TariffElements) yield return ("elements", child);
                break;
            case ChargingTariffElement element:
                foreach (var child in element.ChargingPriceComponents) yield return ("priceComponents", child);
                foreach (var child in element.ChargingTariffRestrictions) yield return ("restrictions", child);
                break;
            case ParkingOperator parking:
                foreach (var child in parking.ParkingProducts) yield return ("parkingProducts", child);
                foreach (var child in parking.ParkingGarages) yield return ("parkingGarages", child);
                foreach (var child in parking.ParkingSpaces) yield return ("parkingSpaces", child);
                foreach (var child in parking.ParkingSensors) yield return ("parkingSensors", child);
                foreach (var child in parking.ParkingSpaceGroups) yield return ("parkingSpaceGroups", child);
                break;
        }
    }
}
