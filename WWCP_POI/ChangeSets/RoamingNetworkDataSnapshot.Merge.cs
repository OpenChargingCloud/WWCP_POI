/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text.Json;

namespace cloud.charging.open.protocols.WWCP.POI
{

    public sealed partial class RoamingNetworkDataSnapshot
    {

        /// <summary>
        /// Check two batches against their common source and require both combined execution orders
        /// to produce the same complete stored data. By default only a merge notice is returned.
        /// Explicit preparation creates a new unsigned batch; application remains a separate step.
        /// </summary>
        /// <param name="left">The first batch, prepared against this exact source.</param>
        /// <param name="right">The second batch, prepared against this exact source.</param>
        /// <param name="mergedChangeSet">Null during preview or on failure; otherwise the new unsigned batch.</param>
        /// <param name="result">The immutable notice or conflict report.</param>
        /// <param name="merge">Explicitly request creation of a combined batch.</param>
        /// <param name="mergedChangeSetId">A new application-supplied ID, required for explicit preparation.</param>
        /// <param name="createdAt">The merge timestamp; defaults to the later input timestamp.</param>
        /// <param name="verifySignature">A verifier for any signed input batches.</param>
        /// <returns>True for a compatible preview or a prepared merge; false for invalid inputs or conflicts.</returns>
        public Boolean TryMerge(RoamingNetworkChangeSet                       left,
                                RoamingNetworkChangeSet                       right,
                                out RoamingNetworkChangeSet?                  mergedChangeSet,
                                out RoamingNetworkChangeSetMergeResult        result,
                                Boolean                                       merge             = false,
                                String?                                       mergedChangeSetId = null,
                                DateTimeOffset?                               createdAt         = null,
                                Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifySignature = null)
        {
            mergedChangeSet = null;

            if (left is null || right is null)
            {
                result = MergeFailure(RoamingNetworkChangeSetMergeStatus.InvalidInput,
                                      "Both input ChangeSets are required.");
                return false;
            }

            if (merge && (String.IsNullOrWhiteSpace(mergedChangeSetId) ||
                          mergedChangeSetId == left.Id || mergedChangeSetId == right.Id))
            {
                result = MergeFailure(RoamingNetworkChangeSetMergeStatus.InvalidInput,
                                      "Explicit merge preparation requires a new, nonempty mergedChangeSetId.");
                return false;
            }

            var issues = ImmutableArray.CreateBuilder<RoamingNetworkChangeSetMergeIssue>();

            // Each incoming transition must be authentic/valid on its own, including AfterETags.
            foreach (var input in new[] { left, right })
            {
                try
                {
                    ApplyChangeSet(input, verifySignature);
                }
                catch (Exception exception)
                {
                    var index = (exception as RoamingNetworkChangeSetException)?.OperationIndex;
                    issues.Add(MergeOperationIssue(input, index, exception.Message));
                }
            }

            if (issues.Count > 0)
            {
                result = new(RoamingNetworkChangeSetMergeStatus.InvalidInput,
                             "Input validation failed against the common source; no merge was prepared.",
                             issues.ToImmutable());
                return false;
            }

            var timestamp = (createdAt ?? (left.CreatedAt >= right.CreatedAt ? left.CreatedAt : right.CreatedAt)).ToUniversalTime();
            // A private placeholder is used only for local candidate execution during preview.
            var candidateId = merge ? mergedChangeSetId! : "merge-preview";

            try
            {
                var forward = TryMergeOrder(left, right, candidateId, timestamp, issues);
                var reverse = TryMergeOrder(right, left, candidateId, timestamp, issues);

                if (forward is not null && reverse is not null)
                    FindMergeDifferences(forward, reverse, issues);

                if (issues.Count > 0)
                {
                    result = new(RoamingNetworkChangeSetMergeStatus.Conflicts,
                                 "The input batches conflict: their unchanged operations do not commute. No merge was prepared.",
                                 issues.ToImmutable());
                    return false;
                }

                if (!merge)
                {
                    result = new(RoamingNetworkChangeSetMergeStatus.MergeAvailable,
                                 $"ChangeSets '{left.Id}' and '{right.Id}' can be merged. Explicitly request merge preparation to combine them.",
                                 ImmutableArray<RoamingNetworkChangeSetMergeIssue>.Empty);
                    return true;
                }

                mergedChangeSet = new(candidateId, Root.Id, Revision, timestamp,
                                      left.Changes.AddRange(right.Changes), ETags, forward!.ETags);
                result = new(RoamingNetworkChangeSetMergeStatus.Merged,
                             $"ChangeSets '{left.Id}' and '{right.Id}' were combined into unsigned '{candidateId}'. Apply it to the common source to publish a successor.",
                             ImmutableArray<RoamingNetworkChangeSetMergeIssue>.Empty);
                return true;
            }
            catch (Exception exception)
            {
                mergedChangeSet = null;
                result = MergeFailure(RoamingNetworkChangeSetMergeStatus.InvalidInput, exception.Message);
                return false;
            }
        }

        private RoamingNetworkDataSnapshot? TryMergeOrder(RoamingNetworkChangeSet left,
                                                          RoamingNetworkChangeSet right,
                                                          String id,
                                                          DateTimeOffset timestamp,
                                                          ImmutableArray<RoamingNetworkChangeSetMergeIssue>.Builder issues)
        {
            var pending = new RoamingNetworkChangeSet(id, Root.Id, Revision, timestamp,
                                                      left.Changes.AddRange(right.Changes), ETags, ETags);
            try
            {
                // Input tags were checked above; the combined result requires newly calculated tags.
                return ApplyOperations(pending);
            }
            catch (RoamingNetworkChangeSetException exception)
            {
                var source = left;
                var index  = exception.OperationIndex;
                if (index >= left.Changes.Length)
                {
                    source = right;
                    index -= left.Changes.Length;
                }
                issues.Add(MergeOperationIssue(source, index,
                    $"Order '{left.Id}' then '{right.Id}': {exception.InnerException?.Message ?? exception.Message}"));
                return null;
            }
        }

        private static RoamingNetworkChangeSetMergeIssue MergeOperationIssue(RoamingNetworkChangeSet source,
                                                                              Int32? index,
                                                                              String message)
        {
            InfrastructureEntityKey? entity = null;
            String? property = null;
            if (index is >= 0 && index < source.Changes.Length)
            {
                var operation = source.Changes[index.Value];
                property = operation.PropertyName;
                // Invalid incoming type/ID text must still be reportable without throwing.
                try
                {
                    var type = InfrastructureChangeSchema.Type(operation.EntityType);
                    entity = Key(type, operation.EntityId, operation.ParentEntityId);
                }
                catch (Exception) { }
            }
            return new(message, source.Id, index, entity, property);
        }

        private static RoamingNetworkChangeSetMergeResult MergeFailure(RoamingNetworkChangeSetMergeStatus status,
                                                                        String message)
            => new(status, message, [new RoamingNetworkChangeSetMergeIssue(message)]);

        private static void FindMergeDifferences(RoamingNetworkDataSnapshot forward,
                                                 RoamingNetworkDataSnapshot reverse,
                                                 ImmutableArray<RoamingNetworkChangeSetMergeIssue>.Builder issues)
        {
            // Compare the frozen runtime values as well: ETags intentionally exclude them.
            foreach (var key in forward.Entities.Keys.Union(reverse.Entities.Keys)
                                       .OrderBy(key => key.Type)
                                       .ThenBy(key => key.Id, StringComparer.Ordinal)
                                       .ThenBy(key => key.Scope, StringComparer.Ordinal))
            {
                if (!forward.Entities.TryGetValue(key, out var first) ||
                    !reverse.Entities.TryGetValue(key, out var second))
                {
                    issues.Add(new("Entity existence depends on the merge order.", entity: key));
                    continue;
                }

                if (first.Parent != second.Parent || !first.Children.SetEquals(second.Children))
                    issues.Add(new("Entity ancestry or children depend on the merge order.", entity: key));

                foreach (var property in first.Properties.Keys.Union(second.Properties.Keys).Order(StringComparer.Ordinal))
                {
                    if (!first.Properties.TryGetValue(property, out var firstValue) ||
                        !second.Properties.TryGetValue(property, out var secondValue) ||
                        !JsonElement.DeepEquals(firstValue, secondValue))
                    {
                        issues.Add(new("Property value depends on the merge order.", entity: key, propertyName: property));
                    }
                }
            }
        }

    }

}
