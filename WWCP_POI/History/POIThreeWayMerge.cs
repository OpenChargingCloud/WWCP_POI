/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace cloud.charging.open.protocols.WWCP.POI;

// Merge stored static values first; then synthesize addressed operations against the left state.
internal sealed partial class POIThreeWayMerge
{
    private readonly RoamingNetworkDataSnapshot ancestor;
    private readonly RoamingNetworkDataSnapshot left;
    private readonly RoamingNetworkDataSnapshot right;
    private readonly Func<RoamingNetworkMergeConflict, RoamingNetworkMergeResolution?>? resolve;
    private readonly DateTimeOffset timestamp;
    private readonly POILifetimeState ancestorLifetime;
    private readonly POILifetimeState leftLifetime;
    private readonly POILifetimeState rightLifetime;
    private readonly Dictionary<String, RoamingNetworkLifetimeOrigin> desiredLifetimes;
    private readonly Dictionary<InfrastructureEntityKey, InfrastructureEntitySnapshot> desired = [];
    private readonly HashSet<InfrastructureEntityKey> handled = [];
    internal List<RoamingNetworkMergeConflict> Conflicts { get; } = [];

    internal POIThreeWayMerge(RoamingNetworkDataSnapshot ancestor, RoamingNetworkDataSnapshot left,
                              RoamingNetworkDataSnapshot right,
                              Func<RoamingNetworkMergeConflict, RoamingNetworkMergeResolution?>? resolve,
                              DateTimeOffset timestamp, POILifetimeState ancestorLifetime,
                              POILifetimeState leftLifetime, POILifetimeState rightLifetime)
    {
        this.ancestor = ancestor;
        this.left = left;
        this.right = right;
        this.resolve = resolve;
        this.timestamp = timestamp;
        this.ancestorLifetime = ancestorLifetime;
        this.leftLifetime = leftLifetime;
        this.rightLifetime = rightLifetime;
        desiredLifetimes = leftLifetime.Origins.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
    }

    internal RoamingNetworkDataSnapshot? Merge()
    {
        foreach (var key in Order(ancestor.Entities.Keys.Union(left.Entities.Keys).Union(right.Entities.Keys)))
        {
            if (handled.Contains(key)) continue;
            ancestor.Entities.TryGetValue(key, out var b);
            left.Entities.TryGetValue(key, out var l);
            right.Entities.TryGetValue(key, out var r);
            var kind = key.Type.ToString();
            if (l is null && r is null) continue;
            if (b is null)
            {
                if (l is not null && r is not null)
                {
                    if (WholeSame(left, right, key, compareLifetimes: false)) ChooseSubtree(key, left);
                    else WholeConflict(key, RoamingNetworkMergeConflictKind.AddAdd, "Both branches added different data at the same graph identity.");
                }
                else
                {
                    desired[key] = l ?? r!;
                    SelectOrigins(Path(key, []), l is null ? right : left);
                }
                continue;
            }
            if (l is null || r is null)
            {
                var surviving = l is null ? right : left;
                if (!WholeSame(ancestor, surviving, key))
                    WholeConflict(key, RoamingNetworkMergeConflictKind.DeleteModify, "One branch deleted the subtree while the other modified it.");
                continue;
            }
            if (ancestorLifetime.At(Path(key, [])) != leftLifetime.At(Path(key, [])) ||
                ancestorLifetime.At(Path(key, [])) != rightLifetime.At(Path(key, [])) ||
                !Equal(Property(b, "created"), Property(l, "created")) || !Equal(Property(b, "created"), Property(r, "created")))
            {
                if (WholeSame(left, right, key)) ChooseSubtree(key, left);
                else if (WholeSame(ancestor, left, key)) ChooseSubtree(key, right);
                else if (WholeSame(ancestor, right, key)) ChooseSubtree(key, left);
                else if (b.Parent != l.Parent && b.Parent != r.Parent && l.Parent != r.Parent)
                    OwnershipConflict(key, b.Parent, l.Parent, r.Parent);
                else WholeConflict(key, RoamingNetworkMergeConflictKind.ReplaceModify,
                                   "Original operations establish different entity lifetimes; select a complete branch state.");
                continue;
            }
            var parent = Atom(Parent(b.Parent), Parent(l.Parent), Parent(r.Parent), null, key, [], "$parent",
                              RoamingNetworkMergeConflictKind.Ownership);
            var properties = Object(kind, Own(b), Own(l), Own(r), key, []);
            desired[key] = new(l.Key, ReadParent(parent), Properties(properties), []);
        }
        if (Conflicts.Any(conflict => conflict.Resolution is null)) return null;
        var retried = new HashSet<(InfrastructureEntityKey, String?, InfrastructureEntityKey?)>();
        while (true)
        {
            try { return left.MergeTarget(desired.Values); }
            catch (POIMergePlanningException exception)
            {
                var unresolved = new List<RoamingNetworkMergeConflict>();
                var changed = false;
                foreach (var issue in exception.Issues)
                {
                    var key = issue.Entity;
                    var address = (key, issue.PropertyName, issue.RelatedEntity);
                    var kind = issue.RelatedEntity is not null && issue.PropertyName != "$parent"
                                   ? RoamingNetworkMergeConflictKind.Reference : RoamingNetworkMergeConflictKind.InvalidResult;
                    var conflict = new RoamingNetworkMergeConflict(kind, Path(key, [], issue.PropertyName), issue.Message, key,
                        issue.PropertyName, baseValue: GraphValue(ancestor, key), leftValue: GraphValue(left, key),
                        rightValue: GraphValue(right, key), relatedEntity: issue.RelatedEntity);
                    var resolution = retried.Contains(address) ? null : resolve?.Invoke(conflict);
                    if (resolution is null) { unresolved.Add(conflict); continue; }
                    if (resolution.Choice == RoamingNetworkMergeChoice.Custom)
                        throw new ArgumentException("Graph validation/reference conflicts require a whole Base, Left, Right or Remove choice.");
                    retried.Add(address);
                    Conflicts.Add(conflict.WithResolution(resolution));
                    ChooseSubtree(key, resolution.Choice switch {
                        RoamingNetworkMergeChoice.Base => ancestor,
                        RoamingNetworkMergeChoice.Right => right,
                        RoamingNetworkMergeChoice.Remove => null,
                        _ => left
                    });
                    // A whole-entity decision can repair several old issues. Validate again before
                    // reporting or resolving stale issues, and retain only accepted audit decisions.
                    changed = true;
                    break;
                }
                if (changed) continue;
                Conflicts.AddRange(unresolved);
                return null;
            }
        }
    }

    private void WholeConflict(InfrastructureEntityKey key, RoamingNetworkMergeConflictKind kind, String message)
    {
        var conflict = new RoamingNetworkMergeConflict(kind, Path(key, []), message, key,
            baseValue: GraphValue(ancestor, key), leftValue: GraphValue(left, key), rightValue: GraphValue(right, key),
            baseLifetime: ancestorLifetime.At(Path(key, [])), leftLifetime: leftLifetime.At(Path(key, [])),
            rightLifetime: rightLifetime.At(Path(key, [])));
        var resolution = Resolve(conflict);
        if (resolution?.Choice == RoamingNetworkMergeChoice.Custom)
            throw new ArgumentException("Graph structural conflicts require Base, Left, Right or Remove; custom values are for properties/elements.");
        var selected = resolution?.Choice switch {
            RoamingNetworkMergeChoice.Base => ancestor,
            RoamingNetworkMergeChoice.Right => right,
            RoamingNetworkMergeChoice.Remove => null,
            _ => left
        };
        ChooseSubtree(key, selected);
    }

    private void OwnershipConflict(InfrastructureEntityKey key, InfrastructureEntityKey? b,
                                   InfrastructureEntityKey? l, InfrastructureEntityKey? r)
    {
        var address = Path(key, []);
        var conflict = new RoamingNetworkMergeConflict(RoamingNetworkMergeConflictKind.Ownership, Path(key, [], "$parent"),
            "Both branches moved the entity to different owners; select the complete branch lifetime.", key, "$parent",
            baseValue: Parent(b), leftValue: Parent(l), rightValue: Parent(r), baseLifetime: ancestorLifetime.At(address),
            leftLifetime: leftLifetime.At(address), rightLifetime: rightLifetime.At(address));
        var resolution = Resolve(conflict);
        if (resolution?.Choice is RoamingNetworkMergeChoice.Custom or RoamingNetworkMergeChoice.Remove)
            throw new ArgumentException("Ownership conflicts require a Base, Left or Right parent choice.");
        ChooseSubtree(key, resolution?.Choice switch {
            RoamingNetworkMergeChoice.Base => ancestor,
            RoamingNetworkMergeChoice.Right => right,
            _ => left
        });
    }

    private void ChooseSubtree(InfrastructureEntityKey root, RoamingNetworkDataSnapshot? selected)
    {
        var affected = new HashSet<InfrastructureEntityKey>();
        foreach (var snapshot in new[] { ancestor, left, right })
        {
            if (!snapshot.Entities.ContainsKey(root)) continue;
            var pending = new Stack<InfrastructureEntityKey>();
            pending.Push(root);
            while (pending.TryPop(out var key))
            {
                affected.Add(key);
                foreach (var child in snapshot.Entities[key].Children) pending.Push(child);
            }
        }
        foreach (var key in affected)
        {
            handled.Add(key);
            if (selected is not null && selected.Entities.TryGetValue(key, out var node)) desired[key] = node;
            else desired.Remove(key);
            SelectOrigins(Path(key, []), selected);
        }
    }

    private POILifetimeState Lifetimes(RoamingNetworkDataSnapshot snapshot)
        => ReferenceEquals(snapshot, left) ? leftLifetime : ReferenceEquals(snapshot, right) ? rightLifetime : ancestorLifetime;

    private void SelectOrigins(String path, RoamingNetworkDataSnapshot? selected)
    {
        foreach (var key in desiredLifetimes.Keys.Where(key => POILifetimeState.Within(key, path)).ToArray())
            desiredLifetimes.Remove(key);
        if (selected is null) return;
        foreach (var entry in Lifetimes(selected).Origins.Where(entry => POILifetimeState.Within(entry.Key, path)))
            desiredLifetimes[entry.Key] = entry.Value;
    }

    private Boolean LifetimeChanged(String path)
        => leftLifetime.At(path) != desiredLifetimes.GetValueOrDefault(path);

    private Boolean NestedLifetimeChanged(String path)
        => leftLifetime.Origins.Any(entry => POILifetimeState.Within(entry.Key, path) &&
                                            desiredLifetimes.GetValueOrDefault(entry.Key) != entry.Value);

    internal IEnumerable<KeyValuePair<String, RoamingNetworkLifetimeOrigin>> LifetimeSelections
        => desiredLifetimes.Where(entry => leftLifetime.At(entry.Key) is { } old && old != entry.Value).
               OrderBy(entry => entry.Key, StringComparer.Ordinal);

    private JsonElement Object(String kind, JsonElement? b, JsonElement l, JsonElement r,
                                InfrastructureEntityKey key, ImmutableArray<POIElementPathSegment> path)
    {
        var fields = new SortedDictionary<String, JsonElement>(StringComparer.Ordinal);
        foreach (var name in Names(b).Union(Names(l)).Union(Names(r)).Order(StringComparer.Ordinal))
        {
            var bv = Field(b, name);
            var lv = Field(l, name);
            var rv = Field(r, name);
            var value = name == "lastChange" && Managed(kind) ? lv :
                        MergeField(kind, name, bv, lv, rv, key, path);
            if (value is { } present) fields[name] = present;
        }
        return JsonSerializer.SerializeToElement(fields);
    }

    private JsonElement? MergeField(String kind, String name, JsonElement? b, JsonElement? l, JsonElement? r,
                                    InfrastructureEntityKey key, ImmutableArray<POIElementPathSegment> path)
    {
        var relation = POIElementSchema.TryChild(kind, name);
        if (relation is null) return Atom(b, l, r, null, key, path, name);
        if (relation.IsArray && Array(l) && Array(r) && (!b.HasValue || b.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Null))
        {
            var bm = Index(b, relation);
            var lm = Index(l, relation);
            var rm = Index(r, relation);
            var values = new List<JsonElement>();
            foreach (var id in bm.Keys.Union(lm.Keys).Union(rm.Keys).Order(StringComparer.Ordinal))
            {
                var bv = Lookup(bm, id);
                var lv = Lookup(lm, id);
                var rv = Lookup(rm, id);
                var wireId = Id(lv ?? rv ?? bv!.Value, relation)!;
                var itemPath = path.Add(new(name, wireId));
                var value = Element(relation, bv, lv, rv, key, itemPath);
                if (value is { } present)
                {
                    if (relation.Identity!(Id(present, relation)!) != id)
                        throw new ArgumentException("A custom collection resolution cannot change the addressed identity.");
                    values.Add(present);
                }
            }
            return JsonSerializer.SerializeToElement(values.OrderBy(value => Id(value, relation), StringComparer.Ordinal));
        }
        if (!relation.IsArray && (ObjectValue(b) || ObjectValue(l) || ObjectValue(r)))
            return Element(relation, b, l, r, key, path.Add(new(name)));
        var address = Path(key, path, name);
        if (Equivalent(l, r, relation.Kind) && leftLifetime.SameUnder(rightLifetime, address))
        { SelectOrigins(address, left); return l; }
        if (Equivalent(b, l, relation.Kind) && ancestorLifetime.SameUnder(leftLifetime, address))
        { SelectOrigins(address, right); return r; }
        if (Equivalent(b, r, relation.Kind) && ancestorLifetime.SameUnder(rightLifetime, address))
        { SelectOrigins(address, left); return l; }
        var selectedValue = Conflict(!Present(l) || !Present(r) ? RoamingNetworkMergeConflictKind.DeleteModify :
                                                       RoamingNetworkMergeConflictKind.DifferentValues,
                             "The relation value or its owned-object lifetimes changed incompatibly.", b, l, r, key, path, name);
        SelectResolutionOrigins(address, Conflicts[^1].Resolution, freshCustomLifetime: false);
        return selectedValue;
    }

    private JsonElement? Element(POIElementSchema.Relation relation, JsonElement? b, JsonElement? l, JsonElement? r,
                                 InfrastructureEntityKey key, ImmutableArray<POIElementPathSegment> path)
    {
        var address = Path(key, path);
        Boolean Same(JsonElement? x, JsonElement? y, POILifetimeState xs, POILifetimeState ys)
            => Equivalent(x, y, relation.Kind) && (relation.IsReference || xs.SameUnder(ys, address));
        if (Equivalent(l, r, relation.Kind) && (!Present(b) || relation.IsReference || leftLifetime.SameUnder(rightLifetime, address)))
        { SelectOrigins(address, left); return l; }
        if (Same(b, l, ancestorLifetime, leftLifetime))
        { SelectOrigins(address, right); return r; }
        if (Same(b, r, ancestorLifetime, rightLifetime))
        { SelectOrigins(address, left); return l; }
        var kind = !Present(b) ? RoamingNetworkMergeConflictKind.AddAdd :
                   !Present(l) || !Present(r) ? RoamingNetworkMergeConflictKind.DeleteModify :
                   RoamingNetworkMergeConflictKind.ReplaceModify;
        var sameIdentity = relation.Identity is null || SameIdentity(b, l, relation) && SameIdentity(b, r, relation);
        var sameCreation = Equal(Field(b, "created"), Field(l, "created")) && Equal(Field(b, "created"), Field(r, "created"));
        var sameLifetime = ancestorLifetime.At(address) == leftLifetime.At(address) &&
                           ancestorLifetime.At(address) == rightLifetime.At(address);
        if (ObjectValue(b) && ObjectValue(l) && ObjectValue(r) && sameIdentity && sameCreation && sameLifetime)
            return Object(relation.Kind, b, l!.Value, r!.Value, key, path);
        return Conflict(kind, "Incompatible addition, deletion or replacement of the addressed element.", b, l, r, key, path, null);
    }

    private JsonElement? Atom(JsonElement? b, JsonElement? l, JsonElement? r, String? kind,
                              InfrastructureEntityKey key, ImmutableArray<POIElementPathSegment> path, String name,
                              RoamingNetworkMergeConflictKind conflictKind = RoamingNetworkMergeConflictKind.DifferentValues)
    {
        if (Equivalent(l, r, kind)) return l;
        if (Equivalent(b, l, kind)) return r;
        if (Equivalent(b, r, kind)) return l;
        if (!l.HasValue || !r.HasValue) conflictKind = RoamingNetworkMergeConflictKind.DeleteModify;
        return Conflict(conflictKind, "Both branches changed the value incompatibly.", b, l, r, key, path, name);
    }

    private JsonElement? Conflict(RoamingNetworkMergeConflictKind kind, String message, JsonElement? b,
                                  JsonElement? l, JsonElement? r, InfrastructureEntityKey key,
                                  ImmutableArray<POIElementPathSegment> path, String? name)
    {
        var address = Path(key, path);
        var conflict = new RoamingNetworkMergeConflict(kind, Path(key, path, name), message, key, name, path, b, l, r,
            baseLifetime: name is null ? ancestorLifetime.At(address) : null,
            leftLifetime: name is null ? leftLifetime.At(address) : null,
            rightLifetime: name is null ? rightLifetime.At(address) : null);
        var resolution = Resolve(conflict);
        if (name == "$parent" && resolution?.Choice is RoamingNetworkMergeChoice.Custom or RoamingNetworkMergeChoice.Remove)
            throw new ArgumentException("Ownership conflicts require a Base, Left or Right parent choice.");
        if (name is null)
            SelectResolutionOrigins(address, resolution);
        return resolution?.Choice switch {
            RoamingNetworkMergeChoice.Base => b,
            RoamingNetworkMergeChoice.Right => r,
            RoamingNetworkMergeChoice.Remove => null,
            RoamingNetworkMergeChoice.Custom => NormalizeCustom(resolution.Value!.Value, key, path, name),
            _ => l
        };
    }

    private void SelectResolutionOrigins(String address, RoamingNetworkMergeResolution? resolution,
                                         Boolean freshCustomLifetime = true)
        => SelectOrigins(address, resolution?.Choice switch {
            RoamingNetworkMergeChoice.Base => ancestor,
            RoamingNetworkMergeChoice.Right => right,
            RoamingNetworkMergeChoice.Remove => null,
            RoamingNetworkMergeChoice.Custom => freshCustomLifetime ? null : left,
            _ => left
        });

    private RoamingNetworkMergeResolution? Resolve(RoamingNetworkMergeConflict conflict)
    {
        var resolution = resolve?.Invoke(conflict);
        Conflicts.Add(resolution is null ? conflict : conflict.WithResolution(resolution));
        return resolution;
    }

    private JsonElement NormalizeCustom(JsonElement value, InfrastructureEntityKey key,
                                         ImmutableArray<POIElementPathSegment> path, String? name)
    {
        var kind = key.Type.ToString();
        String? parentKind = null;
        POIElementSchema.Relation? terminal = null;
        foreach (var segment in path)
        {
            parentKind = kind;
            terminal = POIElementSchema.Child(kind, segment.PropertyName);
            kind = terminal.Kind;
        }
        var token = InfrastructureJson.ReadToken(value.GetRawText());
        JObject wrapper;
        String field;
        if (name is null)
        {
            field = path[^1].PropertyName;
            wrapper = new(new JProperty(field, terminal!.IsArray ? new JArray(token) : token));
            kind = parentKind!;
        }
        else
        {
            field = name;
            token = Enum.TryParse<InfrastructureEntityType>(kind, out var type) ?
                        MetrologyJson.NormalizeProperty(type, name, token) : POIElementSchema.NormalizeProperty(kind, name, token);
            wrapper = new(new JProperty(name, token));
        }
        POIRepresentation.RequireStatic(wrapper, kind);
        POIRepresentation.RemoveETags(wrapper, kind);
        POIRepresentation.InitializeNestedMetadata(wrapper, kind, timestamp);
        MetrologyJson.NormalizeReadings(wrapper, kind);
        POIRepresentation.NormalizeStaticTimestamps(wrapper, kind);
        token = name is null && terminal!.IsArray ? wrapper[field]![0]! : wrapper[field]!;
        if (name is null && !terminal!.IsReference) token = POIElementSchema.Normalize(terminal.Kind, token);
        using var document = JsonDocument.Parse(token.ToString(Newtonsoft.Json.Formatting.None));
        return document.RootElement.Clone();
    }

    internal ImmutableArray<RoamingNetworkChange>? Operations(RoamingNetworkDataSnapshot target, DateTimeOffset timestamp)
    {
        var removals = left.Entities.Keys.Where(key => !target.Entities.ContainsKey(key)).ToHashSet();
        var additions = target.Entities.Keys.Where(key => !left.Entities.ContainsKey(key)).ToHashSet();
        foreach (var key in left.Entities.Keys.Intersect(target.Entities.Keys))
        {
            var source = left.Entities[key];
            var destination = target.Entities[key];
            if (LifetimeChanged(Path(key, [])) || source.Parent != destination.Parent || source.Properties.Keys.Union(destination.Properties.Keys).Any(name =>
                    name != "lastChange" && !Editable(key.Type, name) && !Equal(Property(source, name), Property(destination, name))))
            { removals.Add(key); additions.Add(key); }
        }
        // Rebuilding an owner imports its complete target subtree; remove existing identities first.
        Boolean changed;
        do
        {
            var count = removals.Count + additions.Count;
            Expand(removals, left);
            Expand(additions, target);
            removals.UnionWith(additions.Where(left.Entities.ContainsKey));
            additions.UnionWith(removals.Where(target.Entities.ContainsKey));
            changed = count != removals.Count + additions.Count;
        } while (changed);
        if (removals.Contains(left.Root)) throw new ArgumentException("Root identity/creation fields cannot be replaced by a merge.");
        var pending = new List<RoamingNetworkChange>();
        foreach (var key in Order(removals.Where(key => left.Entities[key].Parent is not { } parent || !removals.Contains(parent))))
        {
            var node = left.Entities[key];
            pending.Add(RoamingNetworkChange.Remove(key.Type.ToString(), key.Id, null, node.Parent?.Type.ToString(), node.Parent?.Id));
        }
        foreach (var key in Order(additions.Where(key => target.Entities[key].Parent is not { } parent || !additions.Contains(parent))))
        {
            var node = target.Entities[key];
            pending.Add(RoamingNetworkChange.Add(key.Type.ToString(), key.Id, target.MergeEntityValue(key), node.Parent!.Value.Type.ToString(), node.Parent.Value.Id));
        }
        foreach (var key in Order(left.Entities.Keys.Intersect(target.Entities.Keys).Where(key => !additions.Contains(key))))
            DiffObject(key.Type.ToString(), Own(left.Entities[key]), Own(target.Entities[key]), left.Entities[key], [], pending);

        var working = left;
        var ordered = ImmutableArray.CreateBuilder<RoamingNetworkChange>();
        while (pending.Count > 0)
        {
            var failures = new List<(RoamingNetworkChange Operation, String? Error)>();
            var progress = false;
            foreach (var operation in pending.ToArray())
            {
                if (AwaitingReferenceRemoval(operation, pending))
                { failures.Add((operation, "Reference restoration awaits the remaining removal operations.")); continue; }
                if (!working.TryMergeOperation(operation, timestamp, out var next, out var error))
                { failures.Add((operation, error)); continue; }
                working = next!;
                pending.Remove(operation);
                ordered.Add(operation);
                progress = true;
            }
            if (progress) continue;
            if (TryDetachReferences(ref working, target, pending, ordered, timestamp)) continue;
            foreach (var failure in failures)
            {
                var operation = failure.Operation;
                var key = new InfrastructureEntityKey(InfrastructureChangeSchema.Type(operation.EntityType), operation.EntityId,
                    operation.EntityType == nameof(ChargingConnector) ? operation.ParentEntityId : null);
                Conflicts.Add(new(RoamingNetworkMergeConflictKind.InvalidResult, Path(key, operation.ElementPath, operation.PropertyName),
                    failure.Error ?? "No valid ordering of the pending operations was found.", key, operation.PropertyName,
                    operation.ElementPath, GraphValue(ancestor, key), GraphValue(left, key), GraphValue(right, key)));
            }
            return null;
        }
        if (!SameState(working, target))
            throw new ArgumentException("The generated operations do not preserve the selected merged static values.");
        CompleteReferenceTransitions(ordered, working);
        return ordered.ToImmutable();
    }

    private void DiffObject(String kind, JsonElement source, JsonElement target, InfrastructureEntitySnapshot node,
                                    ImmutableArray<POIElementPathSegment> path, List<RoamingNetworkChange> operations)
    {
        foreach (var name in Names(source).Union(Names(target)).Order(StringComparer.Ordinal))
        {
            if (name == "lastChange" && Managed(kind)) continue;
            var old = Field(source, name);
            var value = Field(target, name);
            var relation = POIElementSchema.TryChild(kind, name);
            if (Equal(old, value) && (relation is null || !NestedLifetimeChanged(Path(node.Key, path, name)))) continue;
            if (relation is { IsArray: true } && Array(value) && (!old.HasValue || old.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Null))
            {
                var before = operations.Count;
                var om = Index(old, relation);
                var nm = Index(value, relation);
                foreach (var id in om.Keys.Union(nm.Keys).Order(StringComparer.Ordinal))
                    DiffElement(relation, Lookup(om, id), Lookup(nm, id), node,
                        path.Add(new(name, Id(Lookup(om, id) ?? nm[id], relation))), operations);
                // An order-only change has no element edit; preserve it as an explicit property write.
                if (operations.Count != before) continue;
            }
            else if (relation is { IsArray: false } && (ObjectValue(old) || ObjectValue(value)))
            {
                DiffElement(relation, Present(old) ? old : null, Present(value) ? value : null, node, path.Add(new(name)), operations);
                // RemoveElement writes null; preserve a requested property absence explicitly.
                if (!value.HasValue) PropertyOperation(node, path, name, JsonSerializer.SerializeToElement<Object?>(null), null, operations);
                continue;
            }
            PropertyOperation(node, path, name, old, value, operations);
        }
    }

    private void DiffElement(POIElementSchema.Relation relation, JsonElement? old, JsonElement? value,
                                     InfrastructureEntitySnapshot node, ImmutableArray<POIElementPathSegment> path,
                                     List<RoamingNetworkChange> operations)
    {
        if (Equal(old, value) && !NestedLifetimeChanged(Path(node.Key, path))) return;
        if (!old.HasValue)
        { operations.Add(RoamingNetworkChange.AddElement(node.Key.Type.ToString(), node.Key.Id, path, value!.Value, node.Parent?.Type.ToString(), node.Parent?.Id)); return; }
        if (!value.HasValue)
        { operations.Add(RoamingNetworkChange.RemoveElement(node.Key.Type.ToString(), node.Key.Id, path, old, node.Parent?.Type.ToString(), node.Parent?.Id)); return; }
        var identityChanged = !SameIdentity(old, value, relation);
        var recreate = LifetimeChanged(Path(node.Key, path)) || identityChanged || !Equal(Field(old, "created"), Field(value, "created")) ||
                       ObjectValue(old) && ObjectValue(value) && RequiredChildRecreated(relation.Kind, old.Value, value.Value, node.Key, path);
        if (recreate)
        {
            if (!relation.IsArray && !relation.Optional && identityChanged)
            {
                operations.Add(RoamingNetworkChange.ReplaceElement(node.Key.Type.ToString(), node.Key.Id, path, old, value.Value,
                    node.Parent?.Type.ToString(), node.Parent?.Id));
                return;
            }
            DiffElement(relation, old, null, node, path, operations);
            DiffElement(relation, null, value, node, path, operations);
            return;
        }
        if (ObjectValue(old) && ObjectValue(value) && Names(old).Union(Names(value)).All(name =>
                name == "lastChange" && Managed(relation.Kind) || ElementEditable(relation.Kind, name) || Equal(Field(old, name), Field(value, name))))
        { DiffObject(relation.Kind, old.Value, value.Value, node, path, operations); return; }
        operations.Add(RoamingNetworkChange.ReplaceElement(node.Key.Type.ToString(), node.Key.Id, path, old, value.Value,
            node.Parent?.Type.ToString(), node.Parent?.Id));
    }

    private static void PropertyOperation(InfrastructureEntitySnapshot node, ImmutableArray<POIElementPathSegment> path,
                                          String name, JsonElement? old, JsonElement? value, List<RoamingNetworkChange> operations)
    {
        var type = node.Key.Type.ToString();
        var parentType = node.Parent?.Type.ToString();
        var parentId = node.Parent?.Id;
        operations.Add(path.IsEmpty ? value is { } next ? RoamingNetworkChange.UpdateProperty(type, node.Key.Id, name, old, next, parentType, parentId) :
                                                        RoamingNetworkChange.RemoveProperty(type, node.Key.Id, name, old, parentType, parentId) :
                                     value is { } nested ? RoamingNetworkChange.UpdateElementProperty(type, node.Key.Id, path, name, old, nested, parentType, parentId) :
                                                          RoamingNetworkChange.RemoveElementProperty(type, node.Key.Id, path, name, old, parentType, parentId));
    }

    private static Boolean SameState(RoamingNetworkDataSnapshot a, RoamingNetworkDataSnapshot b)
        => a.Entities.Count == b.Entities.Count && a.Entities.All(entry => b.Entities.TryGetValue(entry.Key, out var other) &&
            entry.Value.Parent == other.Parent && Equivalent(Own(entry.Value), Own(other), entry.Key.Type.ToString()));

    private static void Expand(HashSet<InfrastructureEntityKey> keys, RoamingNetworkDataSnapshot snapshot)
    {
        var pending = new Stack<InfrastructureEntityKey>(keys);
        while (pending.TryPop(out var key))
            foreach (var child in snapshot.Entities[key].Children) if (keys.Add(child)) pending.Push(child);
    }

    internal static String Path(InfrastructureEntityKey key, ImmutableArray<POIElementPathSegment> path, String? property = null)
    {
        var result = "/Entities/" + key.Type + "/" + Escape(InfrastructureChangeSchema.Identity(key.Type, key.Id));
        if (key.Scope is { } scope) result += "/EVSE=" + Escape(InfrastructureChangeSchema.Identity(InfrastructureEntityType.EVSE, scope));
        var kind = key.Type.ToString();
        foreach (var segment in path)
        {
            result += "/" + Escape(segment.PropertyName);
            var relation = POIElementSchema.Child(kind, segment.PropertyName);
            if (segment.ElementId is { } id) result += "/@id=" + Escape(relation.Identity!(id));
            kind = relation.Kind;
        }
        return property is null ? result : result + "/" + Escape(property);
    }

    private static String Escape(String value) => value.Replace("~", "~0").Replace("/", "~1");
    private static Boolean Editable(InfrastructureEntityType kind, String name)
    { try { InfrastructureChangeSchema.Property(kind, name); return true; } catch (ArgumentException) { return false; } }
    private static Boolean ElementEditable(String kind, String name)
    { try { POIElementSchema.Property(kind, name); return true; } catch (ArgumentException) { return false; } }
    private Boolean RequiredChildRecreated(String kind, JsonElement source, JsonElement target,
                                          InfrastructureEntityKey key, ImmutableArray<POIElementPathSegment> path)
        => Names(source).Union(Names(target)).Any(name =>
            POIElementSchema.TryChild(kind, name) is { IsArray: false, Optional: false } relation &&
            ObjectValue(Field(source, name)) && ObjectValue(Field(target, name)) &&
            SameIdentity(Field(source, name), Field(target, name), relation) &&
            (!Equal(Field(Field(source, name), "created"), Field(Field(target, name), "created")) ||
             LifetimeChanged(Path(key, path.Add(new(name))))));
    private static Boolean Managed(String? kind) => kind is not null && (POIElementSchema.HasManagedMetadata(kind) ||
        Enum.TryParse<InfrastructureEntityType>(kind, out var type) && InfrastructureChangeSchema.HasMetadata(type));
    private static IEnumerable<InfrastructureEntityKey> Order(IEnumerable<InfrastructureEntityKey> keys)
        => keys.OrderBy(key => Depth(key.Type)).ThenBy(key => key.Type).ThenBy(key => key.Id, StringComparer.Ordinal).ThenBy(key => key.Scope, StringComparer.Ordinal);
    private static Int32 Depth(InfrastructureEntityType type)
    { var depth = 0; while (type != InfrastructureEntityType.RoamingNetwork) { type = InfrastructureChangeSchema.Relations[type].Parent; depth++; } return depth; }
    private static JsonElement Own(InfrastructureEntitySnapshot node) => JsonSerializer.SerializeToElement(node.Properties);
    private static ImmutableDictionary<String, JsonElement> Properties(JsonElement value)
        => value.EnumerateObject().ToImmutableDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
    private static JsonElement? Full(RoamingNetworkDataSnapshot snapshot, InfrastructureEntityKey key)
        => snapshot.Entities.ContainsKey(key) ? snapshot.MergeEntityValue(key) : null;
    private Boolean WholeSame(RoamingNetworkDataSnapshot a, RoamingNetworkDataSnapshot b, InfrastructureEntityKey key,
                              Boolean compareLifetimes = true)
    {
        if (a.Entities[key].Parent != b.Entities[key].Parent || !Equivalent(Full(a, key), Full(b, key), key.Type.ToString())) return false;
        if (!compareLifetimes) return true;
        var pending = new Stack<InfrastructureEntityKey>();
        pending.Push(key);
        while (pending.TryPop(out var child))
        {
            if (!Lifetimes(a).SameUnder(Lifetimes(b), Path(child, []))) return false;
            foreach (var descendant in a.Entities[child].Children) pending.Push(descendant);
        }
        return true;
    }
    private static JsonElement? GraphValue(RoamingNetworkDataSnapshot snapshot, InfrastructureEntityKey key)
        => snapshot.Entities.TryGetValue(key, out var node) ? JsonSerializer.SerializeToElement(new {
            Parent = Parent(node.Parent), Document = snapshot.MergeEntityValue(key)
        }) : null;
    private static JsonElement? Property(InfrastructureEntitySnapshot node, String name) => node.Properties.TryGetValue(name, out var value) ? value : null;
    private static JsonElement? Field(JsonElement? value, String name)
        => ObjectValue(value) && value!.Value.TryGetProperty(name, out var field) ? field : null;
    private static IEnumerable<String> Names(JsonElement? value)
        => ObjectValue(value) ? value!.Value.EnumerateObject().Select(property => property.Name) : [];
    private static Boolean Array(JsonElement? value) => value?.ValueKind == JsonValueKind.Array;
    private static Boolean ObjectValue(JsonElement? value) => value?.ValueKind == JsonValueKind.Object;
    private static Boolean Present(JsonElement? value) => value.HasValue && value.Value.ValueKind != JsonValueKind.Null;
    private static JsonElement? Lookup(Dictionary<String, JsonElement> values, String key) => values.TryGetValue(key, out var value) ? value : null;
    private static String? Id(JsonElement value, POIElementSchema.Relation relation)
        => relation.IsReference ? value.GetString() : Field(value, relation.IdField ?? "id")?.GetString();
    private static Boolean SameIdentity(JsonElement? a, JsonElement? b, POIElementSchema.Relation relation)
    {
        if (relation.Identity is null) return true;
        var first = a.HasValue ? Id(a.Value, relation) : null;
        var second = b.HasValue ? Id(b.Value, relation) : null;
        return first is null || second is null ? first == second : relation.Identity(first) == relation.Identity(second);
    }
    private static Dictionary<String, JsonElement> Index(JsonElement? value, POIElementSchema.Relation relation)
        => Array(value) ? value!.Value.EnumerateArray().ToDictionary(item => relation.Identity!(Id(item, relation)!), item => item, StringComparer.Ordinal) : [];
    private static Boolean Equal(JsonElement? a, JsonElement? b) => Equivalent(a, b, null);
    private static Boolean Equivalent(JsonElement? a, JsonElement? b, String? kind)
    {
        if (!a.HasValue || !b.HasValue) return a.HasValue == b.HasValue;
        var x = a.Value;
        var y = b.Value;
        if (x.ValueKind != y.ValueKind) return false;
        if (x.ValueKind == JsonValueKind.Object)
        {
            var names = Names(x).Union(Names(y)).Where(name => name != "lastChange" || !Managed(kind));
            foreach (var name in names)
            {
                var left = Field(x, name);
                var right = Field(y, name);
                var relation = kind is null ? null : POIElementSchema.TryChild(kind, name);
                if (relation is { IsArray: true } && Array(left) && Array(right))
                {
                    var lm = Index(left, relation);
                    var rm = Index(right, relation);
                    if (lm.Count != rm.Count || !lm.All(entry => rm.TryGetValue(entry.Key, out var other) && Equivalent(entry.Value, other, relation.Kind))) return false;
                }
                else
                {
                    String? childKind = relation?.Kind;
                    if (childKind is null && kind is not null && Enum.TryParse<InfrastructureEntityType>(kind, out var type))
                        foreach (var child in InfrastructureChangeSchema.Relations)
                            if (child.Value.Parent == type && child.Value.Field == name) { childKind = child.Key.ToString(); break; }
                    if (!Equivalent(left, right, childKind)) return false;
                }
            }
            return true;
        }
        if (x.ValueKind == JsonValueKind.Array)
            return x.GetArrayLength() == y.GetArrayLength() && x.EnumerateArray().Zip(y.EnumerateArray()).All(pair => Equivalent(pair.First, pair.Second, kind));
        return x.ValueKind == JsonValueKind.String ? x.GetString() == y.GetString() : x.GetRawText() == y.GetRawText();
    }

    private static JsonElement? Parent(InfrastructureEntityKey? key)
        => key is { } value ? JsonSerializer.SerializeToElement(new { EntityType = value.Type.ToString(), EntityId = value.Id, value.Scope }) : null;
    private static InfrastructureEntityKey? ReadParent(JsonElement? value)
        => value is { } key ? new(InfrastructureChangeSchema.Type(key.GetProperty("EntityType").GetString()!),
                                  key.GetProperty("EntityId").GetString()!, key.GetProperty("Scope").GetString()) : null;
}
