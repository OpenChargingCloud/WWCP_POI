/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text.Json;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// An explicit temporary reference detachment and its final restoration in a validated merge plan.
/// </summary>
public sealed class RoamingNetworkMergeReferenceTransition
{
    internal RoamingNetworkMergeReferenceTransition(InfrastructureEntityKey consumer, String propertyName,
        ImmutableArray<InfrastructureEntityKey> targets, JsonElement beforeValue, JsonElement? detachedValue,
        JsonElement? afterValue, ImmutableArray<Int32> detachOperationIndices, ImmutableArray<Int32> restoreOperationIndices)
    {
        Consumer = consumer;
        PropertyName = propertyName;
        Targets = targets;
        BeforeValue = beforeValue.Clone();
        DetachedValue = detachedValue?.Clone();
        AfterValue = afterValue?.Clone();
        DetachOperationIndices = detachOperationIndices;
        RestoreOperationIndices = restoreOperationIndices;
    }

    /// <summary>
    /// The referencing entity, including its EVSE scope for a connector.
    /// </summary>
    public InfrastructureEntityKey Consumer { get; }

    /// <summary>
    /// The schema property carrying the affected references.
    /// </summary>
    public String PropertyName { get; }

    /// <summary>
    /// The temporarily detached target identities in deterministic order.
    /// </summary>
    public ImmutableArray<InfrastructureEntityKey> Targets { get; }

    /// <summary>
    /// The actual reference value immediately before detachment.
    /// </summary>
    public JsonElement BeforeValue { get; }

    /// <summary>
    /// The reference value after detachment; absence differs from explicit JSON null.
    /// </summary>
    public JsonElement? DetachedValue { get; }

    /// <summary>
    /// The actual final value after normalization; absence also covers a permanently removed consumer.
    /// </summary>
    public JsonElement? AfterValue { get; }

    /// <summary>
    /// Zero-based positions of explicit detachment operations in the complete planned batch.
    /// </summary>
    public ImmutableArray<Int32> DetachOperationIndices { get; }

    /// <summary>
    /// Zero-based restoration positions, including a subtree Add when the consumer is rebuilt.
    /// </summary>
    public ImmutableArray<Int32> RestoreOperationIndices { get; }
}
