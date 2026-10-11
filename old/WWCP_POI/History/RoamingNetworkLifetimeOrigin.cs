/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// The original retained commit and operation establishing an uninterrupted owned-object lifetime.
/// </summary>
public sealed record RoamingNetworkLifetimeOrigin
{
    internal RoamingNetworkLifetimeOrigin(RoamingNetworkCommitId commitId, Int32 operationIndex)
    {
        if (!commitId.IsValid || operationIndex < -1)
            throw new ArgumentException("A lifetime requires a valid commit and operation index.");
        CommitId = commitId;
        OperationIndex = operationIndex;
    }

    /// <summary>
    /// The checkpoint or original commit establishing this lifetime.
    /// </summary>
    public RoamingNetworkCommitId CommitId { get; }

    /// <summary>
    /// The zero-based operation position; -1 identifies the original checkpoint state.
    /// </summary>
    public Int32 OperationIndex { get; }
}
