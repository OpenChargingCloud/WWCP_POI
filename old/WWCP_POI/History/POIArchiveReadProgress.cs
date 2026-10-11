/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

namespace cloud.charging.open.protocols.WWCP.POI;

internal enum POIArchiveReadStage
{
    Limits,
    Index,
    Canonical,
    CommitModel,
    ReceiptModel,
    RootState,
    SuffixCapture,
    Replay,
    HeadState
}

/// <summary>
/// Per-call cooperative parsing checks. No ambient state or retained history delegate owns this value.
/// The optional internal observer permits deterministic cancellation at existing parser boundaries.
/// Individual Styx, model, cryptography and memory-copy calls are not preempted.
/// </summary>
internal readonly struct POIArchiveReadProgress(CancellationToken token,
    Action<POIArchiveReadStage, Int32>? observer = null)
{
    internal void Check(POIArchiveReadStage stage, Int32 position = 0)
    {
        if (!token.CanBeCanceled && observer is null) return;
        token.ThrowIfCancellationRequested();
        observer?.Invoke(stage, position);
        token.ThrowIfCancellationRequested();
    }
}
