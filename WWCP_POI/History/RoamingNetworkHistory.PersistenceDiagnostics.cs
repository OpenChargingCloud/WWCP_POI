/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

namespace cloud.charging.open.protocols.WWCP.POI;

internal enum ArchiveWriteStage
{
    BeforeTemporaryWrite,
    TemporaryFileFlushed,
    ArchiveReplaced
}

public sealed partial class RoamingNetworkHistory
{
    // Internal process-crash seam. A test can terminate the process after replacement;
    // observers must not throw after the archive has been replaced.
    internal Action<ArchiveWriteStage>? ArchiveWriteObserver { get; set; }
}
