/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text.Json;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// The outcome of a retained-history three-way merge preview or explicit preparation.
/// </summary>
public enum RoamingNetworkMergeStatus
{
    MergeAvailable,
    Prepared,
    AlreadyIntegrated,
    Conflicts,
    AmbiguousAncestor,
    InvalidInput,
    Unavailable
}

/// <summary>
/// The semantic category of a three-way conflict or candidate validation failure.
/// </summary>
public enum RoamingNetworkMergeConflictKind
{
    DifferentValues,
    DeleteModify,
    AddAdd,
    ReplaceModify,
    Ownership,
    InvalidResult,

    /// <summary>
    /// A merged consumer references a missing target or a target outside its allowed owner scope.
    /// </summary>
    Reference
}

/// <summary>
/// An explicit branch/value choice; missing values are distinct from JSON null.
/// </summary>
public enum RoamingNetworkMergeChoice
{
    Base,
    Left,
    Right,
    Remove,
    Custom
}

/// <summary>
/// An immutable explicit conflict resolution supplied by the application.
/// </summary>
public sealed class RoamingNetworkMergeResolution
{
    private RoamingNetworkMergeResolution(RoamingNetworkMergeChoice choice, JsonElement? value = null)
    {
        if (value is { } element) RoamingNetworkChangeSet.ValidateSigningJSON(element);
        Choice = choice;
        Value = value?.Clone();
    }

    /// <summary>
    /// The selected branch, deletion or application-supplied value.
    /// </summary>
    public RoamingNetworkMergeChoice Choice { get; }

    /// <summary>
    /// A detached custom JSON value; a defined JSON null remains a value.
    /// </summary>
    public JsonElement? Value { get; }

    /// <summary>
    /// Select the common ancestor's value, including its absence.
    /// </summary>
    public static RoamingNetworkMergeResolution UseBase { get; } = new(RoamingNetworkMergeChoice.Base);

    /// <summary>
    /// Select the left value, including its absence.
    /// </summary>
    public static RoamingNetworkMergeResolution UseLeft { get; } = new(RoamingNetworkMergeChoice.Left);

    /// <summary>
    /// Select the right value, including its absence.
    /// </summary>
    public static RoamingNetworkMergeResolution UseRight { get; } = new(RoamingNetworkMergeChoice.Right);

    /// <summary>
    /// Select absence rather than a JSON null value.
    /// </summary>
    public static RoamingNetworkMergeResolution Remove { get; } = new(RoamingNetworkMergeChoice.Remove);

    /// <summary>
    /// Select a detached custom property or nested element value.
    /// </summary>
    public static RoamingNetworkMergeResolution Custom(JsonElement value) => new(RoamingNetworkMergeChoice.Custom, value);
}

/// <summary>
/// A stable addressed conflict with detached base/left/right JSON and any explicit resolution.
/// </summary>
public sealed class RoamingNetworkMergeConflict
{
    internal RoamingNetworkMergeConflict(RoamingNetworkMergeConflictKind kind, String path, String message,
        InfrastructureEntityKey? entity = null, String? propertyName = null,
        ImmutableArray<POIElementPathSegment> elementPath = default,
        JsonElement? baseValue = null, JsonElement? leftValue = null, JsonElement? rightValue = null,
        RoamingNetworkMergeResolution? resolution = null, InfrastructureEntityKey? relatedEntity = null)
    {
        Kind = kind;
        Path = path;
        Message = message;
        Entity = entity;
        PropertyName = propertyName;
        ElementPath = elementPath.IsDefault ? [] : elementPath;
        BaseValue = baseValue?.Clone();
        LeftValue = leftValue?.Clone();
        RightValue = rightValue?.Clone();
        Resolution = resolution;
        RelatedEntity = relatedEntity;
    }

    /// <summary>
    /// The conflict category.
    /// </summary>
    public RoamingNetworkMergeConflictKind Kind { get; }

    /// <summary>
    /// A stable escaped JSON pointer using scoped graph and nested element identities.
    /// </summary>
    public String Path { get; }

    /// <summary>
    /// A diagnostic describing the incompatible edits or validation failure.
    /// </summary>
    public String Message { get; }

    /// <summary>
    /// The affected graph entity, including connector scope.
    /// </summary>
    public InfrastructureEntityKey? Entity { get; }

    /// <summary>
    /// The unresolved or out-of-scope reference target, or invalid merged parent.
    /// This identity is independent of the affected consumer's Entity and PropertyName.
    /// </summary>
    public InfrastructureEntityKey? RelatedEntity { get; }

    /// <summary>
    /// The affected property; null denotes an entity/element structural conflict.
    /// </summary>
    public String? PropertyName { get; }

    /// <summary>
    /// The typed nested ownership path beneath the graph entity.
    /// </summary>
    public ImmutableArray<POIElementPathSegment> ElementPath { get; }

    /// <summary>
    /// The ancestor value; no nullable value denotes absence rather than JSON null.
    /// </summary>
    public JsonElement? BaseValue { get; }

    /// <summary>
    /// The left branch value, including optional absence.
    /// </summary>
    public JsonElement? LeftValue { get; }

    /// <summary>
    /// The right branch value, including optional absence.
    /// </summary>
    public JsonElement? RightValue { get; }

    /// <summary>
    /// An explicit accepted resolution; null means the conflict remains unresolved.
    /// </summary>
    public RoamingNetworkMergeResolution? Resolution { get; }

    internal RoamingNetworkMergeConflict WithResolution(RoamingNetworkMergeResolution resolution)
        => new(Kind, Path, Message, Entity, PropertyName, ElementPath, BaseValue, LeftValue, RightValue, resolution, RelatedEntity);
}

/// <summary>
/// An immutable report that binds the checked tips, selected ancestor and candidate static result.
/// </summary>
public sealed class RoamingNetworkMergeResult
{
    internal RoamingNetworkMergeResult(RoamingNetworkMergeStatus status, String message,
        RoamingNetworkCommitId left, RoamingNetworkCommitId right, RoamingNetworkCommitId? ancestor = null,
        ImmutableArray<RoamingNetworkCommitId> ancestors = default,
        ImmutableArray<RoamingNetworkMergeConflict> conflicts = default, ImmutableArray<ETag> afterETags = default)
    {
        Status = status;
        Message = message;
        Left = left;
        Right = right;
        Ancestor = ancestor;
        AncestorCandidates = ancestors.IsDefault ? [] : ancestors;
        Conflicts = conflicts.IsDefault ? [] : conflicts;
        AfterETags = afterETags.IsDefault ? [] : afterETags;
    }

    /// <summary>
    /// Whether a preview, prepared merge, already-integrated branch or failure was observed.
    /// </summary>
    public RoamingNetworkMergeStatus Status { get; }

    /// <summary>
    /// Whether the tips are compatible or the right tip has already been integrated.
    /// </summary>
    public Boolean CanMerge => Status is RoamingNetworkMergeStatus.MergeAvailable or RoamingNetworkMergeStatus.Prepared or
                                           RoamingNetworkMergeStatus.AlreadyIntegrated;

    /// <summary>
    /// Whether preparation of a compatible preview still requires an explicit request.
    /// </summary>
    public Boolean RequiresExplicitMerge => Status == RoamingNetworkMergeStatus.MergeAvailable;

    /// <summary>
    /// The diagnostic or merge notice.
    /// </summary>
    public String Message { get; }

    /// <summary>
    /// The first-parent source of any prepared merge.
    /// </summary>
    public RoamingNetworkCommitId Left { get; }

    /// <summary>
    /// The branch tip to integrate as an additional parent.
    /// </summary>
    public RoamingNetworkCommitId Right { get; }

    /// <summary>
    /// The selected retained common ancestor, if unambiguous or explicitly selected.
    /// </summary>
    public RoamingNetworkCommitId? Ancestor { get; }

    /// <summary>
    /// All best common ancestors; multiple candidates require an explicit selection.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommitId> AncestorCandidates { get; }

    /// <summary>
    /// Stable ordered conflicts, including any explicit resolutions and candidate validation issues.
    /// </summary>
    public ImmutableArray<RoamingNetworkMergeConflict> Conflicts { get; }

    /// <summary>
    /// The validated candidate's JSON/CBOR state tags; empty when no candidate could be validated.
    /// </summary>
    public ImmutableArray<ETag> AfterETags { get; }
}
