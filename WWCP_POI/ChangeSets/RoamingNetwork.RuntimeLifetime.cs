/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using Newtonsoft.Json.Linq;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetwork
{
    /// <summary>
    /// A removed/reintroduced ownership slot starts a new runtime lifetime, even when the
    /// final static child ID equals its original ID. Unrelated static edits preserve histories.
    /// </summary>
    private static Boolean ResetsNestedRuntime(RoamingNetworkChangeSet changes, InfrastructureEntityType ownerType,
                                               String ownerId, ImmutableArray<POIElementPathSegment> runtimePath)
    {
        foreach (var change in changes.Changes)
        {
            if (InfrastructureChangeSchema.Type(change.EntityType) != ownerType ||
                !InfrastructureChangeSchema.SameId(ownerType, change.EntityId, ownerId)) continue;

            ImmutableArray<POIElementPathSegment> prefix;
            switch (change.Kind)
            {
                case RoamingNetworkChangeKind.RemoveElement:
                    if (RuntimePathPrefix(ownerType.ToString(), change.ElementPath, runtimePath)) return true;
                    continue;
                case RoamingNetworkChangeKind.ReplaceElement:
                    prefix = change.ElementPath;
                    break;
                case RoamingNetworkChangeKind.UpdateProperty:
                    prefix = [new(change.PropertyName!)];
                    break;
                case RoamingNetworkChangeKind.UpdateElementProperty:
                    prefix = change.ElementPath.Add(new(change.PropertyName!));
                    break;
                default:
                    continue;
            }
            if (!RuntimePathPrefix(ownerType.ToString(), prefix, runtimePath)) continue;

            var kind = ownerType.ToString();
            POIElementSchema.Relation relation = null!;
            for (var index = 0; index < prefix.Length; index++)
            {
                relation = POIElementSchema.Child(kind, runtimePath[index].PropertyName);
                kind = relation.Kind;
            }
            var replacement = JToken.Parse(change.NewValue!.Value.GetRawText());
            if (!ContainsRuntimeIdentity(replacement, relation, runtimePath, prefix.Length - 1,
                                         change.Kind == RoamingNetworkChangeKind.ReplaceElement)) return true;
        }
        return false;
    }

    private static Boolean RuntimePathPrefix(String kind, ImmutableArray<POIElementPathSegment> prefix,
                                             ImmutableArray<POIElementPathSegment> path)
    {
        if (prefix.Length > path.Length) return false;
        for (var index = 0; index < prefix.Length; index++)
        {
            var left = prefix[index];
            var right = path[index];
            if (left.PropertyName != right.PropertyName) return false;
            var relation = POIElementSchema.Child(kind, right.PropertyName);
            if (relation.IsArray && left.ElementId is { } id &&
                relation.Identity!(id) != relation.Identity(right.ElementId!)) return false;
            kind = relation.Kind;
        }
        return true;
    }

    private static Boolean ContainsRuntimeIdentity(JToken? value, POIElementSchema.Relation relation,
                                                    ImmutableArray<POIElementPathSegment> path, Int32 index,
                                                    Boolean selectedArrayElement = false)
    {
        if (value is null || value.Type == JTokenType.Null) return false;
        var segment = path[index];
        if (relation.IsArray && !selectedArrayElement)
            value = (value as JArray)?.FirstOrDefault(item => relation.Matches(item, segment.ElementId!));
        if (value is not JObject document) return false;
        if (relation.Identity is not null)
        {
            var actual = relation.ReadId(document);
            if (segment.ElementId is { } expected)
            {
                if (actual is null || relation.Identity(actual) != relation.Identity(expected)) return false;
            }
            else if (actual is not null) return false;
        }
        if (index == path.Length - 1) return true;
        var next = path[index + 1];
        return ContainsRuntimeIdentity(document[next.PropertyName], POIElementSchema.Child(relation.Kind, next.PropertyName), path, index + 1);
    }
}
