/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// The payload and ancestry role of a static history commit.
/// </summary>
public enum RoamingNetworkCommitKind
{
    Checkpoint,
    ChangeSet,
    Snapshot
}
