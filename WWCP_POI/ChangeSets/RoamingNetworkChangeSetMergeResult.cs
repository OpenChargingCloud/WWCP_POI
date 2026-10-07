/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The outcome of checking or explicitly preparing a ChangeSet merge.
    /// </summary>
    public enum RoamingNetworkChangeSetMergeStatus
    {

        /// <summary>
        /// Both batches can be combined; explicit merge preparation is still required.
        /// </summary>
        MergeAvailable,

        /// <summary>
        /// A new unsigned merged batch has been prepared for explicit application.
        /// </summary>
        Merged,

        /// <summary>
        /// Combining the valid batches fails a precondition or depends on their order.
        /// </summary>
        Conflicts,

        /// <summary>
        /// An input batch, signature or requested merge header is invalid.
        /// </summary>
        InvalidInput

    }

    /// <summary>
    /// A merge failure, optionally identifying an original operation or differing result field.
    /// </summary>
    public sealed class RoamingNetworkChangeSetMergeIssue
    {

        internal RoamingNetworkChangeSetMergeIssue(String                   message,
                                                   String?                  changeSetId   = null,
                                                   Int32?                   operationIndex = null,
                                                   InfrastructureEntityKey? entity        = null,
                                                   String?                  propertyName  = null)
        {
            Message        = message;
            ChangeSetId    = changeSetId;
            OperationIndex = operationIndex;
            Entity         = entity;
            PropertyName   = propertyName;
        }

        /// <summary>
        /// The failure and, for combined operations, their attempted execution order.
        /// </summary>
        public String Message { get; }

        /// <summary>
        /// The original input batch identifier, if the failure belongs to one batch.
        /// </summary>
        public String? ChangeSetId { get; }

        /// <summary>
        /// The zero-based operation index within the original input batch, if applicable.
        /// </summary>
        public Int32? OperationIndex { get; }

        /// <summary>
        /// The affected entity, including the EVSE scope of a connector.
        /// </summary>
        public InfrastructureEntityKey? Entity { get; }

        /// <summary>
        /// The affected top-level property, or null for a structural/batch failure.
        /// </summary>
        public String? PropertyName { get; }

    }

    /// <summary>
    /// An immutable merge report. A successful preview does not create or publish a merged batch.
    /// </summary>
    public sealed class RoamingNetworkChangeSetMergeResult
    {

        internal RoamingNetworkChangeSetMergeResult(RoamingNetworkChangeSetMergeStatus              status,
                                                    String                                         message,
                                                    ImmutableArray<RoamingNetworkChangeSetMergeIssue> issues)
        {
            Status  = status;
            Message = message;
            Issues  = issues;
        }

        /// <summary>
        /// Whether a merge is available, prepared, conflicting or invalid.
        /// </summary>
        public RoamingNetworkChangeSetMergeStatus Status { get; }

        /// <summary>
        /// Whether the requested check or merge preparation succeeded.
        /// </summary>
        public Boolean CanMerge
            => Status is RoamingNetworkChangeSetMergeStatus.MergeAvailable or RoamingNetworkChangeSetMergeStatus.Merged;

        /// <summary>
        /// Whether the caller must explicitly request preparation of the compatible merge.
        /// </summary>
        public Boolean RequiresExplicitMerge
            => Status == RoamingNetworkChangeSetMergeStatus.MergeAvailable;

        /// <summary>
        /// A notice for the caller, including the input batch identifiers on success.
        /// </summary>
        public String Message { get; }

        /// <summary>
        /// Validation failures, failed operations or fields that differ between execution orders.
        /// </summary>
        public ImmutableArray<RoamingNetworkChangeSetMergeIssue> Issues { get; }

    }

}
